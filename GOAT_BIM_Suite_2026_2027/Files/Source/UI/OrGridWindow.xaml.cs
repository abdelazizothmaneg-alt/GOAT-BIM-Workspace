using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using ORTools.Models;

namespace ORTools.UI
{
    public partial class OrGridWindow : Window
    {
        public double TotalWidthHorizontalMm => HorizontalRows.Sum(r => r.FromPreviousMm);
        public double TotalHeightVerticalMm => VerticalRows.Sum(r => r.FromPreviousMm);
        public bool IsSpecialSameBase => !IsSpecialOwnBasePoint;
        public bool IsLinearArray => !IsSpecialRadial;

        private readonly Document _doc;
        private readonly HashSet<string> _existingProjectGridNames;
        private bool _isInitializing = true;
        private bool _suppressTableRebuild = false;

        public ObservableCollection<GridRowItem> HorizontalRows { get; } = new ObservableCollection<GridRowItem>();
        public ObservableCollection<GridRowItem> VerticalRows { get; } = new ObservableCollection<GridRowItem>();
        public ObservableCollection<GridRowItem> SpecialRows { get; } = new ObservableCollection<GridRowItem>();

        public ElementId SelectedGridTypeId { get; private set; }
        public BubbleDisplayMode SelectedBubbleMode { get; private set; } = BubbleDisplayMode.GridTypeDefault;
        public double ExtensionMm { get; private set; } = 2000.0;
        public bool IsSpecialEnabled => ChkEnableSpecial.IsChecked == true;
        public bool IsSpecialRadial => RbRadialArray.IsChecked == true;
        public bool IsSpecialOwnBasePoint => RbSpecialOwnBase.IsChecked == true;
        public double SpecialRotationDeg { get; private set; } = 30.0;
        public double SpecialLengthMm { get; private set; } = 20000.0;

        public OrGridWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            _existingProjectGridNames = new HashSet<string>(
                new FilteredElementCollector(_doc)
                    .OfClass(typeof(Autodesk.Revit.DB.Grid))
                    .Cast<Autodesk.Revit.DB.Grid>()
                    .Select(g => g.Name),
                StringComparer.OrdinalIgnoreCase
            );

            DgHorizontal.ItemsSource = HorizontalRows;
            DgVertical.ItemsSource = VerticalRows;
            DgSpecial.ItemsSource = SpecialRows;

            LoadGridTypes();

            _isInitializing = false;

            RebuildHorizontalTable();
            RebuildVerticalTable();
            RebuildSpecialTable();
            RefreshValidationAndPreview();
        }

        private void LoadGridTypes()
        {
            var gridTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(GridType))
                .Cast<GridType>()
                .OrderBy(gt => gt.Name)
                .Select(gt => new GridTypeItem { Name = gt.Name, TypeId = gt.Id })
                .ToList();

            CbGridType.ItemsSource = gridTypes;
            if (gridTypes.Count > 0)
            {
                int idx = gridTypes.FindIndex(g => g.Name.IndexOf("8mm", StringComparison.OrdinalIgnoreCase) >= 0);
                CbGridType.SelectedIndex = idx >= 0 ? idx : 0;
            }
        }

        private void AttachRowListeners(IEnumerable<GridRowItem> rows)
        {
            foreach (var r in rows)
            {
                r.PropertyChanged -= RowItem_PropertyChanged;
                r.PropertyChanged += RowItem_PropertyChanged;
            }
        }

        private void RowItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressTableRebuild) return;
            if (e.PropertyName == nameof(GridRowItem.Name) || e.PropertyName == nameof(GridRowItem.FromPreviousMm))
            {
                RefreshValidationAndPreview();
            }
        }

        private void RebuildHorizontalTable()
        {
            if (_isInitializing) return;
            _suppressTableRebuild = true;
            try
            {
                int count = ParseCount(TxtHCount.Text, 4);
                List<double> spacings = GridFormulaHelper.ParseSpacingFormula(TxtHSpacing.Text, count);
                List<string> names = GridFormulaHelper.GenerateGridNames(
                    count, TxtHStart.Text, TxtHPrefix.Text, TxtHSuffix.Text, ChkHSkipIO.IsChecked == true);

                HorizontalRows.Clear();
                for (int i = 0; i < count; i++)
                {
                    HorizontalRows.Add(new GridRowItem
                    {
                        Index = i + 1,
                        Name = names[i],
                        FromPreviousMm = spacings[i]
                    });
                }
                AttachRowListeners(HorizontalRows);
            }
            finally
            {
                _suppressTableRebuild = false;
            }
            RefreshValidationAndPreview();
        }

        private void RebuildVerticalTable()
        {
            if (_isInitializing) return;
            _suppressTableRebuild = true;
            try
            {
                int count = ParseCount(TxtVCount.Text, 5);
                List<double> spacings = GridFormulaHelper.ParseSpacingFormula(TxtVSpacing.Text, count);
                List<string> names = GridFormulaHelper.GenerateGridNames(
                    count, TxtVStart.Text, TxtVPrefix.Text, TxtVSuffix.Text, ChkVSkipIO.IsChecked == true);

                VerticalRows.Clear();
                for (int i = 0; i < count; i++)
                {
                    VerticalRows.Add(new GridRowItem
                    {
                        Index = i + 1,
                        Name = names[i],
                        FromPreviousMm = spacings[i]
                    });
                }
                AttachRowListeners(VerticalRows);
            }
            finally
            {
                _suppressTableRebuild = false;
            }
            RefreshValidationAndPreview();
        }

        private void RebuildSpecialTable()
        {
            if (_isInitializing) return;
            _suppressTableRebuild = true;
            try
            {
                int count = ParseCount(TxtSCount.Text, 3);
                List<double> spacings = GridFormulaHelper.ParseSpacingFormula(TxtSSpacing.Text, count);
                List<string> names = GridFormulaHelper.GenerateGridNames(
                    count, TxtSStart.Text, TxtSPrefix.Text, TxtSSuffix.Text, ChkSSkipIO.IsChecked == true);

                SpecialRows.Clear();
                for (int i = 0; i < count; i++)
                {
                    SpecialRows.Add(new GridRowItem
                    {
                        Index = i + 1,
                        Name = names[i],
                        FromPreviousMm = spacings[i]
                    });
                }
                AttachRowListeners(SpecialRows);
            }
            finally
            {
                _suppressTableRebuild = false;
            }
            RefreshValidationAndPreview();
        }

        private static int ParseCount(string text, int fallback)
        {
            if (int.TryParse((text ?? string.Empty).Trim(), out int c))
            {
                return Math.Max(0, Math.Min(150, c));
            }
            return fallback;
        }

        private void RefreshValidationAndPreview()
        {
            if (_isInitializing) return;

            var activeRows = new List<GridRowItem>();
            activeRows.AddRange(HorizontalRows);
            activeRows.AddRange(VerticalRows);
            if (IsSpecialEnabled)
                activeRows.AddRange(SpecialRows);

            Dictionary<string, int> dialogNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in activeRows)
            {
                string nm = (r.Name ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(nm)) continue;
                dialogNameCounts[nm] = dialogNameCounts.ContainsKey(nm) ? dialogNameCounts[nm] + 1 : 1;
            }

            int issueCount = 0;
            foreach (var r in activeRows)
            {
                string nm = (r.Name ?? string.Empty).Trim();
                bool clash = string.IsNullOrEmpty(nm) ||
                             _existingProjectGridNames.Contains(nm) ||
                             (dialogNameCounts.ContainsKey(nm) && dialogNameCounts[nm] > 1);
                r.HasNameIssue = clash;
                if (clash) issueCount++;
            }

            int hCount = HorizontalRows.Count;
            int vCount = VerticalRows.Count;
            int sCount = IsSpecialEnabled ? SpecialRows.Count : 0;
            int total = hCount + vCount + sCount;

            TxtSummaryCounts.Text = $"{hCount} horizontal · {vCount} vertical · {sCount} special = {total} grids";

            if (issueCount > 0)
            {
                TxtClashWarning.Text = $"{issueCount} name issue(s) highlighted in the tables. You can still create: clashing grids get a temporary name (e.g. A_dup1) and are selected in the view for you to resolve.";
                TxtClashWarning.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(146, 64, 14));
            }
            else
            {
                TxtClashWarning.Text = "No name clashes detected. All grid names are unique.";
                TxtClashWarning.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 101, 52));
            }

            RenderLivePreview();
        }

        private void RenderLivePreview()
        {
            if (CnvPreview == null) return;
            CnvPreview.Children.Clear();

            double w = CnvPreview.ActualWidth;
            double h = CnvPreview.ActualHeight;
            if (w < 50 || h < 50) return;

            double extMm = 2000.0;
            if (double.TryParse(TxtExtensionMm?.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedExt))
                extMm = Math.Max(0.0, parsedExt);
            ExtensionMm = extMm;

            List<(GridRowItem Row, double Y)> hCoords = new List<(GridRowItem, double)>();
            double curY = 0.0;
            foreach (var r in HorizontalRows)
            {
                curY += r.FromPreviousMm;
                hCoords.Add((r, curY));
            }

            List<(GridRowItem Row, double X)> vCoords = new List<(GridRowItem, double)>();
            double curX = 0.0;
            foreach (var r in VerticalRows)
            {
                curX += r.FromPreviousMm;
                vCoords.Add((r, curX));
            }

            double minX = vCoords.Count > 0 ? Math.Min(0.0, vCoords.Min(v => v.X)) : 0.0;
            double maxX = vCoords.Count > 0 ? Math.Max(6000.0, vCoords.Max(v => v.X)) : 12000.0;
            double minY = hCoords.Count > 0 ? Math.Min(0.0, hCoords.Min(hc => hc.Y)) : 0.0;
            double maxY = hCoords.Count > 0 ? Math.Max(6000.0, hCoords.Max(hc => hc.Y)) : 12000.0;

            double modelMinX = minX - extMm;
            double modelMaxX = maxX + extMm;
            double modelMinY = minY - extMm;
            double modelMaxY = maxY + extMm;

            double spanX = Math.Max(1000.0, modelMaxX - modelMinX);
            double spanY = Math.Max(1000.0, modelMaxY - modelMinY);

            double margin = 48.0;
            double availW = Math.Max(40.0, w - margin * 2);
            double availH = Math.Max(40.0, h - margin * 2);

            double scale = Math.Min(availW / spanX, availH / spanY);

            double offsetX = margin + (availW - spanX * scale) * 0.5;
            double offsetY = margin + (availH - spanY * scale) * 0.5;

            Func<double, double> toScreenX = mx => offsetX + (mx - modelMinX) * scale;
            Func<double, double> toScreenY = my => h - (offsetY + (my - modelMinY) * scale);

            double bubbleRadius = 11.5;
            DoubleCollection dashPattern = new DoubleCollection { 6, 3, 1.5, 3 };

            foreach (var item in hCoords)
            {
                double sy = toScreenY(item.Y);
                double sx1 = toScreenX(modelMinX);
                double sx2 = toScreenX(modelMaxX);

                Brush stroke = item.Row.HasNameIssue
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(82, 82, 91));

                System.Windows.Shapes.Line ln = new System.Windows.Shapes.Line
                {
                    X1 = sx1, Y1 = sy,
                    X2 = sx2, Y2 = sy,
                    Stroke = stroke,
                    StrokeThickness = 1.2,
                    StrokeDashArray = dashPattern
                };
                CnvPreview.Children.Add(ln);

                DrawGridBubble(sx1 - bubbleRadius, sy, bubbleRadius, item.Row.Name, stroke);
            }

            foreach (var item in vCoords)
            {
                double sx = toScreenX(item.X);
                double syBottom = toScreenY(modelMinY);
                double syTop = toScreenY(modelMaxY);

                Brush stroke = item.Row.HasNameIssue
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(82, 82, 91));

                System.Windows.Shapes.Line ln = new System.Windows.Shapes.Line
                {
                    X1 = sx, Y1 = syBottom,
                    X2 = sx, Y2 = syTop,
                    Stroke = stroke,
                    StrokeThickness = 1.2,
                    StrokeDashArray = dashPattern
                };
                CnvPreview.Children.Add(ln);

                DrawGridBubble(sx, syTop - bubbleRadius, bubbleRadius, item.Row.Name, stroke);
            }

            if (IsSpecialEnabled && SpecialRows.Count > 0)
            {
                double.TryParse(TxtSpecialRotation?.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double rotDeg);
                double.TryParse(TxtSpecialLength?.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lenMm);
                SpecialRotationDeg = rotDeg;
                SpecialLengthMm = Math.Max(1000.0, lenMm);

                double halfLen = SpecialLengthMm * 0.5;
                Brush specialOkBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199));
                Brush specialClashBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));

                if (!IsSpecialRadial)
                {
                    double rad = rotDeg * Math.PI / 180.0;
                    double dirX = Math.Cos(rad);
                    double dirY = Math.Sin(rad);
                    double perpX = -dirY;
                    double perpY = dirX;

                    double cumOffset = 0.0;
                    foreach (var sRow in SpecialRows)
                    {
                        cumOffset += sRow.FromPreviousMm;
                        double cx = perpX * cumOffset;
                        double cy = perpY * cumOffset;

                        double x1 = toScreenX(cx - dirX * halfLen);
                        double y1 = toScreenY(cy - dirY * halfLen);
                        double x2 = toScreenX(cx + dirX * halfLen);
                        double y2 = toScreenY(cy + dirY * halfLen);

                        Brush b = sRow.HasNameIssue ? specialClashBrush : specialOkBrush;
                        CnvPreview.Children.Add(new System.Windows.Shapes.Line
                        {
                            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                            Stroke = b, StrokeThickness = 1.3, StrokeDashArray = dashPattern
                        });
                        DrawGridBubble(x2, y2, bubbleRadius, sRow.Name, b);
                    }
                }
                else
                {
                    double cumAngle = rotDeg;
                    foreach (var sRow in SpecialRows)
                    {
                        cumAngle += sRow.FromPreviousMm;
                        double rad = cumAngle * Math.PI / 180.0;
                        double x1 = toScreenX(0);
                        double y1 = toScreenY(0);
                        double x2 = toScreenX(Math.Cos(rad) * SpecialLengthMm);
                        double y2 = toScreenY(Math.Sin(rad) * SpecialLengthMm);

                        Brush b = sRow.HasNameIssue ? specialClashBrush : specialOkBrush;
                        CnvPreview.Children.Add(new System.Windows.Shapes.Line
                        {
                            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                            Stroke = b, StrokeThickness = 1.3, StrokeDashArray = dashPattern
                        });
                        DrawGridBubble(x2, y2, bubbleRadius, sRow.Name, b);
                    }
                }
            }

            double bx = toScreenX(0.0);
            double by = toScreenY(0.0);
            Brush blueBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199));

            CnvPreview.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = bx - 6, Y1 = by, X2 = bx + 6, Y2 = by,
                Stroke = blueBrush, StrokeThickness = 1.8
            });
            CnvPreview.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = bx, Y1 = by - 6, X2 = bx, Y2 = by + 6,
                Stroke = blueBrush, StrokeThickness = 1.8
            });

            TextBlock lblBase = new TextBlock
            {
                Text = "base point",
                Foreground = blueBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            Canvas.SetLeft(lblBase, bx + 6);
            Canvas.SetTop(lblBase, by + 3);
            CnvPreview.Children.Add(lblBase);
        }

        private void DrawGridBubble(double cx, double cy, double radius, string text, Brush borderBrush)
        {
            System.Windows.Shapes.Ellipse el = new System.Windows.Shapes.Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = Brushes.White,
                Stroke = borderBrush,
                StrokeThickness = 1.4
            };
            Canvas.SetLeft(el, cx - radius);
            Canvas.SetTop(el, cy - radius);
            CnvPreview.Children.Add(el);

            TextBlock tb = new TextBlock
            {
                Text = text ?? string.Empty,
                Foreground = borderBrush,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Width = radius * 2
            };
            Canvas.SetLeft(tb, cx - radius);
            Canvas.SetTop(tb, cy - 7);
            CnvPreview.Children.Add(tb);
        }

        private void HorizontalInputs_Changed(object sender, RoutedEventArgs e) => RebuildHorizontalTable();
        private void VerticalInputs_Changed(object sender, RoutedEventArgs e) => RebuildVerticalTable();
        private void SpecialInputs_Changed(object sender, RoutedEventArgs e) => RebuildSpecialTable();

        private void SpecialToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            PnlSpecialContent.IsEnabled = ChkEnableSpecial.IsChecked == true;
            RefreshValidationAndPreview();
        }

        private void SpecialMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            if (RbRadialArray.IsChecked == true)
            {
                LblSpecialAngle.Text = "Start angle (°)";
                LblSpecialSpacing.Text = "Angle step (°)";
            }
            else
            {
                LblSpecialAngle.Text = "Rotation (°)";
                LblSpecialSpacing.Text = "Spacing (mm)";
            }
            RefreshValidationAndPreview();
        }

        private void SpecialPreview_Changed(object sender, TextChangedEventArgs e) => RefreshValidationAndPreview();
        private void Extension_Changed(object sender, TextChangedEventArgs e) => RefreshValidationAndPreview();
        private void CbBubblesMode_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshValidationAndPreview();
        private void CnvPreview_SizeChanged(object sender, SizeChangedEventArgs e) => RenderLivePreview();

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnPickAndCreate_Click(object sender, RoutedEventArgs e)
        {
            if (CbGridType.SelectedItem is GridTypeItem selType)
                SelectedGridTypeId = selType.TypeId;

            SelectedBubbleMode = (BubbleDisplayMode)Math.Max(0, CbBubblesMode.SelectedIndex);

            if (double.TryParse(TxtExtensionMm.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ext))
                ExtensionMm = Math.Max(0.0, ext);

            if (double.TryParse(TxtSpecialRotation.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double rot))
                SpecialRotationDeg = rot;

            if (double.TryParse(TxtSpecialLength.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double len))
                SpecialLengthMm = Math.Max(1000.0, len);

            DialogResult = true;
            Close();
        }
    }



}
