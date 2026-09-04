using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HyperBoost
{
    internal sealed class TweaksRow : Control
    {
        private bool selected = true, applied;
        private bool hover, pressed;
        private string name = "", description = "";
        private bool effective;
        private AppLanguage language = AppLanguage.English;
        private readonly Font fontName = new Font("Segoe UI Semibold", 9.75F);
        private readonly Font fontDesc = new Font("Segoe UI", 8F);
        private readonly Font fontState = new Font("Consolas", 9F, FontStyle.Bold);

        internal TweaksRow()
        {
            Size = new Size(560, 54);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }
        internal AppLanguage Language { set { language = value; Invalidate(); } }
        internal void SetName(string value) { name = value; Invalidate(); }
        internal void SetDescription(string value) { description = value; Invalidate(); }
        internal void SetEffective(bool value) { effective = value; Invalidate(); }
        internal bool Applied { get { return applied; } set { applied = value; Invalidate(); } }
        internal bool Selected { get { return selected; } set { selected = value; Invalidate(); } }

        protected override void Dispose(bool disposing) { if (disposing) { fontName.Dispose(); fontDesc.Dispose(); fontState.Dispose(); } base.Dispose(disposing); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left && !applied) { pressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnClick(EventArgs e) { if (!applied) { selected = !selected; Invalidate(); } base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var r = ClientRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int yShift = pressed ? 1 : 0;
            var box = new Rectangle(0, yShift, r.Width, r.Height - 3);
            using (var bg = new LinearGradientBrush(new Rectangle(0, yShift, r.Width, r.Height), pressed ? Color.FromArgb(6, 22, 29) : hover ? Color.FromArgb(15, 46, 57) : Color.FromArgb(9, 27, 35), pressed ? Color.FromArgb(5, 17, 23) : hover ? Color.FromArgb(10, 33, 42) : Color.FromArgb(7, 20, 27), 90f))
                g.FillRectangle(bg, box);
            using (var pen = new Pen(applied ? Color.FromArgb(0, 200, 150) : selected ? Color.FromArgb(0, 150, 120) : Color.FromArgb(24, 60, 70), 1))
                g.DrawRectangle(pen, 0, yShift, r.Width - 1, r.Height - 4);
            using (var accent = new SolidBrush(applied ? Color.FromArgb(0, 255, 199) : Color.FromArgb(0, 160, 130)))
                g.FillRectangle(accent, 0, yShift, 3, r.Height - 4);

            int bx = 18, by = r.Height / 2 + yShift;
            using (var pen = new Pen(applied || selected ? Color.FromArgb(0, 255, 199) : Color.FromArgb(70, 110, 120), 2f))
                g.DrawRectangle(pen, bx, by - 8, 16, 16);
            if (applied || selected)
                using (var pen = new Pen(Color.FromArgb(0, 232, 179), 2f))
                {
                    g.DrawLine(pen, bx + 3, by, bx + 7, by + 4);
                    g.DrawLine(pen, bx + 7, by + 4, bx + 14, by - 4);
                }

            bool dim = !selected && !applied;
            Color nameColor = applied ? Color.FromArgb(150, 238, 214) : dim ? Color.FromArgb(110, 140, 148) : Color.FromArgb(224, 255, 248);
            TextRenderer.DrawText(g, name, fontName, new Rectangle(50, 7 + yShift, r.Width - 200, 20), nameColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            Color descColor = dim ? Color.FromArgb(85, 112, 120) : Color.FromArgb(105, 151, 161);
            TextRenderer.DrawText(g, description, fontDesc, new Rectangle(50, 29 + yShift, r.Width - 200, r.Height - 32), descColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            string state = applied ? Texts.T(language, "appliedRow") : effective ? Texts.T(language, "onRow") : Texts.T(language, "offRow");
            Color stateColor = applied ? Color.FromArgb(0, 255, 199) : effective ? Color.FromArgb(115, 241, 209) : Color.FromArgb(120, 150, 158);
            TextRenderer.DrawText(g, state, fontState, new Rectangle(r.Width - 120, yShift, 100, r.Height - 3), stateColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
    }
    internal sealed class TweaksForm : Form
    {
        private readonly AppLanguage language;
        private readonly TweaksRow[] rows;
        private readonly Label note = new Label();
        private readonly Button applyButton = new Button(), revertButton = new Button();

        internal TweaksForm(AppLanguage appLanguage)
        {
            language = appLanguage;
            Text = Texts.T(language, "tweaksForm");
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 560);
            BackColor = Color.FromArgb(5, 11, 17);
            DoubleBuffered = true;
            RightToLeft = language == AppLanguage.Arabic ? RightToLeft.Yes : RightToLeft.No;
            Font = new Font("Segoe UI", 9F);

            var heading = new Label
            {
                Dock = DockStyle.Top, Height = 52, Padding = new Padding(18, 12, 18, 0),
                BackColor = Color.FromArgb(8, 20, 30), ForeColor = Color.FromArgb(160, 246, 224),
                Font = new Font("Segoe UI Semibold", 11F), Text = Texts.T(language, "tweaksForm")
            };

            string[] keys = { "tw0", "tw1", "tw3", "tw4", "tw5", "tw6" };
            rows = new TweaksRow[keys.Length];
            int top = 62;
            for (int i = 0; i < keys.Length; i++)
            {
                rows[i] = new TweaksRow { Left = 16, Top = top, Width = ClientSize.Width - 32, Language = language };
                rows[i].SetName(Texts.T(language, keys[i]));
                rows[i].SetDescription(Texts.T(language, keys[i] + "d"));
                rows[i].SetEffective(GameTweaks.Effective(i));
                rows[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                Controls.Add(rows[i]);
                top += rows[i].Height + 6;
            }

            var countLabel = new Label { AutoSize = true, ForeColor = Color.FromArgb(0, 225, 171), Font = new Font("Consolas", 9F, FontStyle.Bold), BackColor = Color.FromArgb(5, 11, 17) };
            countLabel.Left = 18; countLabel.Top = top + 4;
            note.Height = 40;
            note.ForeColor = Color.FromArgb(255, 180, 81);
            note.Font = new Font("Consolas", 8.25F);
            note.Text = Texts.T(language, "tweaksNote");
            note.Left = 18; note.Width = ClientSize.Width - 36; note.Top = top + 26;

            Action<int> updateCount = delegate(int appliedCount) { countLabel.Text = string.Format(Texts.T(language, "countFmt"), appliedCount); };
            updateCount(0);

            applyButton.Width = 150; applyButton.Height = 34; applyButton.FlatStyle = FlatStyle.Flat;
            applyButton.FlatAppearance.BorderColor = Color.FromArgb(0, 232, 179);
            applyButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 92, 94);
            applyButton.BackColor = Color.FromArgb(15, 53, 65);
            applyButton.ForeColor = Color.FromArgb(230, 255, 250);
            applyButton.Font = new Font("Segoe UI Semibold", 8.5F);
            applyButton.Text = Texts.T(language, "applyTweaks");
            applyButton.Left = 18; applyButton.Top = ClientSize.Height - 44;

            revertButton.Width = 150; revertButton.Height = 34; revertButton.FlatStyle = FlatStyle.Flat;
            revertButton.FlatAppearance.BorderColor = Color.FromArgb(255, 108, 128);
            revertButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 30, 38);
            revertButton.BackColor = Color.FromArgb(70, 26, 39);
            revertButton.ForeColor = Color.FromArgb(230, 255, 250);
            revertButton.Font = new Font("Segoe UI Semibold", 8.5F);
            revertButton.Text = Texts.T(language, "revertTweaks");
            revertButton.Left = 178; revertButton.Top = ClientSize.Height - 44;
            revertButton.Enabled = GameTweaks.AppliedCount > 0;

            Controls.Add(note); Controls.Add(countLabel); Controls.Add(applyButton); Controls.Add(revertButton); Controls.Add(heading);

            applyButton.Click += delegate
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (!rows[i].Selected || rows[i].Applied) continue;
                    try { GameTweaks.Apply(i); rows[i].Applied = true; } catch { }
                }
                applyButton.Enabled = false; revertButton.Enabled = true;
                int appliedCount = 0; foreach (var row in rows) if (row.Applied) appliedCount++;
                updateCount(appliedCount);
                note.ForeColor = Color.FromArgb(74, 245, 174);
                note.Text = Texts.T(language, "tweaksApplied");
            };
            revertButton.Click += delegate
            {
                GameTweaks.RevertAll();
                for (int i = 0; i < rows.Length; i++) { rows[i].Applied = false; rows[i].SetEffective(GameTweaks.Effective(i)); }
                applyButton.Enabled = true; revertButton.Enabled = false;
                updateCount(0);
                note.ForeColor = Color.FromArgb(255, 180, 81);
                note.Text = Texts.T(language, "tweaksNote");
            };
        }
    }
}
