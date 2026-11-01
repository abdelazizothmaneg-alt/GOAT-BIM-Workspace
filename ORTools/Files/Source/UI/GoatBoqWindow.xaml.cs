using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using ORTools.Models;

namespace ORTools.UI
{
    public class BoqRowItem
    {
        public bool IsSelected { get; set; } = true;
        public string ItemCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string TypeDescription { get; set; } = string.Empty;
        public string LevelName { get; set; } = "All Levels";
        public int Count { get; set; }
        public double TotalLengthM { get; set; }
        public double TotalAreaSqm { get; set; }
        public double TotalVolumeCum { get; set; }
        public string PrimaryUnit { get; set; } = "m²";
        public double PrimaryQuantity { get; set; }
    }

    public class GoatBoqEventHandler : IExternalEventHandler
    {
        public GoatBoqWindow Window { get; set; }

        public void Execute(UIApplication uiapp)
        {
            if (Window == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc?.Document == null) return;
            Document doc = uidoc.Document;

            try
            {
                bool activeOnly = Window.RbActiveView.IsChecked == true;
                bool byLevel = Window.ChkGroupByLevel.IsChecked == true;

                var targetCategories = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Ceilings,
                    BuiltInCategory.OST_Roofs,
                    BuiltInCategory.OST_Doors,
                    BuiltInCategory.OST_Windows,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_StructuralFraming,
                    BuiltInCategory.OST_Columns,
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_Casework,
                    BuiltInCategory.OST_PlumbingFixtures,
                    BuiltInCategory.OST_LightingFixtures
                };

                FilteredElementCollector col = (activeOnly && doc.ActiveView != null)
                    ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                    : new FilteredElementCollector(doc);

                var multiFilter = new LogicalOrFilter(targetCategories.Select(c => (ElementFilter)new ElementCategoryFilter(c)).ToList());
                var elements = col.WhereElementIsNotElementType().WherePasses(multiFilter).ToList();

                var rawItems = new List<(string Cat, string TypeName, string Lvl, double LenM, double AreaSqm, double VolCum)>();

                foreach (Element el in elements)
                {
                    if (el.Category == null) continue;
                    string cat = el.Category.Name;
                    string typeName = GetFamilyAndTypeName(doc, el);
                    string lvlName = byLevel ? GetLevelName(doc, el) : "All Levels";

                    double lenM = GetMetricParam(el, BuiltInParameter.CURVE_ELEM_LENGTH, UnitTypeId.Meters);
                    double areaSqm = GetMetricParam(el, BuiltInParameter.HOST_AREA_COMPUTED, UnitTypeId.SquareMeters);
                    double volCum = GetMetricParam(el, BuiltInParameter.HOST_VOLUME_COMPUTED, UnitTypeId.CubicMeters);

                    rawItems.Add((cat, typeName, lvlName, lenM, areaSqm, volCum));
                }

                var grouped = rawItems
                    .GroupBy(x => (x.Cat, x.TypeName, x.Lvl))
                    .OrderBy(g => g.Key.Cat)
                    .ThenBy(g => g.Key.TypeName)
                    .ThenBy(g => g.Key.Lvl)
                    .ToList();

                var rows = new List<BoqRowItem>();
                int index = 1;
                foreach (var g in grouped)
                {
                    int cnt = g.Count();
                    double sumLen = Math.Round(g.Sum(x => x.LenM), 2);
                    double sumArea = Math.Round(g.Sum(x => x.AreaSqm), 2);
                    double sumVol = Math.Round(g.Sum(x => x.VolCum), 2);

                    string unit = "No.";
                    double primaryQty = cnt;
                    if (sumArea > 0.01) { unit = "m²"; primaryQty = sumArea; }
                    else if (sumVol > 0.01) { unit = "m³"; primaryQty = sumVol; }
                    else if (sumLen > 0.01) { unit = "m.l"; primaryQty = sumLen; }

                    rows.Add(new BoqRowItem
                    {
                        IsSelected = true,
                        ItemCode = $"BOQ-{index:000}",
                        CategoryName = g.Key.Cat,
                        TypeDescription = g.Key.TypeName,
                        LevelName = g.Key.Lvl,
                        Count = cnt,
                        TotalLengthM = sumLen,
                        TotalAreaSqm = sumArea,
                        TotalVolumeCum = sumVol,
                        PrimaryUnit = unit,
                        PrimaryQuantity = primaryQty
                    });
                    index++;
                }

                Window.ApplyScannedBoq(rows, elements.Count);
            }
            catch (Exception ex)
            {
                Window.TxtStatus.Text = $"Error: {ex.Message}";
            }
        }

        public string GetName() => "GOAT BOQ External Event Handler";

        private static string GetFamilyAndTypeName(Document doc, Element el)
        {
            ElementId tid = el.GetTypeId();
            if (tid != ElementId.InvalidElementId && doc.GetElement(tid) is ElementType et)
            {
                return !string.IsNullOrWhiteSpace(et.FamilyName)
                    ? $"{et.FamilyName} : {et.Name}"
                    : et.Name;
            }
            return el.Name ?? "Unnamed";
        }

        private static string GetLevelName(Document doc, Element el)
        {
            if (el.LevelId != ElementId.InvalidElementId && doc.GetElement(el.LevelId) is Level l)
                return l.Name;
            Parameter p = el.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                       ?? el.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)
                       ?? el.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
            if (p != null && p.AsElementId() != ElementId.InvalidElementId && doc.GetElement(p.AsElementId()) is Level l2)
                return l2.Name;
            return "Unassigned";
        }

        private static double GetMetricParam(Element el, BuiltInParameter bip, ForgeTypeId unitId)
        {
            try
            {
                Parameter p = el.get_Parameter(bip);
                if (p != null && p.StorageType == StorageType.Double && p.AsDouble() > 0)
                {
                    return UnitUtils.ConvertFromInternalUnits(p.AsDouble(), unitId);
                }
            }
            catch { }
            return 0.0;
        }
    }

    public partial class GoatBoqWindow : Window
    {
        private readonly GoatBoqEventHandler _handler;
        private readonly ExternalEvent _extEvent;
        private List<BoqRowItem> _allRows = new List<BoqRowItem>();
        public ObservableCollection<BoqRowItem> DisplayedRows { get; } = new ObservableCollection<BoqRowItem>();
        private bool _isLoaded = false;

        public GoatBoqWindow(GoatBoqEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            DgBoq.ItemsSource = DisplayedRows;

            Loaded += (s, e) =>
            {
                _isLoaded = true;
                _extEvent.Raise();
            };
        }

        public void ApplyScannedBoq(List<BoqRowItem> rows, int totalElements)
        {
            _allRows = rows ?? new List<BoqRowItem>();
            ApplyFilter();
            TxtStatus.Text = $"✔ Computed QTO for {totalElements} model elements grouped into {_allRows.Count} BOQ line items.";
        }

        private void ApplyFilter()
        {
            string q = TxtFilter?.Text?.Trim() ?? "";
            DisplayedRows.Clear();
            foreach (var r in _allRows)
            {
                if (string.IsNullOrEmpty(q) ||
                    r.CategoryName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.TypeDescription.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.LevelName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.ItemCode.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    DisplayedRows.Add(r);
                }
            }
        }

        private void Scope_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            _extEvent.Raise();
        }

        private void BtnRescan_Click(object sender, RoutedEventArgs e) => _extEvent.Raise();
        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnExportXlsx_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = DisplayedRows.Where(r => r.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("No BOQ items selected for export.", "GOAT BOQ", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Export GOAT BOQ Bill of Quantities (.xlsx)",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"GOAT_Model_BOQ_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (sfd.ShowDialog() != true) return;

                var sheet = new GoatExcelSheet
                {
                    Name = "Model BOQ Schedule",
                    ColumnWidths = new List<double> { 14, 18, 36, 16, 12, 14, 15, 14, 12, 16 }
                };

                sheet.Rows.Add(new List<GoatExcelCell>
                {
                    new GoatExcelCell("BOQ Code", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Category", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Family & Type Description", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Level", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Count (pcs)", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Length (m)", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Net Area (m²)", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Volume (m³)", GoatCellStyle.HeaderNavy),
                    new GoatExcelCell("Primary Unit", GoatCellStyle.HeaderTeal),
                    new GoatExcelCell("Primary Quantity", GoatCellStyle.HeaderTeal)
                });

                foreach (var r in selected)
                {
                    sheet.Rows.Add(new List<GoatExcelCell>
                    {
                        new GoatExcelCell(r.ItemCode, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(r.CategoryName, GoatCellStyle.ReadOnlyGrey),
                        new GoatExcelCell(r.TypeDescription, GoatCellStyle.NormalEditable),
                        new GoatExcelCell(r.LevelName, GoatCellStyle.ReadOnlyGrey),
                        new GoatExcelCell(r.Count.ToString(CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, true),
                        new GoatExcelCell(r.TotalLengthM.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, true),
                        new GoatExcelCell(r.TotalAreaSqm.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, true),
                        new GoatExcelCell(r.TotalVolumeCum.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, true),
                        new GoatExcelCell(r.PrimaryUnit, GoatCellStyle.SummaryBold),
                        new GoatExcelCell(r.PrimaryQuantity.ToString("0.00", CultureInfo.InvariantCulture), GoatCellStyle.SummaryBold, true)
                    });
                }

                GoatExcelEngine.WriteXlsx(sfd.FileName, new List<GoatExcelSheet> { sheet });
                TxtStatus.Text = $"✔ Exported {selected.Count} BOQ items to {System.IO.Path.GetFileName(sfd.FileName)}";

                try
                {
                    Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"BOQ Export Error:\n{ex.Message}", "GOAT BOQ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
