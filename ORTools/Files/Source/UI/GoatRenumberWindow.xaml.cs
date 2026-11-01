using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace ORTools.UI
{
    public class RenumberRowItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _elementTitle = string.Empty;
        private string _proposedNumber = string.Empty;

        public ElementId ElementId { get; set; } = ElementId.InvalidElementId;
        public long IdValue => ElementId.Value;

        public string ElementTitle
        {
            get => _elementTitle;
            set { _elementTitle = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string HostRoomNumber { get; set; } = string.Empty;
        public string CurrentNumber { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string ProposedNumber
        {
            get => _proposedNumber;
            set { _proposedNumber = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class GoatRenumberEventHandler : IExternalEventHandler
    {
        public GoatRenumberWindow Window { get; set; }
        public bool IsScanOnly { get; set; }

        public void Execute(UIApplication uiapp)
        {
            if (Window == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc?.Document == null) return;
            Document doc = uidoc.Document;

            try
            {
                if (IsScanOnly)
                {
                    ExecuteScan(doc);
                    return;
                }

                if (Window.RbModeInteractivePick.IsChecked == true)
                {
                    ExecuteInteractivePick(uidoc, doc);
                }
                else
                {
                    ExecuteBatchApply(doc);
                }
            }
            catch (Exception ex)
            {
                Window.TxtStatus.Text = $"Error: {ex.Message}";
            }
        }

        public string GetName() => "GOAT RENUMBER External Event Handler";

        private void ExecuteScan(Document doc)
        {
            int catIdx = Window.CbCategory.SelectedIndex >= 0 ? Window.CbCategory.SelectedIndex : 0;
            bool activeOnly = Window.RbScopeActiveView.IsChecked == true;

            FilteredElementCollector col = (activeOnly && doc.ActiveView != null && catIdx != 4)
                ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                : new FilteredElementCollector(doc);

            var elements = new List<Element>();
            switch (catIdx)
            {
                case 0: // Rooms
                    elements = col.OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToList();
                    break;
                case 1: // Doors
                    elements = col.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToList();
                    break;
                case 2: // Windows
                    elements = col.OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().ToList();
                    break;
                case 3: // Grids
                    elements = col.OfClass(typeof(Autodesk.Revit.DB.Grid)).ToList();
                    break;
                case 4: // Sheets
                    elements = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s => !s.IsPlaceholder).Cast<Element>().ToList();
                    break;
                case 5: // Viewports on Active Sheet
                    if (doc.ActiveView is ViewSheet activeSheet)
                    {
                        elements = activeSheet.GetAllViewports().Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
                    }
                    break;
                case 6: // Furniture & Casework
                    elements = col.WhereElementIsNotElementType()
                        .WherePasses(new LogicalOrFilter(
                            new ElementCategoryFilter(BuiltInCategory.OST_Furniture),
                            new ElementCategoryFilter(BuiltInCategory.OST_Casework)))
                        .ToList();
                    break;
                case 7: // Parking
                    elements = col.OfCategory(BuiltInCategory.OST_Parking).WhereElementIsNotElementType().ToList();
                    break;
            }

            // Collect Rooms for spatial lookup if non-door/window
            var allRooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<Room>()
                .Where(r => r.Area > 0.001)
                .ToList();

            var rows = new List<RenumberRowItem>();
            foreach (Element el in elements)
            {
                XYZ pt = GetElementPosition(doc, el);
                string currentVal = GetElementNumberValue(el, catIdx);
                string hostRoomNum = GetElementHostRoomNumber(el, pt, allRooms);
                string title = GetElementDisplayTitle(doc, el);

                rows.Add(new RenumberRowItem
                {
                    ElementId = el.Id,
                    ElementTitle = title,
                    HostRoomNumber = hostRoomNum,
                    CurrentNumber = currentVal,
                    X = Math.Round(pt.X, 2),
                    Y = Math.Round(pt.Y, 2),
                    IsSelected = true
                });
            }

            Window.LoadScannedRows(rows);
        }

        private void ExecuteBatchApply(Document doc)
        {
            int catIdx = Window.CbCategory.SelectedIndex >= 0 ? Window.CbCategory.SelectedIndex : 0;
            var selectedRows = Window.Rows.Where(r => r.IsSelected && !string.IsNullOrWhiteSpace(r.ProposedNumber)).ToList();
            if (selectedRows.Count == 0) return;

            int updated = 0;
            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT RENUMBER — Batch Renumber"))
            {
                tg.Start();

                // PASS 1: Temporary unique prefix to eliminate any Revit duplicate Mark/Number/Name conflicts
                using (Transaction t1 = new Transaction(doc, "Pass 1 Temp Swap"))
                {
                    t1.Start();
                    int tmpIdx = 1;
                    foreach (var row in selectedRows)
                    {
                        Element el = doc.GetElement(row.ElementId);
                        if (el != null)
                        {
                            SetElementNumberValue(el, catIdx, $"__GOAT_TMP_{tmpIdx++}_{row.IdValue}");
                        }
                    }
                    t1.Commit();
                }

                // PASS 2: Assign final proposed numbers AND updated Room/Element Names
                using (Transaction t2 = new Transaction(doc, "Pass 2 Final Numbers & Names"))
                {
                    t2.Start();
                    foreach (var row in selectedRows)
                    {
                        Element el = doc.GetElement(row.ElementId);
                        if (el != null)
                        {
                            SetElementDisplayTitle(doc, el, row.ElementTitle);
                            if (SetElementNumberValue(el, catIdx, row.ProposedNumber))
                            {
                                row.CurrentNumber = row.ProposedNumber;
                                updated++;
                            }
                        }
                    }
                    t2.Commit();
                }

                tg.Assimilate();
            }

            Window.DgPreview.Items.Refresh();
            Window.TxtStatus.Text = $"✔ Successfully renumbered & updated names on {updated} elements with zero duplicate conflicts! (1x Ctrl+Z to Undo)";
        }

        private void ExecuteInteractivePick(UIDocument uidoc, Document doc)
        {
            int catIdx = Window.CbCategory.SelectedIndex >= 0 ? Window.CbCategory.SelectedIndex : 0;
            string prefix = Window.TxtPrefix.Text ?? "";
            string suffix = Window.TxtSuffix.Text ?? "";
            int.TryParse(Window.TxtStartNum.Text, out int currentNum);
            if (currentNum <= 0) currentNum = 1;
            int.TryParse(Window.TxtStep.Text, out int step);
            if (step == 0) step = 1;
            int.TryParse(Window.TxtPadding.Text, out int pad);
            pad = Math.Clamp(pad, 1, 8);

            int pickedCount = 0;
            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT RENUMBER — Interactive Pick"))
            {
                tg.Start();
                while (true)
                {
                    Reference pickedRef = null;
                    string nextVal = $"{prefix}{currentNum.ToString().PadLeft(pad, '0')}{suffix}";
                    try
                    {
                        pickedRef = uidoc.Selection.PickObject(ObjectType.Element, $"Click element to assign '{nextVal}' (Press ESC to finish)");
                    }
                    catch
                    {
                        break;
                    }

                    if (pickedRef == null) break;
                    Element el = doc.GetElement(pickedRef.ElementId);
                    if (el == null) continue;

                    using (Transaction t = new Transaction(doc, $"Assign {nextVal}"))
                    {
                        t.Start();
                        if (SetElementNumberValue(el, catIdx, nextVal))
                        {
                            currentNum += step;
                            pickedCount++;
                        }
                        t.Commit();
                    }
                }
                tg.Assimilate();
            }

            ExecuteScan(doc);
            Window.TxtStatus.Text = $"✔ Interactive Pick finished: renumbered {pickedCount} elements. (1x Ctrl+Z to Undo)";
        }

        private static XYZ GetElementPosition(Document doc, Element el)
        {
            if (el is Viewport vp) return vp.GetBoxCenter();
            if (el.Location is LocationPoint lp) return lp.Point;
            if (el.Location is LocationCurve lc && lc.Curve != null) return lc.Curve.Evaluate(0.5, true);
            BoundingBoxXYZ bb = el.get_BoundingBox(null);
            if (bb != null) return (bb.Min + bb.Max) * 0.5;
            return XYZ.Zero;
        }

        private static string GetElementDisplayTitle(Document doc, Element el)
        {
            if (el is Room r)
                return r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? r.Name ?? "Room";
            if (el is ViewSheet vs)
                return vs.Name;
            if (el is Viewport vp)
                return doc.GetElement(vp.ViewId)?.Name ?? "Viewport";
            if (el is FamilyInstance fi)
                return $"{fi.Symbol?.FamilyName} : {fi.Name}";
            return el.Name ?? el.Category?.Name ?? "Element";
        }

        private static void SetElementDisplayTitle(Document doc, Element el, string newTitle)
        {
            if (el == null || string.IsNullOrWhiteSpace(newTitle)) return;
            string clean = newTitle.Trim();
            try
            {
                if (el is Room r)
                {
                    Parameter rn = r.get_Parameter(BuiltInParameter.ROOM_NAME);
                    if (rn != null && !rn.IsReadOnly) rn.Set(clean);
                    else r.Name = clean;
                    return;
                }
                if (el is ViewSheet vs)
                {
                    vs.Name = clean;
                    return;
                }
                if (el is Viewport vp)
                {
                    if (doc.GetElement(vp.ViewId) is View v)
                        v.Name = clean;
                    return;
                }
                if (el is FamilyInstance fi && fi.Symbol != null)
                {
                    if (clean.Contains(":"))
                    {
                        string[] parts = clean.Split(new[] { ':' }, 2);
                        string typeName = parts[1].Trim();
                        if (!string.IsNullOrEmpty(typeName) && !string.Equals(fi.Symbol.Name, typeName, StringComparison.Ordinal))
                            fi.Symbol.Name = typeName;
                    }
                    else if (!string.Equals(fi.Symbol.Name, clean, StringComparison.Ordinal))
                    {
                        fi.Symbol.Name = clean;
                    }
                }
            }
            catch { }
        }

        private static string GetElementHostRoomNumber(Element el, XYZ pt, List<Room> allRooms)
        {
            if (el is Room rm) return rm.Number ?? "";
            if (el is FamilyInstance fi)
            {
                try
                {
                    if (fi.ToRoom != null && !string.IsNullOrWhiteSpace(fi.ToRoom.Number))
                        return fi.ToRoom.Number;
                    if (fi.FromRoom != null && !string.IsNullOrWhiteSpace(fi.FromRoom.Number))
                        return fi.FromRoom.Number;
                    if (fi.Room != null && !string.IsNullOrWhiteSpace(fi.Room.Number))
                        return fi.Room.Number;
                }
                catch { }
            }

            foreach (Room r in allRooms)
            {
                try
                {
                    if (r.IsPointInRoom(pt)) return r.Number ?? "";
                }
                catch { }
            }
            return "";
        }

        private static string GetElementNumberValue(Element el, int catIdx)
        {
            if (el is Room r) return r.Number ?? "";
            if (el is Autodesk.Revit.DB.Grid g) return g.Name ?? "";
            if (el is ViewSheet vs) return vs.SheetNumber ?? "";
            if (el is Viewport vp)
                return vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.AsString() ?? "";
            return el.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
        }

        private static bool SetElementNumberValue(Element el, int catIdx, string val)
        {
            try
            {
                if (el is Room r) { r.Number = val; return true; }
                if (el is Autodesk.Revit.DB.Grid g) { g.Name = val; return true; }
                if (el is ViewSheet vs) { vs.SheetNumber = val; return true; }
                if (el is Viewport vp)
                {
                    Parameter p = vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);
                    if (p != null && !p.IsReadOnly) { p.Set(val); return true; }
                    return false;
                }
                Parameter markParam = el.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (markParam != null && !markParam.IsReadOnly)
                {
                    markParam.Set(val);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }

    public partial class GoatRenumberWindow : Window
    {
        private readonly GoatRenumberEventHandler _handler;
        private readonly ExternalEvent _extEvent;
        private bool _isLoaded = false;

        public ObservableCollection<RenumberRowItem> Rows { get; } = new ObservableCollection<RenumberRowItem>();

        public GoatRenumberWindow(GoatRenumberEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            DgPreview.ItemsSource = Rows;

            Loaded += (s, e) =>
            {
                _isLoaded = true;
                TriggerScan();
            };
        }

        public void LoadScannedRows(List<RenumberRowItem> scanned)
        {
            Rows.Clear();
            foreach (var r in scanned) Rows.Add(r);
            RecalculateProposedNumbers();
        }

        private void TriggerScan()
        {
            if (!_isLoaded) return;
            _handler.IsScanOnly = true;
            _extEvent.Raise();
        }

        private void RecalculateProposedNumbers()
        {
            if (Rows.Count == 0) return;

            string prefix = TxtPrefix.Text ?? "";
            string suffix = TxtSuffix.Text ?? "";
            int.TryParse(TxtStartNum.Text, out int startNum);
            if (startNum <= 0) startNum = 1;
            int.TryParse(TxtStep.Text, out int step);
            if (step == 0) step = 1;
            int.TryParse(TxtPadding.Text, out int pad);
            pad = Math.Clamp(pad, 1, 8);

            bool byRoom = RbModeByHostRoom.IsChecked == true;
            bool alphaRoomSuffix = CbRoomSuffixStyle.SelectedIndex == 0;

            // Sort rows according to chosen spatial scan
            List<RenumberRowItem> sorted;
            if (RbModeSpatialYX.IsChecked == true)
            {
                sorted = Rows.OrderByDescending(r => Math.Round(r.Y / 2.0)).ThenBy(r => r.X).ToList();
            }
            else
            {
                sorted = Rows.OrderBy(r => Math.Round(r.Y / 2.0)).ThenBy(r => r.X).ToList();
            }

            Rows.Clear();
            foreach (var item in sorted) Rows.Add(item);

            if (byRoom)
            {
                var groups = Rows.Where(r => r.IsSelected).GroupBy(r => string.IsNullOrWhiteSpace(r.HostRoomNumber) ? "UNASSIGNED" : r.HostRoomNumber);
                foreach (var grp in groups)
                {
                    var items = grp.ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        string roomNum = grp.Key;
                        if (items.Count == 1)
                        {
                            items[i].ProposedNumber = $"{prefix}{roomNum}{suffix}";
                        }
                        else
                        {
                            string multiTag = alphaRoomSuffix
                                ? ((char)('A' + (i % 26))).ToString()
                                : $"-{i + 1}";
                            items[i].ProposedNumber = $"{prefix}{roomNum}{multiTag}{suffix}";
                        }
                    }
                }
            }
            else
            {
                int current = startNum;
                foreach (var item in Rows)
                {
                    if (!item.IsSelected)
                    {
                        item.ProposedNumber = "";
                        continue;
                    }
                    item.ProposedNumber = $"{prefix}{current.ToString().PadLeft(pad, '0')}{suffix}";
                    current += step;
                }
            }

            if (BtnApply != null)
            {
                BtnApply.Content = RbModeInteractivePick.IsChecked == true
                    ? "🎯 Start Interactive Pick in Revit"
                    : "🚀 Apply Renumbering";
            }
        }

        private void Config_Changed(object sender, SelectionChangedEventArgs e) => TriggerScan();
        private void Scope_Changed(object sender, RoutedEventArgs e) => TriggerScan();
        private void Rule_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            RecalculateProposedNumbers();
        }
        private void Format_Changed(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            RecalculateProposedNumbers();
        }
        private void BtnRescan_Click(object sender, RoutedEventArgs e) => TriggerScan();

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            _handler.IsScanOnly = false;
            _extEvent.Raise();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
