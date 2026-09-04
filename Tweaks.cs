using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace HyperBoost
{
    internal static class GameTweaks
    {
        internal sealed class UndoEntry { internal RegistryKey Hive; internal string Path; internal string Name; internal bool Existed; internal object Value; internal string[] PowerRestore; }

        private static readonly object gate = new object();
        private static readonly List<UndoEntry> undo = new List<UndoEntry>();
        internal static int AppliedCount { get { lock (gate) return undo.Count; } }

        internal static bool ReadDword(RegistryKey hive, string path, string name, out int value)
        {
            value = 0;
            try
            {
                using (var key = hive.OpenSubKey(path))
                {
                    if (key == null) return false;
                    object raw = key.GetValue(name);
                    if (raw is int) { value = (int)raw; return true; }
                    if (raw != null) { value = Convert.ToInt32(raw); return true; }
                    return false;
                }
            }
            catch { return false; }
        }

        private static void SetDword(RegistryKey hive, string path, string name, int newValue)
        {
            bool existed = false; object previous = null;
            using (var key = hive.CreateSubKey(path))
            {
                previous = key.GetValue(name);
                existed = previous != null;
                key.SetValue(name, newValue, RegistryValueKind.DWord);
            }
            lock (gate) undo.Add(new UndoEntry { Hive = hive, Path = path, Name = name, Existed = existed, Value = previous });
        }

        private static void SetString(RegistryKey hive, string path, string name, string newValue)
        {
            bool existed = false; object previous = null;
            using (var key = hive.CreateSubKey(path))
            {
                previous = key.GetValue(name);
                existed = previous != null;
                key.SetValue(name, newValue, RegistryValueKind.String);
            }
            lock (gate) undo.Add(new UndoEntry { Hive = hive, Path = path, Name = name, Existed = existed, Value = previous });
        }

        private static readonly string[][] PowerSettings = {
            new[] { "SUB_PCIEXPRESS", "ASPM" },
            new[] { "SUB_USB", "USBSELECTIVESUSPEND" },
            new[] { "SUB_PROCESSOR", "PERFBOOSTMODE" }
        };
        private static readonly string[] PowerNewValues = { "0", "0", "2" };

        private static string PowerQuery(string sub, string setting)
        {
            string output = Boost.Run("powercfg", "/q SCHEME_CURRENT " + sub + " " + setting);
            Match match = Regex.Match(output, @"Current AC Power Setting Index:\s*0x([0-9a-fA-F]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        internal static void Apply(int id)
        {
            switch (id)
            {
                case 0:
                    SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
                    SetDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
                    break;
                case 1:
                    SetDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0);
                    SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
                    break;
                case 2:
                    SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                    break;
                case 3:
                    SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF));
                    break;
                case 4:
                    SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 10);
                    break;
                case 5:
                    var restore = new List<string>();
                    for (int i = 0; i < PowerSettings.Length; i++)
                    {
                        string original = PowerQuery(PowerSettings[i][0], PowerSettings[i][1]);
                        if (original != null) restore.Add("-setacvalueindex SCHEME_CURRENT " + PowerSettings[i][0] + " " + PowerSettings[i][1] + " " + original);
                        Boost.Run("powercfg", "-setacvalueindex SCHEME_CURRENT " + PowerSettings[i][0] + " " + PowerSettings[i][1] + " " + PowerNewValues[i]);
                    }
                    Boost.Run("powercfg", "-setactive SCHEME_CURRENT");
                    lock (gate) undo.Add(new UndoEntry { PowerRestore = restore.ToArray() });
                    break;
                case 6:
                    SetString(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\QoS\HyperBoost", "Application Name", "*");
                    SetString(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\QoS\HyperBoost", "DSCP Value", "46");
                    SetString(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\QoS\HyperBoost", "Throttle Rate", "-1");
                    SetString(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\QoS\HyperBoost", "Protocol", "*");
                    break;
            }
        }

        internal static void RevertAll()
        {
            lock (gate)
            {
                for (int i = undo.Count - 1; i >= 0; i--)
                {
                    UndoEntry entry = undo[i];
                    try
                    {
                        if (entry.PowerRestore != null)
                        {
                            foreach (string command in entry.PowerRestore) Boost.Run("powercfg", command);
                            Boost.Run("powercfg", "-setactive SCHEME_CURRENT");
                        }
                        else
                        {
                            using (var key = entry.Hive.CreateSubKey(entry.Path))
                            {
                                if (key == null) continue;
                                if (entry.Existed) key.SetValue(entry.Name, entry.Value);
                                else { try { key.DeleteValue(entry.Name, false); } catch { } }
                            }
                        }
                    }
                    catch { }
                }
                undo.Clear();
            }
            ClearJournalFile();
        }
        internal static string JournalPath()
        {
            return Path.Combine(Agent.Dir(), "journal.txt");
        }

        internal static void SaveJournal()
        {
            try
            {
                Directory.CreateDirectory(Agent.Dir());
                lock (gate)
                {
                    var lines = new List<string>();
                    foreach (UndoEntry e in undo)
                    {
                        if (e.PowerRestore != null) lines.Add("P|" + string.Join("|", e.PowerRestore));
                        else lines.Add("R|" + (e.Hive == Registry.LocalMachine ? "LM" : "CU") + "|" + e.Path + "|" + e.Name + "|" + (e.Existed ? "1" : "0") + "|" + (e.Value is int ? "D" : "S") + "|" + (e.Value == null ? "" : e.Value.ToString()));
                    }
                    File.WriteAllLines(JournalPath(), lines.ToArray());
                }
            }
            catch { }
        }

        internal static void LoadJournal()
        {
            lock (gate)
            {
                if (undo.Count > 0) return;
                try
                {
                    if (!File.Exists(JournalPath())) return;
                    foreach (string line in File.ReadAllLines(JournalPath()))
                    {
                        string[] p = line.Split('|');
                        if (p[0] == "P" && p.Length > 1)
                        {
                            var commands = new string[p.Length - 1];
                            Array.Copy(p, 1, commands, 0, commands.Length);
                            undo.Add(new UndoEntry { PowerRestore = commands });
                        }
                        else if (p[0] == "R" && p.Length >= 7)
                        {
                            object parsed = null;
                            if (p[5] == "D") { int number; if (int.TryParse(p[6], out number)) parsed = number; }
                            else parsed = p[6];
                            undo.Add(new UndoEntry { Hive = p[1] == "LM" ? Registry.LocalMachine : Registry.CurrentUser, Path = p[2], Name = p[3], Existed = p[4] == "1", Value = parsed });
                        }
                    }
                }
                catch { }
            }
        }

        internal static void ClearJournalFile()
        {
            try { File.Delete(JournalPath()); } catch { }
        }

        internal static bool Effective(int id)
        {
            int value;
            switch (id)
            {
                case 0: return ReadDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", out value) && value == 1;
                case 1: return ReadDword(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", out value) && value == 0
                    && (!ReadDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", out value) || value == 0);
                case 2: return ReadDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", out value) && value == 2;
                case 3: return ReadDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", out value) && value == -1;
                case 4: return ReadDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", out value) && value <= 10;
                case 5:
                    {
                        string aspm = PowerQuery("SUB_PCIEXPRESS", "ASPM");
                        string boost = PowerQuery("SUB_PROCESSOR", "PERFBOOSTMODE");
                        return aspm == "0" && boost == "2";
                    }
                case 6:
                    {
                        try
                        {
                            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\QoS\HyperBoost"))
                            {
                                if (key == null) return false;
                                object dscp = key.GetValue("DSCP Value");
                                return dscp != null && Convert.ToString(dscp) == "46";
                            }
                        }
                        catch { return false; }
                    }
                default: return false;
            }
        }
    }
}
