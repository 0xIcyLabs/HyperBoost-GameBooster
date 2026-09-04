using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HyperBoost
{
    internal static class Agent
    {
        internal static List<string> Games = new List<string>();
        internal static bool AutoBoost = true;

        internal static string Dir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost");
        }
        internal static string GamesFilePath()
        {
            return Path.Combine(Dir(), "games.txt");
        }
        internal static string ConfigFilePath()
        {
            return Path.Combine(Dir(), "agent.cfg");
        }
        internal static void Load()
        {
            try
            {
                Games.Clear();
                if (File.Exists(GamesFilePath()))
                    foreach (string line in File.ReadAllLines(GamesFilePath()))
                        if (!string.IsNullOrWhiteSpace(line)) Games.Add(line.Trim().ToLowerInvariant());
                if (File.Exists(ConfigFilePath()))
                    foreach (string line in File.ReadAllLines(ConfigFilePath()))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2 && parts[0] == "autoboost") AutoBoost = parts[1] == "1";
                    }
            }
            catch { }
        }
        internal static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir());
                File.WriteAllLines(GamesFilePath(), Games.ToArray());
                File.WriteAllLines(ConfigFilePath(), new[] { "autoboost=" + (AutoBoost ? "1" : "0") });
            }
            catch { }
        }
        internal static bool StartupEnabled
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                        return key != null && key.GetValue("HyperBoost") != null;
                }
                catch { return false; }
            }
            set
            {
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    {
                        if (value) key.SetValue("HyperBoost", "\"" + Application.ExecutablePath + "\"");
                        else key.DeleteValue("HyperBoost", false);
                    }
                }
                catch { }
            }
        }
    }

    internal static class RecoveryState
    {
        private static string FilePath()
        {
            return Path.Combine(Agent.Dir(), "recovery.txt");
        }
        internal static void Save(Guid previous, Guid created, bool gpu)
        {
            try
            {
                Directory.CreateDirectory(Agent.Dir());
                File.WriteAllLines(FilePath(), new[] { "prev=" + previous, "created=" + created, "gpu=" + (gpu ? "1" : "0") });
            }
            catch { }
        }
        internal static bool Exists()
        {
            try { return File.Exists(FilePath()); }
            catch { return false; }
        }
        internal static void Clear()
        {
            try { File.Delete(FilePath()); } catch { }
        }
        internal static void RestoreIfInterrupted()
        {
            try
            {
                if (!Exists()) return;
                Guid previous = Guid.Empty, created = Guid.Empty; bool gpu = false;
                foreach (string line in File.ReadAllLines(FilePath()))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    if (parts[0] == "prev") Guid.TryParse(parts[1], out previous);
                    else if (parts[0] == "created") Guid.TryParse(parts[1], out created);
                    else if (parts[0] == "gpu") gpu = parts[1] == "1";
                }
                var choice = MessageBox.Show(Texts.T(AppLanguage.English, "recoverMsg"), Texts.T(AppLanguage.English, "recoverTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (choice == DialogResult.Yes)
                {
                    if (previous != Guid.Empty || created != Guid.Empty) Boost.RestorePower(previous, created);
                    if (gpu) Boost.RestoreGpu();
                    GameTweaks.LoadJournal();
                    GameTweaks.RevertAll();
                }
                Clear();
            }
            catch { }
        }
    }

    internal sealed class AgentForm : Form
    {
        private readonly AppLanguage language;
        private readonly ListBox list = new ListBox();
        private readonly CheckBox autoBox = new CheckBox(), startupBox = new CheckBox();

        internal AgentForm(AppLanguage appLanguage)
        {
            language = appLanguage;
            Text = Texts.T(language, "agentForm");
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 468);
            BackColor = Color.FromArgb(5, 11, 17);
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9F);
            RightToLeft = language == AppLanguage.Arabic ? RightToLeft.Yes : RightToLeft.No;

            var heading = new Label
            {
                Dock = DockStyle.Top, Height = 52, Padding = new Padding(18, 12, 18, 0),
                BackColor = Color.FromArgb(8, 20, 30), ForeColor = Color.FromArgb(160, 246, 224),
                Font = new Font("Segoe UI Semibold", 11F), Text = Texts.T(language, "agentForm")
            };

            var hint = new Label
            {
                Left = 18, Top = 60, Width = ClientSize.Width - 36, Height = 44,
                ForeColor = Color.FromArgb(105, 151, 161), Font = new Font("Segoe UI", 8.5F),
                Text = Texts.T(language, "agentHint")
            };

            list.Left = 18; list.Top = 110; list.Width = ClientSize.Width - 36; list.Height = 200;
            list.BorderStyle = BorderStyle.FixedSingle;
            list.BackColor = Color.FromArgb(7, 17, 25);
            list.ForeColor = Color.FromArgb(213, 232, 235);
            RefreshList();

            var add = MakeButton(Texts.T(language, "agentAdd"), Color.FromArgb(15, 53, 65), Color.FromArgb(0, 232, 179));
            add.Left = 18; add.Top = 320;
            add.Click += delegate
            {
                using (var dialog = new OpenFileDialog { Filter = "Games (*.exe)|*.exe", Title = Texts.T(language, "agentAdd") })
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        string game = Path.GetFileNameWithoutExtension(dialog.FileName).ToLowerInvariant();
                        if (!Agent.Games.Contains(game)) { Agent.Games.Add(game); Agent.Save(); RefreshList(); }
                    }
            };

            var remove = MakeButton(Texts.T(language, "agentRemove"), Color.FromArgb(70, 26, 39), Color.FromArgb(255, 108, 128));
            remove.Left = 18 + 150 + 10; remove.Top = 320;
            remove.Click += delegate
            {
                if (list.SelectedItem != null) { Agent.Games.Remove(Convert.ToString(list.SelectedItem)); Agent.Save(); RefreshList(); }
            };

            autoBox.Left = 18; autoBox.Top = 372; autoBox.Width = ClientSize.Width - 36; autoBox.AutoSize = true;
            autoBox.ForeColor = Color.FromArgb(213, 232, 235); autoBox.Text = Texts.T(language, "agentAuto");
            autoBox.Checked = Agent.AutoBoost;
            autoBox.CheckedChanged += delegate { Agent.AutoBoost = autoBox.Checked; Agent.Save(); };

            startupBox.Left = 18; startupBox.Top = 400; startupBox.Width = ClientSize.Width - 36; startupBox.AutoSize = true;
            startupBox.ForeColor = Color.FromArgb(213, 232, 235); startupBox.Text = Texts.T(language, "agentStartup");
            startupBox.Checked = Agent.StartupEnabled;
            startupBox.CheckedChanged += delegate { Agent.StartupEnabled = startupBox.Checked; };

            Controls.Add(hint); Controls.Add(list); Controls.Add(add); Controls.Add(remove);
            Controls.Add(autoBox); Controls.Add(startupBox); Controls.Add(heading);
        }

        private Button MakeButton(string text, Color back, Color border)
        {
            return new Button { Text = text, Width = 150, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = Color.FromArgb(230, 255, 250), Font = new Font("Segoe UI Semibold", 8.5F), FlatAppearance = { BorderColor = border, MouseOverBackColor = Color.FromArgb(20, 92, 94) } };
        }

        private void RefreshList()
        {
            list.BeginUpdate(); list.Items.Clear();
            foreach (string game in Agent.Games) list.Items.Add(game);
            list.EndUpdate();
        }
    }
}
