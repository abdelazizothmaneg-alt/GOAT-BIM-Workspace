using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.UI;
using ORTools.Commands;
using ORTools.Models;

namespace ORTools.UI
{
    public partial class GoatCeilingWindow : Window
    {
        private readonly GoatCeilingExternalEventHandler _handler;
        private readonly ExternalEvent _extEvent;
        private bool _isInitializing = true;

        public ObservableCollection<CeilingRoomItem> RoomList { get; } = new ObservableCollection<CeilingRoomItem>();
        public bool IsActiveViewScope => RbActiveView.IsChecked == true;

        public GoatCeilingWindow(GoatCeilingExternalEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            DgRooms.ItemsSource = RoomList;
            LoadSavedSettings();
            _isInitializing = false;

            Loaded += (s, e) =>
            {
                TriggerScan();
                RenderRcpPreview();
            };
        }

        private void LoadSavedSettings()
        {
            var cfg = GoatSettingsManager.Load<GoatCeilingSettings>("GoatCeiling");
            RbActiveView.IsChecked = cfg.ActiveViewOnly;
            RbEntireProject.IsChecked = !cfg.ActiveViewOnly;
            ChkAutoCreateCeiling.IsChecked = cfg.AutoCreateMissingCeiling;
            TxtDefaultHeight.Text = cfg.DefaultCeilingHeightMm.ToString("0", CultureInfo.InvariantCulture);
            TxtTileX.Text = cfg.TileWidthMm.ToString("0", CultureInfo.InvariantCulture);
            TxtTileY.Text = cfg.TileLengthMm.ToString("0", CultureInfo.InvariantCulture);
            TxtWallMargin.Text = cfg.WallClearanceMarginMm.ToString("0", CultureInfo.InvariantCulture);
            ChkCenterGrid.IsChecked = cfg.CenterGridInRoom;

            ChkPlaceLighting.IsChecked = cfg.PlaceLighting;
            TxtLightStepX.Text = cfg.LightingStepX.ToString();
            TxtLightStepY.Text = cfg.LightingStepY.ToString();

            ChkPlaceDiffusers.IsChecked = cfg.PlaceDiffusers;
            TxtDiffStepX.Text = cfg.DiffuserStepX.ToString();
            TxtDiffStepY.Text = cfg.DiffuserStepY.ToString();

            ChkPlaceSprinklers.IsChecked = cfg.PlaceSprinklers;
            TxtSpkStepX.Text = cfg.SprinklerStepX.ToString();
            TxtSpkStepY.Text = cfg.SprinklerStepY.ToString();

            ChkAvoidColumns.IsChecked = cfg.AvoidColumns;
            ChkSmartReplace.IsChecked = cfg.SmartReplaceExisting;
            ChkWriteRoomComments.IsChecked = cfg.WriteRoomTagToComments;
        }

        public GoatCeilingSettings CollectSettings()
        {
            double.TryParse(TxtDefaultHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double defH);
            double.TryParse(TxtTileX.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double tx);
            double.TryParse(TxtTileY.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ty);
            double.TryParse(TxtWallMargin.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double margin);

            int.TryParse(TxtLightStepX.Text, out int lx);
            int.TryParse(TxtLightStepY.Text, out int ly);
            int.TryParse(TxtDiffStepX.Text, out int dx);
            int.TryParse(TxtDiffStepY.Text, out int dy);
            int.TryParse(TxtSpkStepX.Text, out int sx);
            int.TryParse(TxtSpkStepY.Text, out int sy);

            return new GoatCeilingSettings
            {
                ActiveViewOnly = RbActiveView.IsChecked == true,
                AutoCreateMissingCeiling = ChkAutoCreateCeiling.IsChecked == true,
                SelectedCeilingTypeName = (CbCeilingTypes.SelectedItem as ElementTypeOption)?.Name ?? "",
                DefaultCeilingHeightMm = defH > 500 ? defH : 2700.0,
                TileWidthMm = tx >= 200 ? tx : 600.0,
                TileLengthMm = ty >= 200 ? ty : 600.0,
                WallClearanceMarginMm = margin >= 50 ? margin : 450.0,
                CenterGridInRoom = ChkCenterGrid.IsChecked == true,

                PlaceLighting = ChkPlaceLighting.IsChecked == true,
                SelectedLightingSymbolName = (CbLightingSymbols.SelectedItem as ElementTypeOption)?.Name ?? "",
                LightingStepX = Math.Max(1, lx),
                LightingStepY = Math.Max(1, ly),

                PlaceDiffusers = ChkPlaceDiffusers.IsChecked == true,
                SelectedDiffuserSymbolName = (CbDiffuserSymbols.SelectedItem as ElementTypeOption)?.Name ?? "",
                DiffuserStepX = Math.Max(1, dx),
                DiffuserStepY = Math.Max(1, dy),

                PlaceSprinklers = ChkPlaceSprinklers.IsChecked == true,
                SelectedSprinklerSymbolName = (CbSprinklerSymbols.SelectedItem as ElementTypeOption)?.Name ?? "",
                SprinklerStepX = Math.Max(1, sx),
                SprinklerStepY = Math.Max(1, sy),

                AvoidColumns = ChkAvoidColumns.IsChecked == true,
                SmartReplaceExisting = ChkSmartReplace.IsChecked == true,
                WriteRoomTagToComments = ChkWriteRoomComments.IsChecked == true
            };
        }

        public List<CeilingRoomItem> GetSelectedRooms() => RoomList.Where(r => r.IsSelected).ToList();

        public void ApplyScannedData(
            List<ElementTypeOption> ceilingTypes,
            List<ElementTypeOption> lightingSymbols,
            List<ElementTypeOption> diffuserSymbols,
            List<ElementTypeOption> sprinklerSymbols,
            List<CeilingRoomItem> rooms)
        {
            CbCeilingTypes.ItemsSource = ceilingTypes;
            if (ceilingTypes.Count > 0) CbCeilingTypes.SelectedIndex = 0;

            CbLightingSymbols.ItemsSource = lightingSymbols;
            if (lightingSymbols.Count > 0) CbLightingSymbols.SelectedIndex = 0;

            CbDiffuserSymbols.ItemsSource = diffuserSymbols;
            if (diffuserSymbols.Count > 0) CbDiffuserSymbols.SelectedIndex = 0;

            CbSprinklerSymbols.ItemsSource = sprinklerSymbols;
            if (sprinklerSymbols.Count > 0) CbSprinklerSymbols.SelectedIndex = 0;

            RoomList.Clear();
            foreach (var r in rooms) RoomList.Add(r);

            UpdateRoomStats();
            RenderRcpPreview();
        }

        public void UpdateStatus(string msg, bool isError)
        {
            TxtStatus.Text = msg;
            TxtStatus.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                : new SolidColorBrush(Color.FromRgb(15, 23, 42));
        }

        private void UpdateRoomStats()
        {
            int sel = RoomList.Count(r => r.IsSelected);
            int missingCeil = RoomList.Count(r => r.IsSelected && !r.HasExistingCeiling);
            if (TxtRoomCount != null) TxtRoomCount.Text = $"{sel} Selected";
            if (TxtStatus != null)
                TxtStatus.Text = $"{sel} Rooms Selected ({missingCeil} Missing Ceilings to Auto-Create)";
        }

        private void TriggerScan()
        {
            if (_handler == null || _extEvent == null) return;
            _handler.CurrentAction = GoatCeilingAction.ScanRoomsAndFamilies;
            _extEvent.Raise();
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            string tag = btn.Tag?.ToString() ?? "0";
            Button[] btns = { BtnTabGrid, BtnTabFixtures, BtnTabRules };
            StackPanel[] panels = { PanelGrid, PanelFixtures, PanelRules };
            for (int i = 0; i < btns.Length; i++)
            {
                bool active = i.ToString() == tag;
                btns[i].Background = active ? Brushes.White : new SolidColorBrush(Color.FromRgb(241, 245, 249));
                btns[i].Foreground = active ? new SolidColorBrush(Color.FromRgb(15, 23, 42)) : new SolidColorBrush(Color.FromRgb(100, 116, 139));
                btns[i].BorderBrush = active ? new SolidColorBrush(Color.FromRgb(15, 23, 42)) : Brushes.Transparent;
                panels[i].Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Scope_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            TriggerScan();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => TriggerScan();

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var r in RoomList) r.IsSelected = true;
            DgRooms.Items.Refresh();
            UpdateRoomStats();
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var r in RoomList) r.IsSelected = false;
            DgRooms.Items.Refresh();
            UpdateRoomStats();
        }

        private void DgRooms_SelectionChanged(object sender, SelectionChangedEventArgs e) => RenderRcpPreview();
        private void PreviewSetting_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;
            RenderRcpPreview();
        }
        private void PreviewCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            RenderRcpPreview();
        }
        private void CanvasRcpPreview_SizeChanged(object sender, SizeChangedEventArgs e) => RenderRcpPreview();

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            _handler.CurrentAction = GoatCeilingAction.GenerateCeilingAndArray;
            _extEvent.Raise();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void RenderRcpPreview()
        {
            if (CanvasRcpPreview == null) return;
            CanvasRcpPreview.Children.Clear();

            double w = CanvasRcpPreview.ActualWidth;
            double h = CanvasRcpPreview.ActualHeight;
            if (w < 50 || h < 50) return;

            var cfg = CollectSettings();
            double pad = 22;
            double roomW = w - pad * 2;
            double roomH = h - pad * 2;

            // Room Wall Core
            var wallRect = new Rectangle
            {
                Width = roomW,
                Height = roomH,
                Stroke = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                StrokeThickness = 4,
                Fill = Brushes.White
            };
            Canvas.SetLeft(wallRect, pad);
            Canvas.SetTop(wallRect, pad);
            CanvasRcpPreview.Children.Add(wallRect);

            // Clearance Margin Dashed Box
            double marginRatio = Math.Clamp(cfg.WallClearanceMarginMm / 6000.0, 0.05, 0.22);
            double mx = roomW * marginRatio;
            double my = roomH * marginRatio;
            var marginRect = new Rectangle
            {
                Width = Math.Max(10, roomW - mx * 2),
                Height = Math.Max(10, roomH - my * 2),
                Stroke = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 }
            };
            Canvas.SetLeft(marginRect, pad + mx);
            Canvas.SetTop(marginRect, pad + my);
            CanvasRcpPreview.Children.Add(marginRect);

            // 600x600 Grid Lines
            int cols = 8;
            int rows = 8;
            double innerW = roomW - mx * 2;
            double innerH = roomH - my * 2;
            double stepPxX = innerW / cols;
            double stepPxY = innerH / rows;

            for (int ix = 0; ix <= cols; ix++)
            {
                double x = pad + mx + ix * stepPxX;
                CanvasRcpPreview.Children.Add(new Line
                {
                    X1 = x, Y1 = pad, X2 = x, Y2 = pad + roomH,
                    Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    StrokeThickness = 1
                });
            }
            for (int iy = 0; iy <= rows; iy++)
            {
                double y = pad + my + iy * stepPxY;
                CanvasRcpPreview.Children.Add(new Line
                {
                    X1 = pad, Y1 = y, X2 = pad + roomW, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    StrokeThickness = 1
                });
            }

            // Place Fixtures on Grid Nodes
            for (int ix = 0; ix < cols; ix++)
            {
                for (int iy = 0; iy < rows; iy++)
                {
                    double cx = pad + mx + (ix + 0.5) * stepPxX;
                    double cy = pad + my + (iy + 0.5) * stepPxY;

                    if (cfg.PlaceLighting && (ix % cfg.LightingStepX == 0) && (iy % cfg.LightingStepY == 0))
                    {
                        var el = new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)) };
                        Canvas.SetLeft(el, cx - 5);
                        Canvas.SetTop(el, cy - 5);
                        CanvasRcpPreview.Children.Add(el);
                    }
                    else if (cfg.PlaceDiffusers && ((ix + 1) % cfg.DiffuserStepX == 0) && ((iy + 1) % cfg.DiffuserStepY == 0))
                    {
                        var el = new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromRgb(2, 132, 199)) };
                        Canvas.SetLeft(el, cx - 5);
                        Canvas.SetTop(el, cy - 5);
                        CanvasRcpPreview.Children.Add(el);
                    }
                    else if (cfg.PlaceSprinklers && ((ix + 2) % cfg.SprinklerStepX == 0) && ((iy + 2) % cfg.SprinklerStepY == 0))
                    {
                        var el = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)) };
                        Canvas.SetLeft(el, cx - 4);
                        Canvas.SetTop(el, cy - 4);
                        CanvasRcpPreview.Children.Add(el);
                    }
                }
            }
        }
    }
}
