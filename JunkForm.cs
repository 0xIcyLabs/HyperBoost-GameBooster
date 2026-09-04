using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HyperBoost
{
    internal sealed class JunkForm : Form
    {
        private readonly AppLanguage language;
        private readonly string[] roots = JunkCleaner.All();
        private readonly JunkRow[] rows;
        private readonly Label totalLabel = new Label { AutoSize = true };
        private readonly Label banner = new Label { AutoSize = false, Height = 22, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Button cleanButton = new Button();
        private readonly System.Windows.Forms.Timer borderTimer = new System.Windows.Forms.Timer { Interval = 50 };
        private readonly Font fontTotal = new Font("Consolas", 13F, FontStyle.Bold);
        private readonly Font fontTitle = new Font("Segoe UI Semibold", 11F);
        private int tick;
        private bool busy;

        internal JunkForm(AppLanguage appLanguage)
        {
            language = appLanguage;
            RightToLeft = language == AppLanguage.Arabic ? RightToLeft.Yes : RightToLeft.No;
            Text = Texts.T(language, "junkForm");
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 372);
            BackColor = Color.FromArgb(5, 11, 17);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            var heading = new Label
            {
                Dock = DockStyle.Top, Height = 52, Padding = new Padding(18, 12, 18, 0),
                BackColor = Color.FromArgb(8, 20, 30), ForeColor = Color.FromArgb(160, 246, 224),
                Font = fontTitle, Text = Texts.T(language, "junkForm")
            };

            totalLabel.Font = fontTotal;
            totalLabel.ForeColor = Color.FromArgb(0, 255, 199);
            totalLabel.BackColor = Color.FromArgb(8, 20, 30);
            totalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            totalLabel.Location = new Point(ClientSize.Width - 190, 14);
            totalLabel.Text = Texts.T(language, "totalLabel") + " 0.0 MB";

            int top = 64;
            rows = new JunkRow[roots.Length];
            for (int i = 0; i < roots.Length; i++)
            {
                rows[i] = new JunkRow { Left = 16, Top = top, Width = ClientSize.Width - 32, Language = language };
                rows[i].SetName(JunkCleaner.Name(language, i));
                rows[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                rows[i].Click += delegate { RecalcTotal(); };
                top += rows[i].Height + 8;
            }
            ClientSize = new Size(560, top + 52);

            banner.ForeColor = Color.FromArgb(74, 245, 174);
            banner.BackColor = Color.FromArgb(7, 17, 25);
            banner.Font = new Font("Consolas", 9F, FontStyle.Bold);
            banner.Left = 18; banner.Width = ClientSize.Width - 190; banner.Top = ClientSize.Height - 44;
            banner.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            cleanButton.Width = 150; cleanButton.Height = 34;
            cleanButton.FlatStyle = FlatStyle.Flat;
            cleanButton.FlatAppearance.BorderColor = Color.FromArgb(255, 180, 81);
            cleanButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 60, 20);
            cleanButton.BackColor = Color.FromArgb(45, 35, 12);
            cleanButton.ForeColor = Color.FromArgb(230, 255, 250);
            cleanButton.Font = new Font("Segoe UI Semibold", 8.5F);
            cleanButton.Text = Texts.T(language, "cleanJunk");
            cleanButton.Enabled = false;
            cleanButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cleanButton.Left = ClientSize.Width - cleanButton.Width - 16;
            cleanButton.Top = ClientSize.Height - cleanButton.Height - 12;
            cleanButton.Click += async delegate { await RunClean(); };

            Controls.AddRange(new Control[] { banner, cleanButton });
            foreach (var row in rows) Controls.Add(row);
            Controls.Add(totalLabel);
            Controls.Add(heading);

            borderTimer.Tick += delegate { tick++; Invalidate(); };
            borderTimer.Start();
            Shown += async delegate { await RunScan(true); };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { borderTimer.Dispose(); fontTotal.Dispose(); fontTitle.Dispose(); }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (tick == 0) return;
            double phase = (1 + Math.Sin(tick * 0.12)) / 2.0;
            int alpha = 100 + (int)(90 * phase);
            var r = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            using (var pen = new Pen(Color.FromArgb(alpha, 0, 232, 179), 2f)) e.Graphics.DrawRectangle(pen, 1, 1, r.Width - 3, r.Height - 3);
            using (var line = new Pen(Color.FromArgb(10, 0, 232, 179), 1))
                for (int x = -r.Height; x < r.Width; x += 40) e.Graphics.DrawLine(line, x, r.Height, x + r.Height, 0);
            using (var pen = new Pen(Color.FromArgb(alpha / 2, 0, 255, 199), 1f)) e.Graphics.DrawRectangle(pen, 4, 4, r.Width - 9, r.Height - 9);
        }

        private void UpdateTotal(long total)
        {
            if (!IsDisposed && IsHandleCreated) totalLabel.Text = Texts.T(language, "totalLabel") + " " + JunkCleaner.FormatBytes(total);
        }

        private void RecalcTotal()
        {
            long total = 0; bool anySelected = false;
            foreach (var row in rows) if (row.Selected) { total += row.LastBytes; anySelected = true; }
            UpdateTotal(total);
            if (!busy) cleanButton.Enabled = anySelected;
        }

        private async Task RunScan(bool initial)
        {
            busy = true;
            cleanButton.Enabled = false;
            long total = 0;
            bool anySelected = false;
            try
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    string path = roots[i];
                    rows[i].StartScan();
                    SizeResult measured = await Task.Run(() => JunkCleaner.Measure(path));
                    if (IsDisposed || !IsHandleCreated) return;
                    rows[i].FinishScan(measured.Bytes);
                    if (rows[i].Selected) { total += measured.Bytes; anySelected = true; }
                    UpdateTotal(total);
                }
            }
            finally
            {
                busy = false;
                if (!IsDisposed && IsHandleCreated) cleanButton.Enabled = anySelected;
            }
        }

        private async Task RunClean()
        {
            if (busy) return;
            busy = true;
            cleanButton.Enabled = false;
            banner.Text = "";
            long freed = 0; int deleted = 0, skipped = 0;
            try
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (!rows[i].Selected) continue;
                    string path = roots[i];
                    rows[i].StartClean();
                    CleanResult result = await Task.Run(() => JunkCleaner.Clean(path));
                    if (IsDisposed || !IsHandleCreated) return;
                    rows[i].FinishClean(result);
                    freed += result.FreedBytes; deleted += result.DeletedFiles; skipped += result.SkippedFiles;
                }
                if (IsDisposed || !IsHandleCreated) return;
                banner.Text = string.Format(Texts.T(language, "freedFmt"), JunkCleaner.FormatBytes(freed), skipped);
                await RunScan(false);
            }
            finally
            {
                busy = false;
                if (!IsDisposed && IsHandleCreated) cleanButton.Enabled = true;
            }
        }
    }
}
