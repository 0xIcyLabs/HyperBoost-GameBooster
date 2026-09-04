using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HyperBoost
{
    internal sealed class OptimizationButton : Control
    {
        private enum BtnState { Idle, Running, Done }
        private BtnState state = BtnState.Idle;
        private double progress;
        private string phaseText = "";
        private string resultText = "";
        private string idleLabel = "OPTIMIZATION";
        private string runningLabel = "OPTIMIZING";
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 40 };
        private Font fontBig = new Font("Consolas", 14F, FontStyle.Bold);
        private Font fontSmall = new Font("Consolas", 8F, FontStyle.Bold);
        private Font fontResult = new Font("Consolas", 9F, FontStyle.Bold);
        private int tick;
        private bool hover, pressed;

        internal OptimizationButton()
        {
            Size = new Size(400, 62);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 13F);
            timer.Tick += delegate { tick++; Invalidate(); };
            timer.Start();
        }
        internal bool SmallMode
        {
            set
            {
                if (!value) return;
                Font.Dispose();
                Font = new Font("Segoe UI Semibold", 11F);
                fontBig.Dispose(); fontBig = new Font("Consolas", 11F, FontStyle.Bold);
                fontSmall.Dispose(); fontSmall = new Font("Consolas", 7F, FontStyle.Bold);
                fontResult.Dispose(); fontResult = new Font("Consolas", 8F, FontStyle.Bold);
                small = true;
            }
        }
        private bool small;
        internal bool IsBusy { get { return state == BtnState.Running; } }
        internal void SetLabels(string idle, string running) { idleLabel = idle; runningLabel = running; Invalidate(); }
        internal void Begin(string phase) { state = BtnState.Running; progress = 0; phaseText = phase; resultText = ""; tick = 0; Cursor = Cursors.Default; Invalidate(); }
        internal void SetProgress(double p, string phase) { progress = p < 0 ? 0 : (p > 1 ? 1 : p); phaseText = phase; }
        internal void Finish(string result) { state = BtnState.Done; resultText = result; phaseText = ""; Cursor = Cursors.Hand; Invalidate(); }
        protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); fontBig.Dispose(); fontSmall.Dispose(); fontResult.Dispose(); } base.Dispose(disposing); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left && state != BtnState.Running) { pressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnClick(EventArgs e) { if (state != BtnState.Running) base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var r = ClientRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool running = state == BtnState.Running;
            bool down = pressed && !running;
            int yShift = down ? 1 : 0;

            // Flat surface (no scan-lines); slightly raised accent-tinted body.
            Color top = down ? Ui.CardDown : (running ? Color.FromArgb(16, 40, 50) : (hover ? Ui.CardHover : Ui.Card));
            Color bottom = down ? Color.FromArgb(9, 18, 24) : Ui.Card;
            using (var bg = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical)) g.FillRectangle(bg, 0, yShift, r.Width, r.Height);

            // 1px accent border; soft glow only while running.
            int pulse = running ? (int)(40 * (1 + Math.Sin(tick * 0.18))) : 0;
            using (var pen = new Pen(Color.FromArgb(Math.Min(255, 235 + pulse), Ui.Accent), down ? 1f : 1.5f)) g.DrawRectangle(pen, 1, 1, r.Width - 3, r.Height - 3);
            if (running) using (var glow = new Pen(Color.FromArgb(44, Ui.Accent), 3f)) g.DrawRectangle(glow, 4, 4, r.Width - 9, r.Height - 9);

            // Static bolt glyph (subtle), left of the label.
            float cy = r.Height / 2f + yShift;
            var bolt = new[] { new PointF(7f, -15f), new PointF(-6f, 2f), new PointF(-1f, 2f), new PointF(-7f, 15f), new PointF(6f, -2f), new PointF(1f, -2f) };
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(bolt);
                using (var mx = new Matrix()) { mx.Translate(small ? 24 : 36, cy); mx.Scale(small ? 0.5f : 0.85f, small ? 0.5f : 0.85f); path.Transform(mx); }
                int boltAlpha = running ? 200 + (int)(40 * Math.Abs(Math.Sin(tick * 0.22))) : 225;
                using (var brush = new SolidBrush(Color.FromArgb(Math.Min(255, boltAlpha), Ui.Accent))) g.FillPath(brush, path);
            }

            string main = running ? runningLabel + new string('.', tick / 9 % 3 + 1) : idleLabel;
            Color textColor = running ? Color.FromArgb(160, 250, 228) : Ui.TextPrimary;
            int textX = small ? 40 : 60;
            int rightReserve = small ? ((running || state == BtnState.Done) ? 84 : 30) : ((running || state == BtnState.Done) ? 140 : 40);
            TextRenderer.DrawText(g, main, Font, new Rectangle(textX, yShift, r.Width - textX - rightReserve, r.Height), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // Sheen sweep: a soft moving highlight across the surface when idle/hover.
            if (!running && state != BtnState.Done && (hover || true))
            {
                double pos = (tick % 90) / 90.0;                       // loops every ~3.6s
                int sheenW = Math.Max(90, r.Width / 3);
                int sx = (int)(-sheenW + pos * (r.Width + sheenW * 2));
                using (var sheen = new LinearGradientBrush(new Rectangle(sx, yShift, sheenW, r.Height),
                    Color.Transparent, Color.FromArgb(hover ? 44 : 22, Ui.Accent), LinearGradientMode.Horizontal))
                {
                    sheen.SetBlendTriangularShape(0.5f, 1f);
                    g.FillRectangle(sheen, sx, yShift, sheenW, r.Height);
                }
            }

            if (running)
            {
                int phaseY = small ? 4 : 8;
                TextRenderer.DrawText(g, phaseText.ToUpper(), fontSmall, new Rectangle(r.Width - rightReserve - 4, phaseY, rightReserve - 8, 16), Color.FromArgb(150, 222, 204), TextFormatFlags.Right);
                TextRenderer.DrawText(g, ((int)(progress * 100)).ToString() + "%", fontBig, new Rectangle(r.Width - rightReserve - 4, small ? 16 : 24, rightReserve - 8, 24), Ui.Accent, TextFormatFlags.Right);
                int trackX = textX, trackW = r.Width - textX - 16;
                using (var track = new SolidBrush(Ui.Divider)) g.FillRectangle(track, trackX, r.Height - 12, trackW, 4);
                int fillW = (int)(trackW * progress);
                if (fillW > 0)
                {
                    using (var fill = new SolidBrush(Ui.Accent)) g.FillRectangle(fill, trackX, r.Height - 12, fillW, 4);
                    // moving highlight on the fill
                    double sweep = (tick % 30) / 30.0;
                    int sx = trackX + (int)((fillW - 24) * sweep);
                    if (sx >= trackX && sx + 24 <= trackX + fillW)
                        using (var scan = new SolidBrush(Color.FromArgb(120, Color.White))) g.FillRectangle(scan, sx, r.Height - 12, 24, 4);
                }
            }
            else if (state == BtnState.Done)
            {
                TextRenderer.DrawText(g, resultText, fontResult, new Rectangle(r.Width - rightReserve - 4, 0, rightReserve - 8, r.Height), Color.FromArgb(130, 235, 205), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
            else
            {
                using (var pen = new Pen(Ui.Accent, 2f))
                {
                    int ax = r.Width - (small ? 20 : 30), ay = r.Height / 2;
                    g.DrawLine(pen, ax - 7, ay - 6, ax, ay);
                    g.DrawLine(pen, ax, ay, ax - 7, ay + 6);
                }
            }
        }
    }
}
