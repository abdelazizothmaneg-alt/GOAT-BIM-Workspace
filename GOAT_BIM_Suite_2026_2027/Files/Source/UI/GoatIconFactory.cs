using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ORTools.UI
{
    /// <summary>
    /// High-DPI Vector Icon &amp; Badge Renderer for GOAT BIM Ribbon buttons (32x32 &amp; 16x16) and Window Headers (48x48).
    /// Ensures cohesive Dark Navy (#0F172A) + Emerald (#10B981) + Sky (#38BDF8) identity across all 16 tools.
    /// </summary>
    public static class GoatIconFactory
    {
        private static readonly Brush NavyBg = Freeze(new SolidColorBrush(Color.FromRgb(15, 23, 42)));       // #0F172A
        private static readonly Brush NavyInner = Freeze(new SolidColorBrush(Color.FromRgb(30, 41, 59)));    // #1E293B
        private static readonly Brush BorderStroke = Freeze(new SolidColorBrush(Color.FromRgb(56, 189, 248))); // #38BDF8
        private static readonly Brush EmeraldBrush = Freeze(new SolidColorBrush(Color.FromRgb(16, 185, 129))); // #10B981
        private static readonly Brush SkyBrush = Freeze(new SolidColorBrush(Color.FromRgb(56, 189, 248)));     // #38BDF8
        private static readonly Brush AmberBrush = Freeze(new SolidColorBrush(Color.FromRgb(245, 158, 11)));   // #F59E0B
        private static readonly Brush WhiteBrush = Freeze(new SolidColorBrush(Colors.White));
        private static readonly Brush MutedBrush = Freeze(new SolidColorBrush(Color.FromRgb(148, 163, 184)));

        private static T Freeze<T>(T freezable) where T : Freezable
        {
            if (freezable.CanFreeze) freezable.Freeze();
            return freezable;
        }

        public static BitmapSource CreateRibbonIcon(string toolKey, int size)
        {
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                double s = size;
                double radius = Math.Max(3.0, s * 0.18);
                Pen borderPen = new Pen(BorderStroke, Math.Max(1.0, s * 0.045));

                // Background rounded badge
                dc.DrawRoundedRectangle(NavyBg, borderPen, new Rect(0.5, 0.5, s - 1, s - 1), radius, radius);

                // Normalize drawing to 32x32 coordinate space
                dc.PushTransform(new ScaleTransform(s / 32.0, s / 32.0));
                DrawToolGlyph32(dc, toolKey?.ToUpperInvariant() ?? "GOAT");
                dc.Pop();
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        public static BitmapSource CreateHeaderBadge(string subtitle)
        {
            int size = 48;
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                Pen whiteBorder = new Pen(WhiteBrush, 1.5);
                dc.DrawRoundedRectangle(Brushes.Black, whiteBorder, new Rect(1, 1, size - 2, size - 2), 7, 7);

                Typeface tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.ExtraBold, FontStretches.Normal);

                FormattedText topTxt = new FormattedText(
                    "GOAT",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    tf,
                    10.5,
                    WhiteBrush,
                    1.25);
                dc.DrawText(topTxt, new Point((size - topTxt.Width) / 2.0, 10));

                string sub = string.IsNullOrWhiteSpace(subtitle) ? "BIM" : subtitle.ToUpperInvariant();
                double subFontSize = sub.Length > 7 ? 7.0 : (sub.Length > 5 ? 8.0 : 9.5);
                FormattedText botTxt = new FormattedText(
                    sub,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    tf,
                    subFontSize,
                    EmeraldBrush,
                    1.25);
                dc.DrawText(botTxt, new Point((size - botTxt.Width) / 2.0, 25));
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        private static void DrawToolGlyph32(DrawingContext dc, string key)
        {
            Pen whitePen = new Pen(WhiteBrush, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            Pen skyPen = new Pen(SkyBrush, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            Pen emeraldPen = new Pen(EmeraldBrush, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            Pen amberPen = new Pen(AmberBrush, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

            switch (key)
            {
                case "LEVELS":
                    // Datum lines + elevation triangle markers
                    dc.DrawLine(whitePen, new Point(6, 10), new Point(22, 10));
                    dc.DrawLine(skyPen, new Point(6, 17), new Point(22, 17));
                    dc.DrawLine(emeraldPen, new Point(6, 24), new Point(22, 24));
                    dc.DrawEllipse(EmeraldBrush, null, new Point(25, 10), 2.5, 2.5);
                    dc.DrawEllipse(SkyBrush, null, new Point(25, 17), 2.5, 2.5);
                    dc.DrawEllipse(AmberBrush, null, new Point(25, 24), 2.5, 2.5);
                    break;

                case "GRIDS":
                    // Orthogonal + radial grid lines with bubbles
                    dc.DrawLine(skyPen, new Point(11, 8), new Point(11, 26));
                    dc.DrawLine(skyPen, new Point(21, 8), new Point(21, 26));
                    dc.DrawLine(whitePen, new Point(6, 13), new Point(26, 13));
                    dc.DrawLine(whitePen, new Point(6, 22), new Point(26, 22));
                    dc.DrawEllipse(EmeraldBrush, null, new Point(11, 6), 2.5, 2.5);
                    dc.DrawEllipse(EmeraldBrush, null, new Point(21, 6), 2.5, 2.5);
                    break;

                case "PARAMETER":
                    // Sliders / tuning controls
                    dc.DrawLine(whitePen, new Point(7, 10), new Point(25, 10));
                    dc.DrawLine(whitePen, new Point(7, 16), new Point(25, 16));
                    dc.DrawLine(whitePen, new Point(7, 22), new Point(25, 22));
                    dc.DrawEllipse(EmeraldBrush, null, new Point(12, 10), 3.0, 3.0);
                    dc.DrawEllipse(SkyBrush, null, new Point(20, 16), 3.0, 3.0);
                    dc.DrawEllipse(AmberBrush, null, new Point(14, 22), 3.0, 3.0);
                    break;

                case "SHEETLINK":
                    // Excel table + bidirectional sync arrows
                    dc.DrawRoundedRectangle(NavyInner, emeraldPen, new Rect(6, 7, 20, 18), 2, 2);
                    dc.DrawLine(emeraldPen, new Point(6, 13), new Point(26, 13));
                    dc.DrawLine(whitePen, new Point(14, 13), new Point(14, 25));
                    dc.DrawLine(skyPen, new Point(18, 17), new Point(23, 17));
                    dc.DrawLine(amberPen, new Point(18, 21), new Point(23, 21));
                    break;

                case "RENUMBER":
                    // 1 -> 2 -> 3 badge sequence
                    DrawBadgeText(dc, "1·2·3", 16, 11, 8.5, EmeraldBrush);
                    dc.DrawLine(skyPen, new Point(7, 22), new Point(25, 22));
                    dc.DrawLine(skyPen, new Point(21, 19), new Point(25, 22));
                    dc.DrawLine(skyPen, new Point(21, 25), new Point(25, 22));
                    break;

                case "BOQ":
                    // Bar chart + cost/quantity check
                    dc.DrawRectangle(SkyBrush, null, new Rect(7, 17, 4, 9));
                    dc.DrawRectangle(EmeraldBrush, null, new Rect(14, 12, 4, 14));
                    dc.DrawRectangle(AmberBrush, null, new Rect(21, 7, 4, 19));
                    break;

                case "FINISHES":
                    // Room outer core + inner finish wall + floor layer
                    dc.DrawRectangle(null, whitePen, new Rect(6, 6, 20, 20));
                    dc.DrawRectangle(null, skyPen, new Rect(9.5, 9.5, 13, 13));
                    dc.DrawLine(amberPen, new Point(9.5, 22.5), new Point(22.5, 22.5));
                    dc.DrawEllipse(EmeraldBrush, null, new Point(16, 16), 2.5, 2.5);
                    break;

                case "ARCH":
                    // Classical architectural arch + keystone
                    PathGeometry archGeo = new PathGeometry();
                    PathFigure pf = new PathFigure { StartPoint = new Point(8, 25), IsClosed = false };
                    pf.Segments.Add(new LineSegment(new Point(8, 15), true));
                    pf.Segments.Add(new ArcSegment(new Point(24, 15), new Size(8, 8), 0, false, SweepDirection.Clockwise, true));
                    pf.Segments.Add(new LineSegment(new Point(24, 25), true));
                    archGeo.Figures.Add(pf);
                    dc.DrawGeometry(null, skyPen, archGeo);
                    dc.DrawRectangle(AmberBrush, null, new Rect(14.5, 5.5, 3, 3.5));
                    break;

                case "CEILING":
                    // 60x60 ceiling grid + lighting fixture nodes
                    dc.DrawRectangle(null, whitePen, new Rect(6, 6, 20, 20));
                    dc.DrawLine( new Pen(MutedBrush, 1.0), new Point(16, 6), new Point(16, 26));
                    dc.DrawLine( new Pen(MutedBrush, 1.0), new Point(6, 16), new Point(26, 16));
                    dc.DrawEllipse(AmberBrush, null, new Point(11, 11), 2.2, 2.2);
                    dc.DrawEllipse(AmberBrush, null, new Point(21, 11), 2.2, 2.2);
                    dc.DrawEllipse(EmeraldBrush, null, new Point(11, 21), 2.2, 2.2);
                    dc.DrawEllipse(EmeraldBrush, null, new Point(21, 21), 2.2, 2.2);
                    break;

                case "D_VIEW":
                    // Door elevation + swing arc
                    dc.DrawRectangle(null, whitePen, new Rect(8, 6, 11, 20));
                    dc.DrawEllipse(AmberBrush, null, new Point(16.5, 16), 1.3, 1.3);
                    dc.DrawLine(emeraldPen, new Point(22, 8), new Point(26, 8));
                    dc.DrawLine(skyPen, new Point(22, 14), new Point(26, 14));
                    break;

                case "W_VIEW":
                    // 4-pane window icon
                    dc.DrawRectangle(null, skyPen, new Rect(7, 7, 18, 18));
                    dc.DrawLine(whitePen, new Point(16, 7), new Point(16, 25));
                    dc.DrawLine(whitePen, new Point(7, 16), new Point(25, 16));
                    break;

                case "ROOM":
                    // Room elevation target marker (N/S/E/W arrows)
                    dc.DrawEllipse(NavyInner, whitePen, new Point(16, 16), 6, 6);
                    dc.DrawLine(emeraldPen, new Point(16, 5), new Point(16, 10));
                    dc.DrawLine(emeraldPen, new Point(16, 22), new Point(16, 27));
                    dc.DrawLine(skyPen, new Point(5, 16), new Point(10, 16));
                    dc.DrawLine(skyPen, new Point(22, 16), new Point(27, 16));
                    break;

                case "DIM":
                    // Dimension line with witnesses and ticks
                    dc.DrawLine(whitePen, new Point(7, 9), new Point(7, 23));
                    dc.DrawLine(whitePen, new Point(25, 9), new Point(25, 23));
                    dc.DrawLine(emeraldPen, new Point(7, 16), new Point(25, 16));
                    dc.DrawLine(amberPen, new Point(5, 18), new Point(9, 14));
                    dc.DrawLine(amberPen, new Point(23, 18), new Point(27, 14));
                    break;

                case "ANNOTATE":
                    // Tag head + leader elbow
                    dc.DrawRoundedRectangle(NavyInner, skyPen, new Rect(9, 7, 16, 10), 2, 2);
                    dc.DrawLine(emeraldPen, new Point(13, 17), new Point(9, 24));
                    dc.DrawEllipse(AmberBrush, null, new Point(8, 25), 2.0, 2.0);
                    break;

                case "ALIGNER":
                    // Viewport grid alignment blocks
                    dc.DrawRectangle(SkyBrush, null, new Rect(7, 8, 8, 7));
                    dc.DrawRectangle(EmeraldBrush, null, new Rect(17, 8, 8, 7));
                    dc.DrawRectangle(EmeraldBrush, null, new Rect(7, 17, 8, 7));
                    dc.DrawRectangle(AmberBrush, null, new Rect(17, 17, 8, 7));
                    break;

                case "SHEETS":
                    // Sheet titleblock stack
                    dc.DrawRectangle(NavyInner, whitePen, new Rect(6, 8, 20, 16));
                    dc.DrawLine(emeraldPen, new Point(20, 8), new Point(20, 24));
                    dc.DrawLine(skyPen, new Point(9, 13), new Point(17, 13));
                    dc.DrawLine(skyPen, new Point(9, 18), new Point(17, 18));
                    break;

                case "ABOUT":
                default:
                    DrawBadgeText(dc, "GOAT", 16, 10, 7.5, WhiteBrush);
                    DrawBadgeText(dc, "BIM", 16, 19, 7.5, EmeraldBrush);
                    break;
            }
        }

        private static void DrawBadgeText(DrawingContext dc, string text, double centerX, double topY, double fontSize, Brush brush)
        {
            Typeface tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.ExtraBold, FontStretches.Normal);
            FormattedText ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                tf,
                fontSize,
                brush,
                1.25);
            dc.DrawText(ft, new Point(centerX - ft.Width / 2.0, topY));
        }
    }
}
