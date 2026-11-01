using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Commands;

namespace ORTools.UI
{
    public class ArchConfigData
    {
        public int ArchTypeIndex { get; set; }
        public string ArchTypeName { get; set; } = "Semicircular";
        public double SpanMm { get; set; } = 1800;
        public double SpringingHeightMm { get; set; } = 1500;
        public double RiseMm { get; set; } = 900;
        public double DepthMm { get; set; } = 250;
        public double RingThicknessMm { get; set; } = 150;
        public bool CutHostWall { get; set; } = true;
    }

    public class WardrobeConfigData
    {
        public double WidthMm { get; set; } = 2400;
        public double HeightMm { get; set; } = 2400;
        public double DepthMm { get; set; } = 600;
        public double PlinthMm { get; set; } = 80;
        public double PanelThicknessMm { get; set; } = 18;
        public int BayCount { get; set; } = 3;
        public int ShelvesPerBay { get; set; } = 4;
        public int DoorStyleIndex { get; set; } = 2;
    }

    public partial class GoatArchWindow : Window
    {
        private readonly GoatArchExternalEventHandler _handler;
        private readonly ExternalEvent _extEvent;
        private int _activeTabIndex = 0;
        private bool _isLoaded = false;

        public GoatArchWindow(GoatArchExternalEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            Loaded += (s, e) =>
            {
                _isLoaded = true;
                RenderPreview();
            };
        }

        public ArchConfigData CollectArchParameters()
        {
            double.TryParse(TxtArchSpan.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double span);
            double.TryParse(TxtArchSpringing.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double spring);
            double.TryParse(TxtArchRise.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double rise);
            double.TryParse(TxtArchDepth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double depth);
            double.TryParse(TxtArchRing.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ring);

            int idx = CbArchType.SelectedIndex >= 0 ? CbArchType.SelectedIndex : 0;
            string name = (CbArchType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Semicircular";

            return new ArchConfigData
            {
                ArchTypeIndex = idx,
                ArchTypeName = name.Split('(')[0].Trim(),
                SpanMm = Math.Max(400, span),
                SpringingHeightMm = Math.Max(200, spring),
                RiseMm = Math.Max(150, rise),
                DepthMm = Math.Max(50, depth),
                RingThicknessMm = Math.Max(40, ring),
                CutHostWall = ChkArchCutWall.IsChecked == true
            };
        }

        public WardrobeConfigData CollectWardrobeParameters()
        {
            double.TryParse(TxtWardWidth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double w);
            double.TryParse(TxtWardHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double h);
            double.TryParse(TxtWardDepth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double d);
            double.TryParse(TxtWardPlinth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double p);
            double.TryParse(TxtWardPanelThk.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double thk);
            int.TryParse(TxtWardBays.Text, out int bays);
            int.TryParse(TxtWardShelves.Text, out int shelves);

            return new WardrobeConfigData
            {
                WidthMm = Math.Max(400, w),
                HeightMm = Math.Max(600, h),
                DepthMm = Math.Max(250, d),
                PlinthMm = Math.Max(0, p),
                PanelThicknessMm = Math.Clamp(thk, 10, 50),
                BayCount = Math.Clamp(bays, 1, 12),
                ShelvesPerBay = Math.Clamp(shelves, 0, 15),
                DoorStyleIndex = CbWardDoorStyle.SelectedIndex >= 0 ? CbWardDoorStyle.SelectedIndex : 2
            };
        }

        public void UpdateStatus(string text, bool isError)
        {
            TxtStatus.Text = text;
            TxtStatus.Foreground = isError
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
        }

        private void TabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            _activeTabIndex = btn.Tag?.ToString() == "1" ? 1 : 0;

            bool isArch = _activeTabIndex == 0;
            BtnTabArch.Background = isArch ? Brushes.White : new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249));
            BtnTabArch.Foreground = isArch ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            BtnTabArch.BorderBrush = isArch ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)) : Brushes.Transparent;

            BtnTabWardrobe.Background = !isArch ? Brushes.White : new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249));
            BtnTabWardrobe.Foreground = !isArch ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            BtnTabWardrobe.BorderBrush = !isArch ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)) : Brushes.Transparent;

            PanelArch.Visibility = isArch ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            PanelWardrobe.Visibility = !isArch ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            TxtPreviewTitle.Text = isArch
                ? "ELEVATION PREVIEW: Parametric Arch Geometry"
                : "ELEVATION PREVIEW: Parametric Wardrobe & Joinery";
            BtnGeneratePrimary.Content = isArch
                ? "🚀 Create Arch in Model"
                : "🚀 Create Wardrobe in Model";

            RenderPreview();
        }

        private void ArchInput_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (CbArchType.SelectedIndex == 0 && double.TryParse(TxtArchSpan.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double sp))
            {
                TxtArchRise.Text = (sp * 0.5).ToString("0", CultureInfo.InvariantCulture);
            }
            RenderPreview();
        }

        private void ArchText_Changed(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            RenderPreview();
        }

        private void WardrobeText_Changed(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            RenderPreview();
        }

        private void CanvasArchPreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isLoaded) return;
            RenderPreview();
        }

        private void BtnGeneratePrimary_Click(object sender, RoutedEventArgs e)
        {
            _handler.CurrentAction = _activeTabIndex == 0
                ? GoatArchAction.CreateArchInWall
                : GoatArchAction.CreateParametricWardrobe;
            _extEvent.Raise();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RenderPreview()
        {
            if (CanvasArchPreview == null) return;
            CanvasArchPreview.Children.Clear();

            double cw = CanvasArchPreview.ActualWidth;
            double ch = CanvasArchPreview.ActualHeight;
            if (cw < 50 || ch < 50) return;

            if (_activeTabIndex == 0)
            {
                var cfg = CollectArchParameters();
                double totalW = cfg.SpanMm + cfg.RingThicknessMm * 2.0;
                double totalH = cfg.SpringingHeightMm + cfg.RiseMm + cfg.RingThicknessMm;

                double scale = Math.Min((cw - 60) / totalW, (ch - 60) / totalH);
                double centerX = cw * 0.5;
                double baseY = ch - 30;

                var innerPts = SampleArchCurvePoints(cfg.ArchTypeIndex, cfg.SpanMm, cfg.SpringingHeightMm, cfg.RiseMm, 32);
                var outerPts = SampleArchCurvePoints(cfg.ArchTypeIndex, cfg.SpanMm + cfg.RingThicknessMm * 2.0, cfg.SpringingHeightMm, cfg.RiseMm + cfg.RingThicknessMm, 32);

                Polygon poly = new Polygon
                {
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(190, 56, 189, 248)),
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                    StrokeThickness = 2
                };

                foreach (var p in innerPts)
                {
                    poly.Points.Add(new System.Windows.Point(centerX + p.X * scale, baseY - p.Y * scale));
                }
                for (int i = outerPts.Count - 1; i >= 0; i--)
                {
                    var p = outerPts[i];
                    poly.Points.Add(new System.Windows.Point(centerX + p.X * scale, baseY - p.Y * scale));
                }

                CanvasArchPreview.Children.Add(poly);

                // Floor datum line
                var floorLine = new System.Windows.Shapes.Line
                {
                    X1 = 20, Y1 = baseY, X2 = cw - 20, Y2 = baseY,
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                    StrokeThickness = 1.5
                };
                CanvasArchPreview.Children.Add(floorLine);
            }
            else
            {
                var cfg = CollectWardrobeParameters();
                double scale = Math.Min((cw - 60) / cfg.WidthMm, (ch - 60) / cfg.HeightMm);
                double drawW = cfg.WidthMm * scale;
                double drawH = cfg.HeightMm * scale;
                double startX = (cw - drawW) * 0.5;
                double startY = (ch - drawH) * 0.5;
                double plinthH = cfg.PlinthMm * scale;

                // Outer frame
                var frame = new System.Windows.Shapes.Rectangle
                {
                    Width = drawW,
                    Height = drawH,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                    StrokeThickness = 2.5
                };
                Canvas.SetLeft(frame, startX);
                Canvas.SetTop(frame, startY);
                CanvasArchPreview.Children.Add(frame);

                // Plinth
                var plinth = new System.Windows.Shapes.Rectangle
                {
                    Width = drawW,
                    Height = Math.Max(4, plinthH),
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(plinth, startX);
                Canvas.SetTop(plinth, startY + drawH - plinthH);
                CanvasArchPreview.Children.Add(plinth);

                // Bays & Shelves
                double bodyH = drawH - plinthH;
                double bayW = drawW / cfg.BayCount;
                for (int b = 1; b < cfg.BayCount; b++)
                {
                    double bx = startX + b * bayW;
                    CanvasArchPreview.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = bx, Y1 = startY, X2 = bx, Y2 = startY + bodyH,
                        Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                        StrokeThickness = 2
                    });
                }

                for (int s = 1; s <= cfg.ShelvesPerBay; s++)
                {
                    double sy = startY + (bodyH / (cfg.ShelvesPerBay + 1)) * s;
                    CanvasArchPreview.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = startX, Y1 = sy, X2 = startX + drawW, Y2 = sy,
                        Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 189, 248)),
                        StrokeThickness = 2
                    });
                }
            }
        }

        public static List<XYZ> SampleArchCurvePoints(int archTypeIndex, double span, double springH, double rise, int segments)
        {
            var pts = new List<XYZ>();
            double halfSpan = span * 0.5;

            // Left jamb bottom -> springing
            pts.Add(new XYZ(-halfSpan, 0, 0));
            pts.Add(new XYZ(-halfSpan, springH, 0));

            for (int i = 1; i < segments; i++)
            {
                double t = (double)i / segments; // 0..1 from left (-halfSpan) to right (+halfSpan)
                double angle = Math.PI * (1.0 - t); // pi..0
                double x = -halfSpan + span * t;
                double y = springH;

                switch (archTypeIndex)
                {
                    case 0: // Semicircular
                    case 1: // Segmental
                    case 4: // Three-centered (elliptical approximation)
                        x = halfSpan * Math.Cos(angle);
                        y = springH + rise * Math.Sin(angle);
                        break;

                    case 2: // Pointed / Gothic
                    {
                        double normX = Math.Abs(2.0 * t - 1.0); // 1 at edges, 0 at apex
                        y = springH + rise * Math.Sqrt(Math.Max(0, 1.0 - normX * normX * 0.85)) * (1.0 - 0.15 * normX);
                        break;
                    }

                    case 3: // Horseshoe / Moorish
                    {
                        double extAngle = -0.22 * Math.PI + (1.44 * Math.PI) * (1.0 - t);
                        x = halfSpan * 1.06 * Math.Cos(extAngle);
                        y = springH + rise * 0.35 + rise * 0.65 * Math.Sin(extAngle);
                        break;
                    }

                    case 5: // Four-centered / Tudor
                    {
                        double normX = Math.Abs(2.0 * t - 1.0);
                        y = springH + rise * (1.0 - Math.Pow(normX, 1.65));
                        break;
                    }

                    case 6: // Ogee (S-curve reverse inflection)
                    {
                        double normX = Math.Abs(2.0 * t - 1.0); // 0 at apex, 1 at springing
                        double sCurve = 0.5 * (1.0 + Math.Cos(Math.PI * normX));
                        y = springH + rise * sCurve;
                        break;
                    }
                }

                pts.Add(new XYZ(x, y, 0));
            }

            // Right springing -> bottom
            pts.Add(new XYZ(halfSpan, springH, 0));
            pts.Add(new XYZ(halfSpan, 0, 0));

            return pts;
        }
    }
}
