using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ORTools.Commands;
using ORTools.Models;

namespace ORTools.UI
{
    public class GoatRoomEventHandler : IExternalEventHandler
    {
        public GoatRoomWindow Window { get; set; }
        public bool IsScanOnly { get; set; }
        public bool IsApplyEditsAndColorsOnly { get; set; }

        private static readonly string[] DefaultPaletteHex =
        {
            "#2E5A44", "#4B3F5E", "#1D4E56", "#5C4033", "#3B4D6B",
            "#4A5D23", "#5E3A4D", "#10B981", "#0284C7", "#D97706"
        };

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

                if (IsApplyEditsAndColorsOnly)
                {
                    ExecuteApplyEditsAndColors(doc);
                    return;
                }

                ExecuteGenerateBundle(doc);
            }
            catch (Exception ex)
            {
                Window.TxtStatus.Text = $"Error: {ex.Message}";
            }
            finally
            {
                IsScanOnly = false;
                IsApplyEditsAndColorsOnly = false;
            }
        }

        public string GetName() => "GOAT ROOM Bundle External Event Handler";

        private void ExecuteScan(Document doc)
        {
            var titleBlocks = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .OfType<FamilySymbol>()
                .OrderBy(t => t.FamilyName)
                .ThenBy(t => t.Name)
                .Select(t => new ElementTypeOption { Id = t.Id, Name = $"{t.FamilyName} : {t.Name}" })
                .ToList();

            bool activeOnly = Window.RbActiveView.IsChecked == true;
            FilteredElementCollector rc = (activeOnly && doc.ActiveView != null)
                ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                : new FilteredElementCollector(doc);

            var rawRooms = rc.OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<Room>()
                .Where(r => r.Area > 0.001 && r.Location != null)
                .OrderBy(r => r.Number)
                .ToList();

            var nameColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int colorIdx = 0;

            var rooms = new List<CeilingRoomItem>();
            foreach (Room r in rawRooms)
            {
                string rName = r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? r.Name ?? "Room";
                if (!nameColorMap.TryGetValue(rName, out string hex))
                {
                    hex = DefaultPaletteHex[colorIdx % DefaultPaletteHex.Length];
                    nameColorMap[rName] = hex;
                    colorIdx++;
                }

                rooms.Add(new CeilingRoomItem
                {
                    IsSelected = true,
                    RoomId = r.Id,
                    LevelId = r.LevelId,
                    RoomNumber = r.Number ?? "",
                    RoomName = rName,
                    FillColorHex = hex,
                    AreaSqm = Math.Round(UnitUtils.ConvertFromInternalUnits(r.Area, UnitTypeId.SquareMeters), 2)
                });
            }

            Window.LoadScannedData(titleBlocks, rooms);
        }

        private void ExecuteApplyEditsAndColors(Document doc)
        {
            var selectedRooms = Window.RoomList.Where(r => r.IsSelected).ToList();
            if (selectedRooms.Count == 0)
            {
                Window.TxtStatus.Text = "Please select at least one room to update.";
                return;
            }

            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ROOM — Update Room Names, Numbers & Fill Colors"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Apply Room Names, Numbers & View Fill Colors"))
                {
                    trans.Start();
                    ApplyRoomNamesNumbersAndViewColorsInternal(doc, doc.ActiveView, selectedRooms);
                    trans.Commit();
                }
                tg.Assimilate();
            }

            Window.DgRooms.Items.Refresh();
            Window.TxtStatus.Text = $"✔ Updated Room Names, Numbers & Fill Colors for {selectedRooms.Count} rooms in Active View! (1x Ctrl+Z to Undo)";
        }

        internal static void ApplyRoomNamesNumbersAndViewColorsInternal(Document doc, View targetView, List<CeilingRoomItem> roomsToUpdate)
        {
            if (doc == null || roomsToUpdate == null || roomsToUpdate.Count == 0) return;

            // 1. Two-pass update of Room Number & Name to prevent duplicate number warnings
            int tmpIdx = 1;
            foreach (var item in roomsToUpdate)
            {
                if (doc.GetElement(item.RoomId) is Room rm && !string.IsNullOrWhiteSpace(item.RoomNumber))
                {
                    if (!string.Equals(rm.Number, item.RoomNumber, StringComparison.Ordinal))
                    {
                        try { rm.Number = $"__GOAT_RM_{tmpIdx++}_{item.RoomId.Value}"; } catch { }
                    }
                }
            }

            foreach (var item in roomsToUpdate)
            {
                if (doc.GetElement(item.RoomId) is Room rm)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(item.RoomNumber))
                            rm.Number = item.RoomNumber.Trim();
                    }
                    catch { }

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(item.RoomName))
                        {
                            Parameter pName = rm.get_Parameter(BuiltInParameter.ROOM_NAME);
                            if (pName != null && !pName.IsReadOnly)
                                pName.Set(item.RoomName.Trim());
                            else
                                rm.Name = item.RoomName.Trim();
                        }
                    }
                    catch { }
                }
            }

            if (targetView == null) return;

            // 2. Find Solid Fill Pattern Id
            ElementId solidFillId = ElementId.InvalidElementId;
            var fillPatterns = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>();
            foreach (var fp in fillPatterns)
            {
                if (fp.GetFillPattern().IsSolidFill)
                {
                    solidFillId = fp.Id;
                    break;
                }
            }

            // Ensure Rooms, Interior Fill & Color Fill subcategories are visible in targetView
            try
            {
                Category roomCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Rooms);
                if (roomCat != null)
                {
                    targetView.SetCategoryHidden(roomCat.Id, false);
                    foreach (Category subCat in roomCat.SubCategories)
                    {
                        if (subCat.Name.IndexOf("Fill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            subCat.Name.IndexOf("Interior", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            subCat.Name.IndexOf("Color", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            try { targetView.SetCategoryHidden(subCat.Id, false); } catch { }
                        }
                    }
                }
            }
            catch { }

            // 3. Apply per-room OverrideGraphicSettings in targetView
            foreach (var item in roomsToUpdate)
            {
                Autodesk.Revit.DB.Color revitColor = ParseHexToRevitColor(item.FillColorHex);
                var ogs = new OverrideGraphicSettings();
                ogs.SetProjectionLineColor(revitColor);
                if (solidFillId != ElementId.InvalidElementId)
                {
                    ogs.SetSurfaceForegroundPatternId(solidFillId);
                    ogs.SetSurfaceForegroundPatternColor(revitColor);
                    ogs.SetSurfaceBackgroundPatternId(solidFillId);
                    ogs.SetSurfaceBackgroundPatternColor(revitColor);
                }
                try { targetView.SetElementOverrides(item.RoomId, ogs); } catch { }
            }

            // 4. Also configure / apply native Revit ColorFillScheme on targetView (for Floor Plan / Section views)
            try
            {
                ElementId roomCatId = new ElementId(BuiltInCategory.OST_Rooms);
                if (targetView.CanCategoryBeHidden(roomCatId))
                {
                    var schemes = new FilteredElementCollector(doc)
                        .OfClass(typeof(ColorFillScheme))
                        .Cast<ColorFillScheme>()
                        .Where(s => s.CategoryId == roomCatId)
                        .ToList();

                    if (schemes.Count > 0)
                    {
                        ColorFillScheme goatScheme = schemes.FirstOrDefault(s => s.Name.Equals("GOAT Room Color Scheme", StringComparison.OrdinalIgnoreCase));
                        if (goatScheme == null)
                        {
                            ElementId dupId = schemes[0].Duplicate("GOAT Room Color Scheme");
                            goatScheme = doc.GetElement(dupId) as ColorFillScheme;
                        }

                        if (goatScheme != null)
                        {
                            goatScheme.Title = "Room Color Fill";
                            try
                            {
                                goatScheme.ParameterDefinition = new ElementId(BuiltInParameter.ROOM_NUMBER);
                            }
                            catch { }

                            targetView.SetColorFillSchemeId(roomCatId, goatScheme.Id);
                            doc.Regenerate();

                            var existingEntries = goatScheme.GetEntries().ToList();
                            foreach (var entry in existingEntries)
                            {
                                string entryVal = entry.GetStringValue() ?? "";
                                var matchRoom = roomsToUpdate.FirstOrDefault(r =>
                                    string.Equals(r.RoomNumber, entryVal, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(r.RoomName, entryVal, StringComparison.OrdinalIgnoreCase));

                                if (matchRoom != null)
                                {
                                    entry.Color = ParseHexToRevitColor(matchRoom.FillColorHex);
                                    if (solidFillId != ElementId.InvalidElementId)
                                        entry.FillPatternId = solidFillId;
                                    try { goatScheme.UpdateEntry(entry); } catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void ParameterDefinitionField(ColorFillScheme scheme)
        {
            try
            {
                // Keep existing parameter definition (usually Room Name or Department) so entries populate automatically
                _ = scheme.ParameterDefinition;
            }
            catch { }
        }

        internal static Autodesk.Revit.DB.Color ParseHexToRevitColor(string hex)
        {
            try
            {
                string clean = (hex ?? "#38BDF8").Trim().TrimStart('#');
                if (clean.Length == 6)
                {
                    byte r = Convert.ToByte(clean.Substring(0, 2), 16);
                    byte g = Convert.ToByte(clean.Substring(2, 2), 16);
                    byte b = Convert.ToByte(clean.Substring(4, 2), 16);
                    return new Autodesk.Revit.DB.Color(r, g, b);
                }
            }
            catch { }
            return new Autodesk.Revit.DB.Color(56, 189, 248);
        }

        private void ExecuteGenerateBundle(Document doc)
        {
            var selectedRooms = Window.RoomList.Where(r => r.IsSelected).ToList();
            if (selectedRooms.Count == 0)
            {
                Window.TxtStatus.Text = "Please select at least one room.";
                return;
            }

            var vfts = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList();
            ViewFamilyType planVft = vfts.FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);
            ViewFamilyType rcpVft = vfts.FirstOrDefault(v => v.ViewFamily == ViewFamily.CeilingPlan);
            ViewFamilyType elevVft = vfts.FirstOrDefault(v => v.ViewFamily == ViewFamily.Elevation);
            ViewFamilyType secVft = vfts.FirstOrDefault(v => v.ViewFamily == ViewFamily.Section);
            ViewFamilyType isoVft = vfts.FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);

            DimensionType dimType = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .FirstOrDefault(d => d.StyleType == DimensionStyleType.Linear);

            double.TryParse(Window.TxtCropOffset.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double cropMm);
            double cropFeet = UnitUtils.ConvertToInternalUnits(Math.Max(50, cropMm), UnitTypeId.Millimeters);
            int.TryParse(Window.TxtViewScale.Text, out int scale);
            if (scale < 1) scale = 50;

            string formula = string.IsNullOrWhiteSpace(Window.TxtNamingFormula.Text)
                ? "RM-{Number} - {Name} - {Type}"
                : Window.TxtNamingFormula.Text;

            bool genPlan = Window.ChkGenPlan.IsChecked == true;
            bool genRcp = Window.ChkGenRcp.IsChecked == true;
            bool genElev = Window.ChkGenElevations.IsChecked == true;
            bool elevParallelAll = Window.RbElevAllWalls.IsChecked == true;
            bool gen3D = Window.ChkGen3DIso.IsChecked == true;

            bool autoDim = Window.ChkBundleAutoDim.IsChecked == true;
            bool autoTag = Window.ChkBundleAutoTag.IsChecked == true;
            bool packSheet = Window.ChkBundlePackSheet.IsChecked == true;
            ElementId tbId = (Window.CbTitleBlocks.SelectedItem as ElementTypeOption)?.Id ?? ElementId.InvalidElementId;
            string sheetPrefix = Window.TxtSheetPrefix.Text?.Trim() ?? "ID-";

            var tagCategories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_PlumbingFixtures,
                BuiltInCategory.OST_Casework
            };

            var boundaryOpts = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            int totalViews = 0;
            int totalDims = 0;
            int totalTags = 0;
            int totalSheets = 0;

            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ROOM — Generate Room Documentation Bundle"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Create Room Views, Dims, Tags & Sheets"))
                {
                    trans.Start();

                    // First save any edited Room Names/Numbers and apply Fill Colors in Active View if checked
                    if (Window.ChkApplyRoomFillColors.IsChecked == true)
                    {
                        ApplyRoomNamesNumbersAndViewColorsInternal(doc, doc.ActiveView, selectedRooms);
                    }
                    else
                    {
                        ApplyRoomNamesNumbersAndViewColorsInternal(doc, null, selectedRooms);
                    }

                    foreach (var item in selectedRooms)
                    {
                        Room room = doc.GetElement(item.RoomId) as Room;
                        if (room == null) continue;
                        BoundingBoxXYZ rbb = room.get_BoundingBox(null);
                        if (rbb == null) continue;

                        BoundingBoxXYZ paddedBox = new BoundingBoxXYZ
                        {
                            Min = new XYZ(rbb.Min.X - cropFeet, rbb.Min.Y - cropFeet, rbb.Min.Z - cropFeet),
                            Max = new XYZ(rbb.Max.X + cropFeet, rbb.Max.Y + cropFeet, rbb.Max.Z + cropFeet)
                        };

                        var roomGeneratedViewIds = new List<ElementId>();

                        // 1. Enlarged Floor Plan
                        if (genPlan && planVft != null)
                        {
                            try
                            {
                                ViewPlan vp = ViewPlan.Create(doc, planVft.Id, room.LevelId);
                                if (vp != null)
                                {
                                    vp.Scale = scale;
                                    vp.CropBoxActive = true;
                                    vp.CropBoxVisible = false;
                                    vp.CropBox = paddedBox;
                                    SetUniqueViewName(doc, vp, FormatRoomViewName(formula, item.RoomNumber, item.RoomName, "Enlarged Plan"));

                                    if (Window.ChkApplyRoomFillColors.IsChecked == true)
                                        ApplyRoomNamesNumbersAndViewColorsInternal(doc, vp, new List<CeilingRoomItem> { item });

                                    if (autoDim)
                                        totalDims += GoatDocEngines.DimensionRoomInPlan(doc, vp, room, dimType, true, true, 400, 500, true);
                                    if (autoTag)
                                        totalTags += GoatDocEngines.TagElementsInView(doc, vp, tagCategories, false, true, true);

                                    roomGeneratedViewIds.Add(vp.Id);
                                    totalViews++;
                                }
                            }
                            catch { }
                        }

                        // 2. Reflected Ceiling Plan (RCP)
                        if (genRcp && rcpVft != null)
                        {
                            try
                            {
                                ViewPlan rcp = ViewPlan.Create(doc, rcpVft.Id, room.LevelId);
                                if (rcp != null)
                                {
                                    rcp.Scale = scale;
                                    rcp.CropBoxActive = true;
                                    rcp.CropBoxVisible = false;
                                    rcp.CropBox = paddedBox;
                                    SetUniqueViewName(doc, rcp, FormatRoomViewName(formula, item.RoomNumber, item.RoomName, "RCP"));
                                    roomGeneratedViewIds.Add(rcp.Id);
                                    totalViews++;
                                }
                            }
                            catch { }
                        }

                        // 3. Interior Elevations (Standard 4-Way N/S/E/W OR Parallel to All Room Walls)
                        if (genElev)
                        {
                            if (!elevParallelAll && elevVft != null)
                            {
                                try
                                {
                                    XYZ center = (rbb.Min + rbb.Max) * 0.5;
                                    ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, elevVft.Id, center, scale);
                                    string[] dirs = { "West", "North", "East", "South" };
                                    for (int i = 0; i < 4; i++)
                                    {
                                        try
                                        {
                                            ViewSection elevView = marker.CreateElevation(doc, doc.ActiveView.Id, i);
                                            if (elevView != null)
                                            {
                                                elevView.Scale = scale;
                                                SetUniqueViewName(doc, elevView, FormatRoomViewName(formula, item.RoomNumber, item.RoomName, $"Elev - {dirs[i]}"));
                                                if (autoDim)
                                                    totalDims += GoatDocEngines.DimensionElevationOrSection(doc, elevView, dimType);
                                                if (autoTag)
                                                    totalTags += GoatDocEngines.TagElementsInView(doc, elevView, tagCategories, false, true, false);
                                                roomGeneratedViewIds.Add(elevView.Id);
                                                totalViews++;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                catch { }
                            }
                            else if (secVft != null)
                            {
                                // Parallel to Every Room Wall Segment (supports angled & L-shaped rooms!)
                                IList<IList<BoundarySegment>> loops = room.GetBoundarySegments(boundaryOpts);
                                if (loops != null && loops.Count > 0)
                                {
                                    int wallIdx = 1;
                                    foreach (BoundarySegment seg in loops[0])
                                    {
                                        Curve c = seg.GetCurve();
                                        if (c == null || c.Length < UnitUtils.ConvertToInternalUnits(600.0, UnitTypeId.Millimeters))
                                            continue;

                                        try
                                        {
                                            XYZ p0 = c.GetEndPoint(0);
                                            XYZ p1 = c.GetEndPoint(1);
                                            XYZ wallDir = (p1 - p0).Normalize();
                                            XYZ viewDir = new XYZ(wallDir.Y, -wallDir.X, 0).Normalize();
                                            XYZ mid = (p0 + p1) * 0.5 - viewDir * UnitUtils.ConvertToInternalUnits(1000.0, UnitTypeId.Millimeters);

                                            Autodesk.Revit.DB.Transform t = Autodesk.Revit.DB.Transform.Identity;
                                            t.Origin = new XYZ(mid.X, mid.Y, rbb.Min.Z);
                                            t.BasisX = wallDir;
                                            t.BasisY = XYZ.BasisZ;
                                            t.BasisZ = wallDir.CrossProduct(XYZ.BasisZ).Normalize();

                                            double halfLen = c.Length * 0.5 + cropFeet;
                                            double height = Math.Max(UnitUtils.ConvertToInternalUnits(2800.0, UnitTypeId.Millimeters), rbb.Max.Z - rbb.Min.Z);
                                            BoundingBoxXYZ secBox = new BoundingBoxXYZ
                                            {
                                                Transform = t,
                                                Min = new XYZ(-halfLen, -cropFeet, 0),
                                                Max = new XYZ(halfLen, height + cropFeet, UnitUtils.ConvertToInternalUnits(1500.0, UnitTypeId.Millimeters))
                                            };

                                            ViewSection wallElev = ViewSection.CreateSection(doc, secVft.Id, secBox);
                                            if (wallElev != null)
                                            {
                                                wallElev.Scale = scale;
                                                SetUniqueViewName(doc, wallElev, FormatRoomViewName(formula, item.RoomNumber, item.RoomName, $"Wall Elev {wallIdx}"));
                                                roomGeneratedViewIds.Add(wallElev.Id);
                                                totalViews++;
                                                wallIdx++;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }

                        // 4. 3D Room Isometric (Section Box)
                        if (gen3D && isoVft != null)
                        {
                            try
                            {
                                View3D iso = View3D.CreateIsometric(doc, isoVft.Id);
                                if (iso != null)
                                {
                                    iso.Scale = scale;
                                    iso.SetSectionBox(paddedBox);
                                    SetUniqueViewName(doc, iso, FormatRoomViewName(formula, item.RoomNumber, item.RoomName, "3D Iso"));
                                    roomGeneratedViewIds.Add(iso.Id);
                                    totalViews++;
                                }
                            }
                            catch { }
                        }

                        // 5. Create Dedicated Sheet & Auto-Pack Views
                        if (packSheet && roomGeneratedViewIds.Count > 0)
                        {
                            try
                            {
                                ViewSheet sheet = ViewSheet.Create(doc, tbId);
                                if (sheet != null)
                                {
                                    sheet.Name = $"{item.RoomName} — Interior Documentation";
                                    SetUniqueSheetNumber(doc, sheet, $"{sheetPrefix}{item.RoomNumber}");
                                    GoatDocEngines.PackViewsOntoSheet(doc, sheet, roomGeneratedViewIds, false);
                                    totalSheets++;
                                }
                            }
                            catch { }
                        }
                    }

                    trans.Commit();
                }
                tg.Assimilate();
            }

            Window.TxtStatus.Text = $"✔ Generated {totalViews} Views · {totalDims} Dim Chains · {totalTags} Smart Tags · {totalSheets} Packed Sheets! (1x Ctrl+Z to Undo)";
        }

        private static string FormatRoomViewName(string formula, string num, string name, string type)
        {
            return formula
                .Replace("{Number}", string.IsNullOrWhiteSpace(num) ? "00" : num)
                .Replace("{Name}", string.IsNullOrWhiteSpace(name) ? "Room" : name)
                .Replace("{Type}", type);
        }

        private static void SetUniqueViewName(Document doc, View v, string baseName)
        {
            string clean = SanitizeRevitName(baseName);
            for (int i = 0; i < 50; i++)
            {
                string candidate = i == 0 ? clean : $"{clean} ({i + 1})";
                try
                {
                    v.Name = candidate;
                    return;
                }
                catch { }
            }
        }

        private static void SetUniqueSheetNumber(Document doc, ViewSheet s, string baseNum)
        {
            string clean = SanitizeRevitName(baseNum);
            for (int i = 0; i < 100; i++)
            {
                string candidate = i == 0 ? clean : $"{clean}.{i}";
                try
                {
                    s.SheetNumber = candidate;
                    return;
                }
                catch { }
            }
        }

        private static string SanitizeRevitName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "GOAT_View";
            char[] forbidden = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '`', '~' };
            foreach (char c in forbidden) s = s.Replace(c, '-');
            return s.Trim();
        }
    }

    public partial class GoatRoomWindow : Window
    {
        private readonly GoatRoomEventHandler _handler;
        private readonly ExternalEvent _extEvent;
        private bool _isInitializing = true;

        public ObservableCollection<CeilingRoomItem> RoomList { get; } = new ObservableCollection<CeilingRoomItem>();

        public GoatRoomWindow(GoatRoomEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            DgRooms.ItemsSource = RoomList;
            _isInitializing = false;

            Loaded += (s, e) =>
            {
                TriggerScan();
                RenderSheetPreview();
            };
        }

        public void LoadScannedData(List<ElementTypeOption> titleBlocks, List<CeilingRoomItem> rooms)
        {
            CbTitleBlocks.ItemsSource = titleBlocks;
            if (titleBlocks.Count > 0) CbTitleBlocks.SelectedIndex = 0;

            RoomList.Clear();
            foreach (var r in rooms) RoomList.Add(r);

            TxtSelectedCount.Text = $"{RoomList.Count(r => r.IsSelected)} Selected";
            RenderSheetPreview();
        }

        private void TriggerScan()
        {
            _handler.IsScanOnly = true;
            _extEvent.Raise();
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
            TxtSelectedCount.Text = $"{RoomList.Count} Selected";
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var r in RoomList) r.IsSelected = false;
            DgRooms.Items.Refresh();
            TxtSelectedCount.Text = "0 Selected";
        }

        private void BtnPickRoomColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is CeilingRoomItem item)
            {
                using (var dlg = new System.Windows.Forms.ColorDialog())
                {
                    dlg.FullOpen = true;
                    try
                    {
                        var cur = GoatRoomEventHandler.ParseHexToRevitColor(item.FillColorHex);
                        dlg.Color = System.Drawing.Color.FromArgb(cur.Red, cur.Green, cur.Blue);
                    }
                    catch { }

                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        item.FillColorHex = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
                        DgRooms.Items.Refresh();
                    }
                }
            }
        }

        private void BtnAutoColorRooms_Click(object sender, RoutedEventArgs e)
        {
            string[] palette =
            {
                "#2E5A44", "#4B3F5E", "#1D4E56", "#5C4033", "#3B4D6B",
                "#4A5D23", "#5E3A4D", "#10B981", "#0284C7", "#D97706"
            };
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int idx = 0;
            foreach (var r in RoomList)
            {
                string key = string.IsNullOrWhiteSpace(r.RoomName) ? r.RoomNumber : r.RoomName.Trim();
                if (!map.TryGetValue(key, out string hex))
                {
                    hex = palette[idx % palette.Length];
                    map[key] = hex;
                    idx++;
                }
                r.FillColorHex = hex;
            }
            DgRooms.Items.Refresh();
            TxtStatus.Text = "Assigned matching architectural fill colors by Room Name. Click 'Apply Names, Numbers & Fill Colors' to update view!";
        }

        private void BtnApplyRoomEditsAndColors_Click(object sender, RoutedEventArgs e)
        {
            _handler.IsScanOnly = false;
            _handler.IsApplyEditsAndColorsOnly = true;
            _extEvent.Raise();
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            RenderSheetPreview();
        }

        private void CanvasSheetPreview_SizeChanged(object sender, SizeChangedEventArgs e) => RenderSheetPreview();

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            _handler.IsScanOnly = false;
            _extEvent.Raise();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void RenderSheetPreview()
        {
            if (CanvasSheetPreview == null) return;
            CanvasSheetPreview.Children.Clear();

            double w = CanvasSheetPreview.ActualWidth;
            double h = CanvasSheetPreview.ActualHeight;
            if (w < 60 || h < 60) return;

            double pad = 14;
            double sheetW = w - pad * 2;
            double sheetH = h - pad * 2;

            // Titleblock Outer Border
            var sheetRect = new System.Windows.Shapes.Rectangle
            {
                Width = sheetW,
                Height = sheetH,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                StrokeThickness = 2.5
            };
            Canvas.SetLeft(sheetRect, pad);
            Canvas.SetTop(sheetRect, pad);
            CanvasSheetPreview.Children.Add(sheetRect);

            // Right Titleblock Strip
            double stripW = sheetW * 0.16;
            var stripRect = new System.Windows.Shapes.Rectangle
            {
                Width = stripW,
                Height = sheetH,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(stripRect, pad + sheetW - stripW);
            Canvas.SetTop(stripRect, pad);
            CanvasSheetPreview.Children.Add(stripRect);

            // Collect enabled views
            var labels = new List<(string Title, System.Windows.Media.Color Fill)>();
            if (ChkGenPlan?.IsChecked == true) labels.Add(("Enlarged Plan\n+ Dims & Tags", System.Windows.Media.Color.FromRgb(224, 242, 254)));
            if (ChkGenRcp?.IsChecked == true) labels.Add(("Ceiling Plan\n(RCP)", System.Windows.Media.Color.FromRgb(237, 233, 254)));
            if (ChkGen3DIso?.IsChecked == true) labels.Add(("3D Room\nSection Box", System.Windows.Media.Color.FromRgb(209, 250, 229)));
            if (ChkGenElevations?.IsChecked == true)
            {
                labels.Add(("Elev - North", System.Windows.Media.Color.FromRgb(254, 243, 199)));
                labels.Add(("Elev - South", System.Windows.Media.Color.FromRgb(254, 243, 199)));
                labels.Add(("Elev - East", System.Windows.Media.Color.FromRgb(254, 243, 199)));
                labels.Add(("Elev - West", System.Windows.Media.Color.FromRgb(254, 243, 199)));
            }

            if (labels.Count == 0) return;

            double drawW = sheetW - stripW - 16;
            double drawH = sheetH - 16;
            int cols = labels.Count <= 3 ? labels.Count : 3;
            int rows = (int)Math.Ceiling((double)labels.Count / cols);
            double cellW = drawW / cols;
            double cellH = drawH / rows;

            for (int i = 0; i < labels.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                double bx = pad + 8 + c * cellW + 4;
                double by = pad + 8 + r * cellH + 4;
                double bw = Math.Max(16, cellW - 8);
                double bh = Math.Max(16, cellH - 8);

                var vpRect = new System.Windows.Shapes.Rectangle
                {
                    Width = bw,
                    Height = bh,
                    Fill = new SolidColorBrush(labels[i].Fill),
                    Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 189, 248)),
                    StrokeThickness = 1.5,
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(vpRect, bx);
                Canvas.SetTop(vpRect, by);
                CanvasSheetPreview.Children.Add(vpRect);

                TextBlock tb = new TextBlock
                {
                    Text = labels[i].Title,
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                    TextAlignment = TextAlignment.Center
                };
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(tb, bx + (bw - tb.DesiredSize.Width) * 0.5);
                Canvas.SetTop(tb, by + (bh - tb.DesiredSize.Height) * 0.5);
                CanvasSheetPreview.Children.Add(tb);
            }
        }
    }
}
