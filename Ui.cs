using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HyperBoost
{
    // Central design tokens: one palette, one spacing scale, one type scale.
    // Everything in the UI reads from here so the whole app looks cohesive.
    internal static class Ui
    {
        // Surfaces (dark, blue-tinted, low contrast between levels)
        internal static readonly Color Page      = Color.FromArgb(6, 12, 17);
        internal static readonly Color Panel     = Color.FromArgb(10, 19, 26);
        internal static readonly Color Card      = Color.FromArgb(14, 25, 33);
        internal static readonly Color CardHover = Color.FromArgb(19, 34, 44);
        internal static readonly Color CardDown  = Color.FromArgb(11, 20, 27);
        internal static readonly Color Divider   = Color.FromArgb(27, 46, 56);

        // Text
        internal static readonly Color TextPrimary  = Color.FromArgb(224, 242, 238);
        internal static readonly Color TextBody     = Color.FromArgb(150, 179, 186);
        internal static readonly Color TextMuted    = Color.FromArgb(99, 128, 138);
        internal static readonly Color TextOnAccent = Color.FromArgb(3, 16, 14);

        // Single accent (teal). Semantic colors reserved strictly for meaning.
        internal static readonly Color Accent   = Color.FromArgb(0, 232, 179);
        internal static readonly Color AccentDim= Color.FromArgb(0, 168, 130);
        internal static readonly Color Danger   = Color.FromArgb(255, 108, 128);
        internal static readonly Color Warn     = Color.FromArgb(255, 180, 81);

        // Spacing scale (px)
        internal const int S1 = 4, S2 = 8, S3 = 12, S4 = 16, S5 = 24, S6 = 32, S7 = 48;
        internal const int Radius = 8;

        internal static Font Body(float size, bool semibold = false)
        {
            return new Font("Segoe UI", size, semibold ? FontStyle.Bold : FontStyle.Regular);
        }
        internal static Font Mono(float size, bool bold = false)
        {
            return new Font("Consolas", size, bold ? FontStyle.Bold : FontStyle.Regular);
        }

        internal static GraphicsPath Rounded(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        internal static void FillRounded(Graphics g, RectangleF r, float radius, Brush brush)
        {
            using (var path = Rounded(r, radius)) g.FillPath(brush, path);
        }
        internal static void DrawRounded(Graphics g, RectangleF r, float radius, Pen pen)
        {
            using (var path = Rounded(r, radius)) g.DrawPath(pen, path);
        }
    }

    // Flat, modern tile button for secondary actions (JUNK / TWEAKS / AUTO / REVERT).
    // Descends from Button so existing .Click wiring is unchanged.
    internal sealed class TileButton : Button
    {
        private bool hover, down;

        internal TileButton(string glyph, Color accent)
        {
            Glyph = glyph; Accent = accent;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Ui.TextPrimary;
            Font = Ui.Body(9.5f, true);
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleCenter;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
        }

        internal string Glyph { get; set; }
        internal Color Accent { get; set; }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            var r = new RectangleF(0, 0, Width, Height);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Ui.Page);

            bool enabled = Enabled;
            Color bg = down ? Ui.CardDown : (hover ? Ui.CardHover : Ui.Card);
            byte alpha = (byte)(enabled ? 255 : 130);
            using (var bgBrush = new SolidBrush(Color.FromArgb(alpha, bg))) Ui.FillRounded(g, r, Ui.Radius, bgBrush);
            using (var pen = new Pen(Color.FromArgb(enabled ? Accent.A : 120, Accent), enabled ? 1f : 1f)) Ui.DrawRounded(g, r, Ui.Radius, pen);

            if (hover && enabled)
                using (var bar = new SolidBrush(Accent)) Ui.FillRounded(g, new RectangleF(5, 7, 3, Height - 14), 1.5f, bar);

            var tf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            using (var b = new SolidBrush(Color.FromArgb(enabled ? 255 : 150, ForeColor))) g.DrawString(Text, Font, b, r, tf);
        }
    }
}
