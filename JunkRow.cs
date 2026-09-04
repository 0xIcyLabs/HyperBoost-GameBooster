using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HyperBoost
{
    internal sealed class JunkRow : Control
    {
        internal enum RowState { Scanning, Ready, Cleaning, Cleaned }

        private RowState state = RowState.Ready;
        private bool selected = true;
        private bool hover, pressed;
        private int tick;
        private string name = "";
        private string sizeText = "";
        private long cleanedBytes;
        private long lastBytes;
        private AppLanguage language = AppLanguage.English;
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 40 };
        private readonly Font fontName = new Font("Segoe UI Semibold", 10F);
        private readonly Font fontSize = new Font("Consolas", 10F, FontStyle.Bold);
        private readonly Font fontPhase = new Font("Consolas", 8F, FontStyle.Bold);

        internal JunkRow()
        {
            Size = new Size(480, 48);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            timer.Tick += delegate { tick++; Invalidate(); };
            timer.Start();
        }

        internal AppLanguage Language { set { language = value; Invalidate(); } }
        internal bool Selected
        {
            get { return selected; }
            set { if (!Busy) { selected = value; Invalidate(); } }
        }
        internal bool Busy { get { return state == RowState.Scanning || state == RowState.Cleaning; } }

        internal void SetName(string value) { name = value; Invalidate(); }
        internal void StartScan() { state = RowState.Scanning; sizeText = ""; Invalidate(); }
        internal void FinishScan(long bytes) { state = RowState.Ready; lastBytes = bytes; sizeText = JunkCleaner.FormatBytes(bytes); Invalidate(); }
        internal void StartClean() { state = RowState.Cleaning; Invalidate(); }
        internal void FinishClean(CleanResult result) { state = RowState.Cleaned; cleanedBytes = result.FreedBytes; sizeText = "-"; lastBytes = 0; Invalidate(); }
        internal long LastBytes { get { return lastBytes; } }

        protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); fontName.Dispose(); fontSize.Dispose(); fontPhase.Dispose(); } base.Dispose(disposing); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnClick(EventArgs e)
        {
            if (!Busy) { selected = !selected; Invalidate(); }
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var r = ClientRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool dim = !selected;
            bool down = pressed && !dim;
            int yShift = down ? 1 : 0;

            Color top = down ? Color.FromArgb(6, 22, 29) : (hover && !dim ? Color.FromArgb(14, 43, 55) : Color.FromArgb(11, 36, 47));
            using (var bg = new LinearGradientBrush(r, top, Color.FromArgb(4, 14, 20), LinearGradientMode.Vertical)) g.FillRectangle(bg, r);
            using (var line = new Pen(Color.FromArgb(16, 0, 232, 179), 1))
                for (int x = -r.Height; x < r.Width; x += 26) g.DrawLine(line, x, r.Height, x + r.Height, 0);

            int pulse = Busy ? (int)(55 * (1 + Math.Sin(tick * 0.18))) : 0;
            byte borderAlpha = Busy ? (byte)Math.Min(255, 185 + pulse) : (byte)(dim ? 60 : 120);
            using (var pen = new Pen(Color.FromArgb(borderAlpha, 0, 232, 179), 2f)) g.DrawRectangle(pen, 1, 1, r.Width - 3, r.Height - 3);
            if (hover && !dim) using (var glow = new Pen(Color.FromArgb(26, 0, 232, 179), 3f)) g.DrawRectangle(glow, 4, 4, r.Width - 9, r.Height - 9);

            using (var pen = new Pen(dim ? Color.FromArgb(70, 0, 140, 110) : Color.FromArgb(0, 255, 199), 2f))
            {
                int m = 5, len = 10, w = r.Width, hh = r.Height;
                g.DrawLine(pen, m, m, m + len, m); g.DrawLine(pen, m, m, m, m + len);
                g.DrawLine(pen, w - m, m, w - m - len, m); g.DrawLine(pen, w - m, m, w - m, m + len);
                g.DrawLine(pen, m, hh - m, m + len, hh - m); g.DrawLine(pen, m, hh - m, m, hh - m - len);
                g.DrawLine(pen, w - m, hh - m, w - m - len, hh - m); g.DrawLine(pen, w - m, hh - m, w - m, hh - m - len);
            }

            int glyphAlpha = dim ? 90 : (int)(170 + 60 * Math.Abs(Math.Sin(tick * 0.22)));
            using (var pen = new Pen(Color.FromArgb(glyphAlpha, 0, 255, 206), 2f))
            {
                int gx = 24, gy = r.Height / 2 + yShift;
                g.DrawLine(pen, gx - 5, gy - 6, gx + 5, gy - 6);
                g.DrawLine(pen, gx - 4, gy - 6, gx - 3, gy + 2);
                g.DrawLine(pen, gx + 4, gy - 6, gx + 3, gy + 2);
                g.DrawRectangle(pen, gx - 3, gy + 2, 6, 5);
                g.DrawLine(pen, gx, gy - 9, gx, gy - 6);
            }

            Color nameColor = dim ? Color.FromArgb(90, 120, 128) : Color.FromArgb(224, 255, 248);
            TextRenderer.DrawText(g, name, fontName, new Rectangle(44, yShift, r.Width - 210, r.Height), nameColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            int rightX = r.Width - 156, rightW = 140;
            if (state == RowState.Scanning)
            {
                TextRenderer.DrawText(g, Texts.T(language, "scanningRow") + new string('.', tick / 9 % 3 + 1), fontPhase, new Rectangle(rightX, yShift, rightW, r.Height), Color.FromArgb(115, 241, 209), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
            else if (state == RowState.Cleaning)
            {
                TextRenderer.DrawText(g, Texts.T(language, "cleaningRow") + new string('.', tick / 9 % 3 + 1), fontPhase, new Rectangle(rightX, yShift, rightW, r.Height), Color.FromArgb(255, 214, 130), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
            else if (state == RowState.Cleaned && cleanedBytes > 0)
            {
                int flash = Math.Min(255, 160 + (int)(95 * Math.Abs(Math.Sin(tick * 0.15))));
                TextRenderer.DrawText(g, "-" + JunkCleaner.FormatBytes(cleanedBytes), fontSize, new Rectangle(rightX, yShift, rightW, r.Height), Color.FromArgb(flash, 74, 245, 174), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
            else
            {
                Color sizeColor = dim ? Color.FromArgb(90, 120, 128) : Color.FromArgb(0, 255, 199);
                TextRenderer.DrawText(g, sizeText, fontSize, new Rectangle(rightX, yShift, rightW, r.Height), sizeColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }

            if (selected && (state == RowState.Ready || state == RowState.Cleaned))
            {
                using (var pen = new Pen(Color.FromArgb(0, 255, 199), 2f))
                {
                    int cx = r.Width - 172, cy = r.Height / 2 + yShift;
                    g.DrawLine(pen, cx - 5, cy, cx - 1, cy + 4);
                    g.DrawLine(pen, cx - 1, cy + 4, cx + 6, cy - 4);
                }
            }

            int trackX = 44, trackW = r.Width - 44 - 16;
            using (var track = new SolidBrush(Color.FromArgb(10, 35, 43))) g.FillRectangle(track, trackX, r.Height - 8, trackW, 3);
            if (Busy)
            {
                double sweep = (tick % 26) / 26.0;
                int sx = trackX + (int)((trackW - 26) * sweep);
                using (var scan = new SolidBrush(Color.FromArgb(80, 0, 255, 210))) g.FillRectangle(scan, sx, r.Height - 8, 26, 3);
            }
            else if (selected)
            {
                using (var fill = new LinearGradientBrush(new Rectangle(trackX, 0, Math.Max(trackW, 2), 3), Color.FromArgb(0, 150, 120), Color.FromArgb(0, 255, 199), LinearGradientMode.Horizontal)) g.FillRectangle(fill, trackX, r.Height - 8, trackW, 3);
            }
        }
    }
}
