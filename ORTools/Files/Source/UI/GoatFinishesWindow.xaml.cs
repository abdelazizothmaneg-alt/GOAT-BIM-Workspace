using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using ORTools.Commands;
using ORTools.Models;

namespace ORTools.UI
{
    public partial class GoatFinishesWindow : Window
    {
        private readonly GoatFinishesExternalEventHandler _handler;
        private readonly ExternalEvent _extEvent;
        private bool _isInitializing = true;

        private List<ElementTypeOption> _wallTypeOptions = new List<ElementTypeOption>();
        private List<ElementTypeOption> _sweepTypeOptions = new List<ElementTypeOption>();
        private List<ElementTypeOption> _floorTypeOptions = new List<ElementTypeOption>();
        private List<ElementTypeOption> _ceilingTypeOptions = new List<ElementTypeOption>();

        public ObservableCollection<RoomFinishItem> RoomList { get; } = new ObservableCollection<RoomFinishItem>();
        public bool IsActiveViewScope => RbScopeActiveView.IsChecked == true;

        public GoatFinishesWindow(GoatFinishesExternalEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            DgRooms.ItemsSource = RoomList;

            LoadSavedSettingsToUI();
            _isInitializing = false;

            Loaded += (s, e) =>
            {
                TriggerScanRooms();
                RenderPlanPreview();
                RenderSectionPreview();
            };
        }

        private void LoadSavedSettingsToUI()
        {
            GoatFinishesSettings cfg = GoatSettingsManager.Load<GoatFinishesSettings>("GoatFinishes");
            RbScopeActiveView.IsChecked = cfg.ActiveViewOnly;
            RbScopeEntireProject.IsChecked = !cfg.ActiveViewOnly;

            ChkCreateWalls.IsChecked = cfg.CreateWalls;
            RbWallSameAll.IsChecked = cfg.WallSameOnAll;
            RbWallFromRoom.IsChecked = !cfg.WallSameOnAll;
            RbWallBaseSlab.IsChecked = cfg.WallBaseOnSlab;
            RbWallBaseFinishFloor.IsChecked = !cfg.WallBaseOnSlab;
            RbWallTopCeiling.IsChecked = cfg.WallTopAtCeiling;
            RbWallTopSlabAbove.IsChecked = !cfg.WallTopAtCeiling;
            TxtCeilingVoidOffset.Text = cfg.CeilingVoidOffsetMm.ToString("0", CultureInfo.InvariantCulture);

            RbColWrapSame.IsChecked = cfg.ColumnHandlingMode == 0;
            RbColSpecificType.IsChecked = cfg.ColumnHandlingMode == 1;
            RbColIgnore.IsChecked = cfg.ColumnHandlingMode == 2;
            RowColWallType.Visibility = cfg.ColumnHandlingMode == 1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            ChkCreateSkirting.IsChecked = cfg.CreateSkirting;
            RbSkirtMethodWall.IsChecked = cfg.SkirtingMethod == 0;
            RbSkirtMethodSweep.IsChecked = cfg.SkirtingMethod == 1;
            TxtSkirtingHeight.Text = cfg.SkirtingHeightMm.ToString("0", CultureInfo.InvariantCulture);
            RbSkirtInFront.IsChecked = cfg.SkirtingInFrontOfWall;
            RbSkirtBelowFlush.IsChecked = !cfg.SkirtingInFrontOfWall;
            ChkJoinSkirting.IsChecked = cfg.SkirtingJoinWithWall;

            ChkCreateFloors.IsChecked = cfg.CreateFloors;
            RbFloorSameAll.IsChecked = cfg.FloorSameOnAll;
            RbFloorFromRoom.IsChecked = !cfg.FloorSameOnAll;
            TxtFloorOffset.Text = cfg.FloorOffsetMm.ToString("0", CultureInfo.InvariantCulture);
            ChkFloorDoorThresholds.IsChecked = cfg.ExtendIntoDoorThresholds;
            TxtDoorThresholdDepth.Text = cfg.DoorThresholdDepthMm.ToString("0", CultureInfo.InvariantCulture);

            ChkCreateCeilings.IsChecked = cfg.CreateCeilings;
            RbCeilingSameAll.IsChecked = cfg.CeilingSameOnAll;
            RbCeilingFromRoom.IsChecked = !cfg.CeilingSameOnAll;
            TxtCeilingHeight.Text = cfg.CeilingHeightMm.ToString("0", CultureInfo.InvariantCulture);

            ChkWriteRoomData.IsChecked = cfg.WriteRoomFinishParameters;
            RbDataUseTypeName.IsChecked = cfg.SyncTypeNameToRoomParams;
            RbDataUseCustomCode.IsChecked = !cfg.SyncTypeNameToRoomParams;
            TxtDataWallCode.Text = cfg.CustomWallFinishCode;
            TxtDataBaseCode.Text = cfg.CustomBaseFinishCode;
            TxtDataFloorCode.Text = cfg.CustomFloorFinishCode;
            TxtDataCeilingCode.Text = cfg.CustomCeilingFinishCode;

            ChkOptBreakAtDoors.IsChecked = cfg.BreakWallsAtDoors;
            ChkOptAutoJoin.IsChecked = cfg.AutoJoinWithHostWalls;
            ChkOptSmartReplace.IsChecked = cfg.SmartReplaceExisting;
            ChkOptSkipUnenclosed.IsChecked = cfg.SkipUnenclosedRooms;
        }

        public GoatFinishesSettings CollectCurrentSettings()
        {
            double.TryParse(TxtCeilingVoidOffset.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double voidOff);
            double.TryParse(TxtSkirtingHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double skirtH);
            double.TryParse(TxtFloorOffset.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double floorOff);
            double.TryParse(TxtDoorThresholdDepth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double doorDepth);
            double.TryParse(TxtCeilingHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ceilH);

            int colMode = RbColSpecificType.IsChecked == true ? 1 : (RbColIgnore.IsChecked == true ? 2 : 0);

            return new GoatFinishesSettings
            {
                ActiveViewOnly = RbScopeActiveView.IsChecked == true,
                CreateWalls = ChkCreateWalls.IsChecked == true,
                SelectedWallTypeName = (CbWallTypes.SelectedItem as ElementTypeOption)?.Name ?? "",
                WallSameOnAll = RbWallSameAll.IsChecked == true,
                WallBaseOnSlab = RbWallBaseSlab.IsChecked == true,
                WallTopAtCeiling = RbWallTopCeiling.IsChecked == true,
                CeilingVoidOffsetMm = voidOff,
                ColumnHandlingMode = colMode,
                SelectedColumnWallTypeName = (CbColumnWallTypes.SelectedItem as ElementTypeOption)?.Name ?? "",

                CreateSkirting = ChkCreateSkirting.IsChecked == true,
                SkirtingMethod = RbSkirtMethodSweep.IsChecked == true ? 1 : 0,
                SelectedSkirtingTypeName = (CbSkirtingTypes.SelectedItem as ElementTypeOption)?.Name ?? "",
                SkirtingHeightMm = skirtH > 10 ? skirtH : 100.0,
                SkirtingInFrontOfWall = RbSkirtInFront.IsChecked == true,
                SkirtingJoinWithWall = ChkJoinSkirting.IsChecked == true,

                CreateFloors = ChkCreateFloors.IsChecked == true,
                SelectedFloorTypeName = (CbFloorTypes.SelectedItem as ElementTypeOption)?.Name ?? "",
                FloorSameOnAll = RbFloorSameAll.IsChecked == true,
                FloorOffsetMm = floorOff,
                ExtendIntoDoorThresholds = ChkFloorDoorThresholds.IsChecked == true,
                DoorThresholdDepthMm = doorDepth > 0 ? doorDepth : 100.0,

                CreateCeilings = ChkCreateCeilings.IsChecked == true,
                SelectedCeilingTypeName = (CbCeilingTypes.SelectedItem as ElementTypeOption)?.Name ?? "",
                CeilingSameOnAll = RbCeilingSameAll.IsChecked == true,
                CeilingHeightMm = ceilH > 500 ? ceilH : 2700.0,

                WriteRoomFinishParameters = ChkWriteRoomData.IsChecked == true,
                SyncTypeNameToRoomParams = RbDataUseTypeName.IsChecked == true,
                CustomWallFinishCode = TxtDataWallCode.Text ?? "",
                CustomBaseFinishCode = TxtDataBaseCode.Text ?? "",
                CustomFloorFinishCode = TxtDataFloorCode.Text ?? "",
                CustomCeilingFinishCode = TxtDataCeilingCode.Text ?? "",

                BreakWallsAtDoors = ChkOptBreakAtDoors.IsChecked == true,
                AutoJoinWithHostWalls = ChkOptAutoJoin.IsChecked == true,
                SmartReplaceExisting = ChkOptSmartReplace.IsChecked == true,
                SkipUnenclosedRooms = ChkOptSkipUnenclosed.IsChecked == true
            };
        }

        public List<RoomFinishItem> GetSelectedRooms()
        {
            return RoomList.Where(r => r.IsSelected).ToList();
        }

        public void ApplyScannedData(
            List<ElementTypeOption> wallTypes,
            List<ElementTypeOption> sweepTypes,
            List<ElementTypeOption> floorTypes,
            List<ElementTypeOption> ceilingTypes,
            List<RoomFinishItem> rooms)
        {
            _wallTypeOptions = wallTypes ?? new List<ElementTypeOption>();
            _sweepTypeOptions = sweepTypes ?? new List<ElementTypeOption>();
            _floorTypeOptions = floorTypes ?? new List<ElementTypeOption>();
            _ceilingTypeOptions = ceilingTypes ?? new List<ElementTypeOption>();

            GoatFinishesSettings saved = GoatSettingsManager.Load<GoatFinishesSettings>("GoatFinishes");

            CbWallTypes.ItemsSource = _wallTypeOptions;
            CbColumnWallTypes.ItemsSource = _wallTypeOptions;
            CbFloorTypes.ItemsSource = _floorTypeOptions;
            CbCeilingTypes.ItemsSource = _ceilingTypeOptions;

            SelectMatchingOption(CbWallTypes, _wallTypeOptions, saved.SelectedWallTypeName, preferThinFinish: true);
            SelectMatchingOption(CbColumnWallTypes, _wallTypeOptions, saved.SelectedColumnWallTypeName, preferThinFinish: true);
            SelectMatchingOption(CbFloorTypes, _floorTypeOptions, saved.SelectedFloorTypeName, preferThinFinish: true);
            SelectMatchingOption(CbCeilingTypes, _ceilingTypeOptions, saved.SelectedCeilingTypeName, preferThinFinish: false);

            PopulateSkirtingComboBox(saved.SelectedSkirtingTypeName);

            RoomList.Clear();
            foreach (var r in rooms)
            {
                RoomList.Add(r);
            }

            if (RoomList.Count > 0)
            {
                DgRooms.SelectedIndex = 0;
            }

            RefreshRoomCreatesAndStats();
            RenderPlanPreview();
            RenderSectionPreview();
        }

        private void PopulateSkirtingComboBox(string preferredName = "")
        {
            if (RbSkirtMethodSweep.IsChecked == true && _sweepTypeOptions.Count > 0)
            {
                CbSkirtingTypes.ItemsSource = _sweepTypeOptions;
                SelectMatchingOption(CbSkirtingTypes, _sweepTypeOptions, preferredName, false);
            }
            else
            {
                CbSkirtingTypes.ItemsSource = _wallTypeOptions;
                SelectMatchingOption(CbSkirtingTypes, _wallTypeOptions, preferredName, true);
            }
        }

        private static void SelectMatchingOption(System.Windows.Controls.ComboBox cb, List<ElementTypeOption> list, string savedName, bool preferThinFinish)
        {
            if (list == null || list.Count == 0) return;

            if (!string.IsNullOrWhiteSpace(savedName))
            {
                var match = list.FirstOrDefault(x => string.Equals(x.Name, savedName, StringComparison.OrdinalIgnoreCase))
                         ?? list.FirstOrDefault(x => x.Name.Contains("Finish", StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    cb.SelectedItem = match;
                    return;
                }
            }

            var finishMatch = list.FirstOrDefault(x => x.Name.Contains("Finish", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("Paint", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("Ceramic", StringComparison.OrdinalIgnoreCase));
            if (finishMatch != null)
            {
                cb.SelectedItem = finishMatch;
                return;
            }

            cb.SelectedItem = preferThinFinish ? list.OrderBy(x => x.ThicknessMm).First() : list[0];
        }

        public void UpdateStatus(string message, bool isError)
        {
            TxtFooterStats.Text = message;
            TxtFooterStats.Foreground = isError
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
        }

        private void RefreshRoomCreatesAndStats()
        {
            bool w = ChkCreateWalls?.IsChecked == true;
            bool s = ChkCreateSkirting?.IsChecked == true;
            bool f = ChkCreateFloors?.IsChecked == true;
            bool c = ChkCreateCeilings?.IsChecked == true;

            string code = $"{(w ? "W" : "-")} {(s ? "S" : "-")} {(f ? "F" : "-")} {(c ? "C" : "-")}";
            foreach (var r in RoomList)
            {
                r.CreatesSummary = r.IsSelected && r.IsEnclosed ? code : "- - - -";
            }

            var selected = RoomList.Where(r => r.IsSelected && r.IsEnclosed).ToList();
            int selCount = selected.Count;
            int estWalls = w ? selected.Sum(r => r.WallSegmentsCount) : 0;
            int estFloors = f ? selCount : 0;
            int estCeilings = c ? selCount : 0;

            if (TxtSelectedRoomsBadge != null)
                TxtSelectedRoomsBadge.Text = $"{selCount} Selected";

            if (TxtFooterStats != null)
                TxtFooterStats.Text = $"{selCount} Rooms Selected → Est. {estWalls} Finish Walls · {estFloors} Floors · {estCeilings} Ceilings";
        }

        private void TriggerScanRooms()
        {
            if (_handler == null || _extEvent == null) return;
            _handler.CurrentAction = GoatFinishesAction.ScanRoomsAndTypes;
            _extEvent.Raise();
        }

        // --- UI Event Handlers ---

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button clickedBtn)) return;
            string tag = clickedBtn.Tag?.ToString() ?? "0";

            Button[] allBtns = { BtnTabWalls, BtnTabSkirting, BtnTabFloors, BtnTabCeilings, BtnTabData, BtnTabOptions };
            StackPanel[] allPanels = { PanelWalls, PanelSkirting, PanelFloors, PanelCeilings, PanelData, PanelOptions };

            for (int i = 0; i < allBtns.Length; i++)
            {
                bool active = i.ToString() == tag;
                allBtns[i].Background = active ? Brushes.White : new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249));
                allBtns[i].Foreground = active ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
                allBtns[i].BorderBrush = active ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)) : Brushes.Transparent;
                allPanels[i].Visibility = active ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        private void ScopeRadio_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            TriggerScanRooms();
        }

        private void BtnRefreshRooms_Click(object sender, RoutedEventArgs e)
        {
            TriggerScanRooms();
        }

        private void ChkHeaderSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool check = ChkHeaderSelectAll.IsChecked == true;
            foreach (var r in RoomList)
            {
                r.IsSelected = check && r.IsEnclosed;
            }
            RefreshRoomCreatesAndStats();
        }

        private void RoomRowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            RefreshRoomCreatesAndStats();
        }

        private void BtnSelectAllRooms_Click(object sender, RoutedEventArgs e)
        {
            if (ChkHeaderSelectAll != null) ChkHeaderSelectAll.IsChecked = true;
            foreach (var r in RoomList) r.IsSelected = r.IsEnclosed;
            RefreshRoomCreatesAndStats();
        }

        private void BtnSelectNoneRooms_Click(object sender, RoutedEventArgs e)
        {
            if (ChkHeaderSelectAll != null) ChkHeaderSelectAll.IsChecked = false;
            foreach (var r in RoomList) r.IsSelected = false;
            RefreshRoomCreatesAndStats();
        }

        private void BtnZoomToRoom_Click(object sender, RoutedEventArgs e)
        {
            var room = DgRooms.SelectedItem as RoomFinishItem ?? RoomList.FirstOrDefault(r => r.IsSelected);
            if (room == null) return;
            _handler.TargetZoomRoomId = room.RoomId;
            _handler.CurrentAction = GoatFinishesAction.ZoomToRoom;
            _extEvent.Raise();
        }

        private void DgRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderPlanPreview();
        }

        private void SkirtMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            PopulateSkirtingComboBox();
            RenderSectionPreview();
        }

        private void SettingChanged_UpdatePreview(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            if (RowColWallType != null)
            {
                RowColWallType.Visibility = RbColSpecificType.IsChecked == true
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
            }
            RefreshRoomCreatesAndStats();
            RenderPlanPreview();
            RenderSectionPreview();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            RenderSectionPreview();
        }

        private void TxtNumeric_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;
            RenderSectionPreview();
        }

        private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderPlanPreview();
            RenderSectionPreview();
        }

        private void BtnGenerateFinishes_Click(object sender, RoutedEventArgs e)
        {
            _handler.CurrentAction = GoatFinishesAction.GenerateFinishes;
            _extEvent.Raise();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = RoomList.Where(r => r.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("Please select at least one room to export.", "GOAT FINISHES", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Export GOAT FINISHES Room Schedule & QTO to Excel (.xlsx)",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"GOAT_RoomFinishes_QTO_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (sfd.ShowDialog() != true) return;

                GoatFinishesSettings cfg = CollectCurrentSettings();
                double wallHeightMeters = (cfg.WallTopAtCeiling ? (cfg.CeilingHeightMm + cfg.CeilingVoidOffsetMm) : 3000.0) / 1000.0;

                // Sheet 1: Room Finishes & QTO Schedule
                var sheet1 = new GoatExcelSheet
                {
                    Name = "Room Finishes QTO",
                    ColumnWidths = new List<double> { 14, 14, 24, 18, 14, 16, 26, 24, 24, 24, 18 }
                };

                sheet1.Rows.Add(new List<GoatExcelCell>
                {
                    new GoatExcelCell("ElementId", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Room Number", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Room Name", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Level", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Net Area (m²)", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Perimeter (m)", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Wall Finish Type", GoatCellStyle.HeaderTeal),
                    new GoatExcelCell("Skirting Finish Type", GoatCellStyle.HeaderTeal),
                    new GoatExcelCell("Floor Finish Type", GoatCellStyle.HeaderTeal),
                    new GoatExcelCell("Ceiling Finish Type", GoatCellStyle.HeaderTeal),
                    new GoatExcelCell("Est. Wall Area (m²)", GoatCellStyle.HeaderNavy)
                });

                double totalArea = 0;
                double totalPerimM = 0;
                double totalWallArea = 0;

                foreach (var r in selected)
                {
                    double perimM = Math.Round(r.PerimeterMm / 1000.0, 2);
                    double estWallSqm = Math.Round(perimM * wallHeightMeters, 2);
                    totalArea += r.AreaSqm;
                    totalPerimM += perimM;
                    totalWallArea += estWallSqm;

                    string wf = !string.IsNullOrWhiteSpace(r.WallFinishParam) ? r.WallFinishParam : cfg.SelectedWallTypeName;
                    string bf = !string.IsNullOrWhiteSpace(r.BaseFinishParam) ? r.BaseFinishParam : cfg.SelectedSkirtingTypeName;
                    string ff = !string.IsNullOrWhiteSpace(r.FloorFinishParam) ? r.FloorFinishParam : cfg.SelectedFloorTypeName;
                    string cf = !string.IsNullOrWhiteSpace(r.CeilingFinishParam) ? r.CeilingFinishParam : cfg.SelectedCeilingTypeName;

                    sheet1.Rows.Add(new List<GoatExcelCell>
                    {
                        new GoatExcelCell(r.RoomId.Value.ToString(), GoatCellStyle.ReadOnlyGrey, true),
                        new GoatExcelCell(r.RoomNumber, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(r.RoomName, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(r.LevelName, GoatCellStyle.ReadOnlyGrey),
                        new GoatExcelCell(r.AreaSqm.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, true),
                        new GoatExcelCell(perimM.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, true),
                        new GoatExcelCell(wf, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(bf, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(ff, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(cf, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(estWallSqm.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, true)
                    });
                }

                // Summary Row
                sheet1.Rows.Add(new List<GoatExcelCell>
                {
                    new GoatExcelCell("TOTAL", GoatCellStyle.SummaryBold),
                    new GoatExcelCell($"{selected.Count} Rooms", GoatCellStyle.SummaryBold),
                    new GoatExcelCell("", GoatCellStyle.SummaryBold),
                    new GoatExcelCell("", GoatCellStyle.SummaryBold),
                    new GoatExcelCell(totalArea.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.SummaryBold, true),
                    new GoatExcelCell(totalPerimM.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.SummaryBold, true),
                    new GoatExcelCell("", GoatCellStyle.SummaryBold),
                    new GoatExcelCell("", GoatCellStyle.SummaryBold),
                    new GoatExcelCell("", GoatCellStyle.SummaryBold),
                    new GoatExcelCell("", GoatCellStyle.SummaryBold),
                    new GoatExcelCell(totalWallArea.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.SummaryBold, true)
                });

                GoatExcelEngine.WriteXlsx(sfd.FileName, new List<GoatExcelSheet> { sheet1 });
                UpdateStatus($"✔ Exported {selected.Count} rooms QTO schedule to {System.IO.Path.GetFileName(sfd.FileName)}", false);

                try
                {
                    Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel Export Error:\n{ex.Message}", "GOAT FINISHES", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- LIVE INTERACTIVE PLAN & SECTION PREVIEW RENDERING ---

        private void RenderPlanPreview()
        {
            if (CanvasPlanPreview == null) return;
            CanvasPlanPreview.Children.Clear();

            double w = CanvasPlanPreview.ActualWidth;
            double h = CanvasPlanPreview.ActualHeight;
            if (w < 40 || h < 40) return;

            var selRoom = DgRooms?.SelectedItem as RoomFinishItem ?? RoomList.FirstOrDefault();
            if (TxtPlanPreviewHeader != null)
            {
                TxtPlanPreviewHeader.Text = selRoom != null
                    ? $"PLAN PREVIEW: {selRoom.DisplayName} ({selRoom.AreaSqm:0.0} m²)"
                    : "PLAN PREVIEW: Room Boundary & Walls";
            }

            bool showFinishWalls = ChkCreateWalls?.IsChecked == true;
            bool breakDoors = ChkOptBreakAtDoors?.IsChecked == true;

            // Outer Host Wall Core Polygon
            double padX = w * 0.14;
            double padY = h * 0.15;
            double rectW = w - padX * 2;
            double rectH = h - padY * 2;

            if (selRoom != null && selRoom.Segments2D != null && selRoom.Segments2D.Count >= 3)
            {
                // Render actual room boundary polygon scaled to fit Canvas!
                double minX = selRoom.Segments2D.Min(s => Math.Min(s.X1, s.X2));
                double maxX = selRoom.Segments2D.Max(s => Math.Max(s.X1, s.X2));
                double minY = selRoom.Segments2D.Min(s => Math.Min(s.Y1, s.Y2));
                double maxY = selRoom.Segments2D.Max(s => Math.Max(s.Y1, s.Y2));

                double spanX = Math.Max(0.1, maxX - minX);
                double spanY = Math.Max(0.1, maxY - minY);
                double scale = Math.Min(rectW / spanX, rectH / spanY);
                double offsetX = (w - spanX * scale) * 0.5;
                double offsetY = (h - spanY * scale) * 0.5;

                foreach (var seg in selRoom.Segments2D)
                {
                    double x1 = offsetX + (seg.X1 - minX) * scale;
                    double y1 = h - (offsetY + (seg.Y1 - minY) * scale);
                    double x2 = offsetX + (seg.X2 - minX) * scale;
                    double y2 = h - (offsetY + (seg.Y2 - minY) * scale);

                    // Core wall line
                    var coreLine = new System.Windows.Shapes.Line
                    {
                        X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                        Stroke = seg.IsColumnFace
                            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139))
                            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                        StrokeThickness = 6,
                        StrokeStartLineCap = PenLineCap.Square,
                        StrokeEndLineCap = PenLineCap.Square
                    };
                    CanvasPlanPreview.Children.Add(coreLine);

                    if (showFinishWalls && !(seg.IsColumnFace && RbColIgnore?.IsChecked == true))
                    {
                        double dx = x2 - x1;
                        double dy = y2 - y1;
                        double len = Math.Sqrt(dx * dx + dy * dy);
                        if (len > 4)
                        {
                            double nx = -dy / len * 5.0;
                            double ny = dx / len * 5.0;
                            var finLine = new System.Windows.Shapes.Line
                            {
                                X1 = x1 + nx, Y1 = y1 + ny,
                                X2 = x2 + nx, Y2 = y2 + ny,
                                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 189, 248)),
                                StrokeThickness = 3
                            };
                            CanvasPlanPreview.Children.Add(finLine);
                        }
                    }
                }
            }
            else
            {
                // Clean schematic rectangular preview matching mockup SVG
                var coreRect = new System.Windows.Shapes.Rectangle
                {
                    Width = rectW,
                    Height = rectH,
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                    StrokeThickness = 7,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(coreRect, padX);
                Canvas.SetTop(coreRect, padY);
                CanvasPlanPreview.Children.Add(coreRect);

                if (showFinishWalls)
                {
                    var finRect = new System.Windows.Shapes.Rectangle
                    {
                        Width = Math.Max(10, rectW - 14),
                        Height = Math.Max(10, rectH - 14),
                        Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 189, 248)),
                        StrokeThickness = 3.5,
                        Fill = Brushes.Transparent
                    };
                    Canvas.SetLeft(finRect, padX + 7);
                    Canvas.SetTop(finRect, padY + 7);
                    CanvasPlanPreview.Children.Add(finRect);
                }
            }

            // Door break indicator on right edge
            if (breakDoors)
            {
                double doorX = w - padX - 7;
                double doorY1 = h * 0.38;
                double doorY2 = h * 0.58;
                var breakGap = new System.Windows.Shapes.Line
                {
                    X1 = doorX, Y1 = doorY1, X2 = doorX, Y2 = doorY2,
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
                    StrokeThickness = 8
                };
                CanvasPlanPreview.Children.Add(breakGap);

                TextBlock doorLbl = new TextBlock
                {
                    Text = "Door Break",
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(doorLbl, doorX - 58);
                Canvas.SetTop(doorLbl, (doorY1 + doorY2) * 0.5 - 7);
                CanvasPlanPreview.Children.Add(doorLbl);
            }

            // Center label
            TextBlock centerTxt = new TextBlock
            {
                Text = selRoom != null ? $"{selRoom.DisplayName}\n{selRoom.AreaSqm:0.00} m²" : "Room Area",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center
            };
            centerTxt.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(centerTxt, (w - centerTxt.DesiredSize.Width) * 0.5);
            Canvas.SetTop(centerTxt, (h - centerTxt.DesiredSize.Height) * 0.5);
            CanvasPlanPreview.Children.Add(centerTxt);
        }

        private void RenderSectionPreview()
        {
            if (CanvasSectionPreview == null) return;
            CanvasSectionPreview.Children.Clear();

            double w = CanvasSectionPreview.ActualWidth;
            double h = CanvasSectionPreview.ActualHeight;
            if (w < 60 || h < 60) return;

            double.TryParse(TxtCeilingHeight?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double ceilHmm);
            if (ceilHmm <= 0) ceilHmm = 2700;
            double.TryParse(TxtCeilingVoidOffset?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double voidMm);
            double.TryParse(TxtFloorOffset?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double floorOffMm);

            bool createWalls = ChkCreateWalls?.IsChecked == true;
            bool createFloors = ChkCreateFloors?.IsChecked == true;
            bool createCeilings = ChkCreateCeilings?.IsChecked == true;
            bool createSkirting = ChkCreateSkirting?.IsChecked == true;
            bool baseOnSlab = RbWallBaseSlab?.IsChecked == true;
            bool topAtCeiling = RbWallTopCeiling?.IsChecked == true;
            bool skirtInFront = RbSkirtInFront?.IsChecked == true;

            double leftMargin = 28;
            double coreWallW = 34;
            double slabH = 22;
            double topSlabY = 12;
            double botSlabY = h - slabH - 12;
            double coreWallH = botSlabY - (topSlabY + slabH);
            double spanW = w - leftMargin - 20;

            // 1. Slab Above (+3000)
            AddSectionRect(leftMargin, topSlabY, spanW, slabH, Color.FromRgb(203, 213, 225));
            AddSectionText("Slab Above (+3000)", leftMargin + spanW * 0.5, topSlabY + 4, Color.FromRgb(51, 65, 85), true);

            // 2. Structural Wall Core (Left vertical)
            AddSectionRect(leftMargin, topSlabY + slabH, coreWallW, coreWallH, Color.FromRgb(226, 232, 240));

            // 3. Slab Below (0.00)
            AddSectionRect(leftMargin, botSlabY, spanW, slabH, Color.FromRgb(203, 213, 225));
            AddSectionText("Slab Below (0.00)", leftMargin + spanW * 0.5, botSlabY + 4, Color.FromRgb(51, 65, 85), true);

            // Calculate positions for Finish Floor & Ceiling
            double finishFloorH = 13;
            double finishFloorY = botSlabY - finishFloorH;
            double ceilingY = topSlabY + slabH + coreWallH * 0.22;
            double ceilingH = 12;

            // 4. Ceiling Layer (#818CF8)
            if (createCeilings)
            {
                AddSectionRect(leftMargin + coreWallW, ceilingY, spanW - coreWallW, ceilingH, Color.FromRgb(129, 140, 248));
                AddSectionText($"Ceiling (+{ceilHmm:0})", leftMargin + coreWallW + (spanW - coreWallW) * 0.52, ceilingY - 1, Colors.White, true);
            }

            // 5. Finish Floor Layer (#FDE047)
            if (createFloors)
            {
                AddSectionRect(leftMargin + coreWallW, finishFloorY, spanW - coreWallW, finishFloorH, Color.FromRgb(253, 224, 71));
                AddSectionText($"Fin. Floor (+{floorOffMm + 100:0})", leftMargin + coreWallW + (spanW - coreWallW) * 0.52, finishFloorY - 1, Color.FromRgb(15, 23, 42), true);
            }

            // 6. Finish Wall Layer (#38BDF8)
            double wallX = leftMargin + coreWallW;
            double wallW = 12;
            double wallTopY = topAtCeiling ? (ceilingY - 12) : (topSlabY + slabH);
            double wallBotY = (baseOnSlab || !createFloors) ? botSlabY : finishFloorY;
            if (createSkirting && !skirtInFront)
            {
                wallBotY -= 14; // Flush skirting sits below finish wall
            }

            if (createWalls && wallBotY > wallTopY + 10)
            {
                AddSectionRect(wallX, wallTopY, wallW, wallBotY - wallTopY, Color.FromRgb(56, 189, 248));
                AddSectionText("Wall Finish", wallX + wallW + 6, (wallTopY + wallBotY) * 0.5 - 7, Color.FromRgb(51, 65, 85), false);

                if (topAtCeiling)
                {
                    var dashLine = new System.Windows.Shapes.Line
                    {
                        X1 = leftMargin, Y1 = wallTopY, X2 = wallX + wallW, Y2 = wallTopY,
                        Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2, 2 }
                    };
                    CanvasSectionPreview.Children.Add(dashLine);
                    AddSectionText($"+{voidMm:0} Void", leftMargin + 2, wallTopY - 14, Color.FromRgb(100, 116, 139), false, 9.5);
                }
            }

            // 7. Skirting Layer (#F59E0B)
            if (createSkirting)
            {
                double skW = 8;
                double skH = 14;
                double skX = skirtInFront ? (wallX + (createWalls ? wallW : 0)) : wallX;
                double skY = (createFloors ? finishFloorY : botSlabY) - skH;
                AddSectionRect(skX, skY, skW, skH, Color.FromRgb(245, 158, 11));
            }
        }

        private void AddSectionRect(double x, double y, double w, double h, System.Windows.Media.Color fill)
        {
            var r = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Max(2, w),
                Height = Math.Max(2, h),
                Fill = new SolidColorBrush(fill)
            };
            Canvas.SetLeft(r, x);
            Canvas.SetTop(r, y);
            CanvasSectionPreview.Children.Add(r);
        }

        private void AddSectionText(string text, double x, double y, System.Windows.Media.Color color, bool centerHoriz, double fontSize = 10.5)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                FontFamily = new FontFamily("Consolas"),
                FontSize = fontSize,
                FontWeight = FontWeights.Bold
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(tb, centerHoriz ? (x - tb.DesiredSize.Width * 0.5) : x);
            Canvas.SetTop(tb, y);
            CanvasSectionPreview.Children.Add(tb);
        }
    }
}
