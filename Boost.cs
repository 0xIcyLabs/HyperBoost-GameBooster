using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace HyperBoost
{
    internal enum GpuBoost { None, NvidiaApplied, NvidiaAlready, AmdGuidance }

    internal static class Boost
    {
        private static readonly Regex GuidRegex = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
        private const string UltimateTemplate = "e9a42b02-d5df-448d-aa00-03f14749eb61";
        private const string HighPerformance = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        internal static string Run(string exe, string args)
        {
            try
            {
                using (var process = new Process())
                {
                    var buffer = new System.Text.StringBuilder();
                    process.StartInfo = new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                    process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs data) { if (data.Data != null) lock (buffer) buffer.AppendLine(data.Data); };
                    process.Start();
                    process.BeginOutputReadLine();
                    if (!process.WaitForExit(10000))
                    {
                        try { process.Kill(); } catch { }
                    }
                    process.WaitForExit();
                    lock (buffer) return buffer.ToString();
                }
            }
            catch { return ""; }
        }

        internal static Guid GetActiveScheme()
        {
            var match = GuidRegex.Match(Run("powercfg", "/getactivescheme"));
            return match.Success ? new Guid(match.Value) : Guid.Empty;
        }

        internal static string ApplyUltimatePower(out Guid previousActive, out Guid createdScheme)
        {
            previousActive = GetActiveScheme();
            createdScheme = Guid.Empty;
            Guid target = Guid.Empty;
            string listing = Run("powercfg", "-list");
            foreach (Match match in GuidRegex.Matches(listing))
            {
                int start = match.Index, end = listing.IndexOf('\n', start);
                string line = end < 0 ? listing.Substring(start) : listing.Substring(start, end - start);
                if (line.IndexOf("Ultimate", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("ìµœì¢… ì„±ëŠ¥", StringComparison.Ordinal) >= 0) { target = new Guid(match.Value); break; }
            }
            if (target == Guid.Empty)
            {
                var match = GuidRegex.Match(Run("powercfg", "-duplicatescheme " + UltimateTemplate));
                if (match.Success) { target = new Guid(match.Value); createdScheme = target; }
            }
            if (target == Guid.Empty) target = new Guid(HighPerformance);
            if (previousActive == target) return "no change";
            Run("powercfg", "-setactive " + target.ToString("D"));
            return GetActiveScheme() == target ? "ok" : "failed";
        }

        internal static string NvidiaSmiPath()
        {
            return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\nvidia-smi.exe");
        }

        // Reads the display adapter descriptions from the registry and returns
        // "nvidia", "amd" or "" (unknown / Intel / etc.).
        internal static string GpuVendor()
        {
            try
            {
                string vendor = "";
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (baseKey == null) return vendor;
                    foreach (string sub in baseKey.GetSubKeyNames())
                    {
                        if (!Regex.IsMatch(sub, @"^\d{4}$")) continue;
                        string desc;
                        using (var key = baseKey.OpenSubKey(sub)) desc = Convert.ToString(key == null ? null : key.GetValue("DriverDesc")) ?? "";
                        if (desc.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 || desc.IndexOf("GeForce", StringComparison.OrdinalIgnoreCase) >= 0) vendor = "nvidia";
                        else if (vendor != "nvidia" && (desc.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 || desc.IndexOf("Radeon", StringComparison.OrdinalIgnoreCase) >= 0)) vendor = "amd";
                    }
                }
                return vendor;
            }
            catch { return ""; }
        }

        internal static GpuBoost SetGpuMaxPerformance()
        {
            string vendor = GpuVendor();
            if (vendor == "amd") return GpuBoost.AmdGuidance;
            string smi = NvidiaSmiPath();
            if (!System.IO.File.Exists(smi)) return GpuBoost.NvidiaAlready; // no NVIDIA tooling present; nothing we can do
            string query = Run(smi, "-q -d PERSISTENCE_MODE");
            if (!Regex.IsMatch(query, "Persistence-Mode", RegexOptions.IgnoreCase)) return GpuBoost.NvidiaAlready; // persistence mode not supported on this driver (typical Windows GeForce) - nothing to do
            if (Regex.IsMatch(query, "Persistence-Mode\\s*:\\s*Enabled", RegexOptions.IgnoreCase)) return GpuBoost.NvidiaAlready;
            Run(smi, "-pm 1");
            return Regex.IsMatch(Run(smi, "-q -d PERSISTENCE_MODE"), "Persistence-Mode\\s*:\\s*Enabled", RegexOptions.IgnoreCase) ? GpuBoost.NvidiaApplied : GpuBoost.NvidiaAlready;
        }

        internal static bool StartTimerResolution(out int previousResolution)
        {
            int minimum, maximum, current;
            NtQueryTimerResolution(out minimum, out maximum, out current);
            previousResolution = current;
            int actual;
            int result = NtSetTimerResolution(5000, true, out actual);
            return result >= 0;
        }

        internal static void StopTimerResolution(int previousResolution)
        {
            if (previousResolution > 0) { int actual; NtSetTimerResolution(previousResolution, true, out actual); }
        }

        internal static void RestoreGpu()
        {
            string smi = NvidiaSmiPath();
            if (System.IO.File.Exists(smi)) Run(smi, "-pm 0");
        }

        internal static void RestorePower(Guid previousScheme, Guid createdScheme)
        {
            if (previousScheme != Guid.Empty) Run("powercfg", "-setactive " + previousScheme.ToString("D"));
            if (createdScheme != Guid.Empty) Run("powercfg", "-delete " + createdScheme.ToString("D"));
        }

        [System.Runtime.InteropServices.DllImport("ntdll.dll")] private static extern int NtQueryTimerResolution(out int minimum, out int maximum, out int current);
        [System.Runtime.InteropServices.DllImport("ntdll.dll")] private static extern int NtSetTimerResolution(int requested, bool set, out int actual);
    }
}
