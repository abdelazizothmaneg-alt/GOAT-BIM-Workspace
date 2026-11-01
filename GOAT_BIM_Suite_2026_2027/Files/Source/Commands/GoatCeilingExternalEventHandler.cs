using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using ORTools.Models;
using ORTools.UI;

namespace ORTools.Commands
{
    public enum GoatCeilingAction
    {
        None,
        ScanRoomsAndFamilies,
        GenerateCeilingAndArray
    }

    public class CeilingRoomItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _roomNumber = string.Empty;
        private string _roomName = string.Empty;
        private string _fillColorHex = "#38BDF8";

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public ElementId RoomId { get; set; } = ElementId.InvalidElementId;
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;

        public string RoomNumber
        {
            get => _roomNumber;
            set { _roomNumber = value ?? string.Empty; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string RoomName
        {
            get => _roomName;
            set { _roomName = value ?? string.Empty; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string FillColorHex
        {
            get => _fillColorHex;
            set { _fillColorHex = string.IsNullOrWhiteSpace(value) ? "#38BDF8" : value; OnPropertyChanged(); }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(RoomNumber) ? RoomName : $"{RoomNumber} - {RoomName}";
        public double AreaSqm { get; set; }
        public bool HasExistingCeiling { get; set; }
        public ElementId ExistingCeilingId { get; set; } = ElementId.InvalidElementId;
        public double ExistingCeilingHeightMm { get; set; } = 2700.0;
        public string CeilingStatus => HasExistingCeiling ? $"● Replace (+{ExistingCeilingHeightMm:0})" : "⊕ Auto-Create";
        public string StatusColor => HasExistingCeiling ? "#F59E0B" : "#0284C7";
        public double MinXFeet { get; set; }
        public double MaxXFeet { get; set; }
        public double MinYFeet { get; set; }
        public double MaxYFeet { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class GoatCeilingSettings
    {
        public bool ActiveViewOnly { get; set; } = true;
        public bool AutoCreateMissingCeiling { get; set; } = true;
        public string SelectedCeilingTypeName { get; set; } = string.Empty;
        public double DefaultCeilingHeightMm { get; set; } = 2700.0;

        public double TileWidthMm { get; set; } = 600.0;
        public double TileLengthMm { get; set; } = 600.0;
        public bool CenterGridInRoom { get; set; } = true;
        public double WallClearanceMarginMm { get; set; } = 450.0;

        public bool PlaceLighting { get; set; } = true;
        public string SelectedLightingSymbolName { get; set; } = string.Empty;
        public int LightingStepX { get; set; } = 2; // Every 2 tiles (1200mm)
        public int LightingStepY { get; set; } = 2; // Every 2 tiles (1200mm)

        public bool PlaceDiffusers { get; set; } = false;
        public string SelectedDiffuserSymbolName { get; set; } = string.Empty;
        public int DiffuserStepX { get; set; } = 4;
        public int DiffuserStepY { get; set; } = 4;

        public bool PlaceSprinklers { get; set; } = false;
        public string SelectedSprinklerSymbolName { get; set; } = string.Empty;
        public int SprinklerStepX { get; set; } = 5;
        public int SprinklerStepY { get; set; } = 5;

        public bool AvoidColumns { get; set; } = true;
        public bool SmartReplaceExisting { get; set; } = true;
        public bool WriteRoomTagToComments { get; set; } = true;
    }

    public class GoatCeilingExternalEventHandler : IExternalEventHandler
    {
        public GoatCeilingWindow Window { get; set; }
        public GoatCeilingAction CurrentAction { get; set; } = GoatCeilingAction.None;

        public void Execute(UIApplication uiapp)
        {
            if (Window == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null) return;
            Document doc = uidoc.Document;

            try
            {
                switch (CurrentAction)
                {
                    case GoatCeilingAction.ScanRoomsAndFamilies:
                        ExecuteScan(doc);
                        break;

                    case GoatCeilingAction.GenerateCeilingAndArray:
                        ExecuteGenerate(doc);
                        break;
                }
            }
            catch (Exception ex)
            {
                Window.UpdateStatus($"Error: {ex.Message}", true);
            }
            finally
            {
                CurrentAction = GoatCeilingAction.None;
            }
        }

        public string GetName() => "GOAT CEILING External Event Handler";

        private void ExecuteScan(Document doc)
        {
            // 1. Ceiling Types
            var ceilingTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(CeilingType))
                .Cast<CeilingType>()
                .OrderBy(c => c.Name)
                .Select(c => new ElementTypeOption { Id = c.Id, Name = c.Name })
                .ToList();

            // 2. Lighting Fixture Symbols
            var lightingSymbols = CollectFamilySymbols(doc, BuiltInCategory.OST_LightingFixtures);

            // 3. Air Terminal (Diffuser) Symbols
            var diffuserSymbols = CollectFamilySymbols(doc, BuiltInCategory.OST_DuctTerminal);

            // 4. Sprinkler / Fire Alarm Symbols
            var sprinklerSymbols = CollectFamilySymbols(doc, BuiltInCategory.OST_Sprinklers);
            if (sprinklerSymbols.Count == 0)
            {
                sprinklerSymbols = CollectFamilySymbols(doc, BuiltInCategory.OST_FireAlarmDevices);
            }

            // 5. Existing Ceilings in Model for spatial room matching
            var existingCeilings = new FilteredElementCollector(doc)
                .OfClass(typeof(Ceiling))
                .Cast<Ceiling>()
                .ToList();

            // 6. Rooms
            bool activeOnly = Window.IsActiveViewScope;
            FilteredElementCollector roomCollector = (activeOnly && doc.ActiveView != null)
                ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                : new FilteredElementCollector(doc);

            var rooms = roomCollector
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<Room>()
                .Where(r => r.Area > 0.001 && r.Location != null)
                .OrderBy(r => r.Number)
                .ToList();

            var roomItems = new List<CeilingRoomItem>();
            foreach (Room r in rooms)
            {
                BoundingBoxXYZ bb = r.get_BoundingBox(null);
                double minX = bb != null ? bb.Min.X : -10;
                double maxX = bb != null ? bb.Max.X : 10;
                double minY = bb != null ? bb.Min.Y : -8;
                double maxY = bb != null ? bb.Max.Y : 8;

                // Check if any ceiling's bbox center falls inside this room
                Ceiling matchedCeiling = null;
                double ceilHmm = 2700.0;
                foreach (Ceiling c in existingCeilings)
                {
                    if (c.LevelId != r.LevelId) continue;
                    BoundingBoxXYZ cbb = c.get_BoundingBox(null);
                    if (cbb == null) continue;
                    XYZ mid = (cbb.Min + cbb.Max) * 0.5;
                    XYZ testPt = new XYZ(mid.X, mid.Y, (bb != null ? bb.Min.Z : 0) + 1.0);
                    if (r.IsPointInRoom(testPt))
                    {
                        matchedCeiling = c;
                        Parameter hp = c.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                        if (hp != null)
                            ceilHmm = Math.Round(UnitUtils.ConvertFromInternalUnits(hp.AsDouble(), UnitTypeId.Millimeters), 0);
                        break;
                    }
                }

                roomItems.Add(new CeilingRoomItem
                {
                    IsSelected = true,
                    RoomId = r.Id,
                    LevelId = r.LevelId,
                    RoomNumber = r.Number ?? "",
                    RoomName = r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? r.Name ?? "Room",
                    AreaSqm = Math.Round(UnitUtils.ConvertFromInternalUnits(r.Area, UnitTypeId.SquareMeters), 2),
                    HasExistingCeiling = matchedCeiling != null,
                    ExistingCeilingId = matchedCeiling != null ? matchedCeiling.Id : ElementId.InvalidElementId,
                    ExistingCeilingHeightMm = ceilHmm,
                    MinXFeet = minX,
                    MaxXFeet = maxX,
                    MinYFeet = minY,
                    MaxYFeet = maxY
                });
            }

            Window.ApplyScannedData(ceilingTypes, lightingSymbols, diffuserSymbols, sprinklerSymbols, roomItems);
        }

        private static List<ElementTypeOption> CollectFamilySymbols(Document doc, BuiltInCategory bic)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(bic)
                .WhereElementIsElementType()
                .OfType<FamilySymbol>()
                .OrderBy(s => s.FamilyName)
                .ThenBy(s => s.Name)
                .Select(s => new ElementTypeOption
                {
                    Id = s.Id,
                    Name = $"{s.FamilyName} : {s.Name}"
                })
                .ToList();
        }

        private void ExecuteGenerate(Document doc)
        {
            GoatCeilingSettings cfg = Window.CollectSettings();
            GoatSettingsManager.Save("GoatCeiling", cfg);

            var selectedRooms = Window.GetSelectedRooms();
            if (selectedRooms.Count == 0)
            {
                Window.UpdateStatus("Please select at least one room.", true);
                return;
            }

            CeilingType defaultCeilingType = null;
            var allCeilTypes = new FilteredElementCollector(doc).OfClass(typeof(CeilingType)).Cast<CeilingType>().ToList();
            if (allCeilTypes.Count > 0)
            {
                defaultCeilingType = allCeilTypes.FirstOrDefault(c => string.Equals(c.Name, cfg.SelectedCeilingTypeName, StringComparison.OrdinalIgnoreCase))
                                  ?? allCeilTypes[0];
            }

            FamilySymbol lightSym = FindSymbol(doc, BuiltInCategory.OST_LightingFixtures, cfg.SelectedLightingSymbolName);
            FamilySymbol diffSym = FindSymbol(doc, BuiltInCategory.OST_DuctTerminal, cfg.SelectedDiffuserSymbolName);
            FamilySymbol spkSym = FindSymbol(doc, BuiltInCategory.OST_Sprinklers, cfg.SelectedSprinklerSymbolName)
                               ?? FindSymbol(doc, BuiltInCategory.OST_FireAlarmDevices, cfg.SelectedSprinklerSymbolName);

            // Collect structural/architectural columns for clash avoidance
            var columnBoxes = new List<BoundingBoxXYZ>();
            if (cfg.AvoidColumns)
            {
                var cols = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WherePasses(new LogicalOrFilter(
                        new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
                        new ElementCategoryFilter(BuiltInCategory.OST_Columns)));
                foreach (Element col in cols)
                {
                    BoundingBoxXYZ cbb = col.get_BoundingBox(null);
                    if (cbb != null) columnBoxes.Add(cbb);
                }
            }

            var boundaryOpts = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            int createdCeilings = 0;
            int placedLights = 0;
            int placedDiffusers = 0;
            int placedSprinklers = 0;

            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT CEILING — Generate Ceilings & Fixture Array"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Create Ceilings & Place MEP/Lighting Grid"))
                {
                    trans.Start();

                    if (lightSym != null && !lightSym.IsActive) lightSym.Activate();
                    if (diffSym != null && !diffSym.IsActive) diffSym.Activate();
                    if (spkSym != null && !spkSym.IsActive) spkSym.Activate();

                    // Smart Replace previously placed GOAT_CEILING elements and existing ceilings in selected rooms
                    var allExistingCeilingsInDoc = new FilteredElementCollector(doc)
                        .OfClass(typeof(Ceiling))
                        .Cast<Ceiling>()
                        .ToList();

                    if (cfg.SmartReplaceExisting)
                    {
                        var roomPrefixes = selectedRooms
                            .SelectMany(r => new[] { $"GOAT_CEILING_ROOM_{r.RoomId.Value}", $"GOAT_FINISH_ROOM_{r.RoomId.Value}" })
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var selectedRoomElements = selectedRooms
                            .Select(r => doc.GetElement(r.RoomId) as Room)
                            .Where(r => r != null)
                            .ToList();

                        var oldIds = new HashSet<ElementId>();
                        var instances = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .WherePasses(new LogicalOrFilter(new List<ElementFilter>
                            {
                                new ElementCategoryFilter(BuiltInCategory.OST_LightingFixtures),
                                new ElementCategoryFilter(BuiltInCategory.OST_DuctTerminal),
                                new ElementCategoryFilter(BuiltInCategory.OST_Sprinklers),
                                new ElementCategoryFilter(BuiltInCategory.OST_FireAlarmDevices)
                            }));

                        foreach (Element el in instances)
                        {
                            string cmt = el.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
                            if (!string.IsNullOrEmpty(cmt))
                            {
                                string prefix = cmt.Split('|')[0].Trim();
                                if (roomPrefixes.Contains(prefix))
                                {
                                    oldIds.Add(el.Id);
                                    continue;
                                }
                            }

                            if (el.Location is LocationPoint lp)
                            {
                                foreach (Room rm in selectedRoomElements)
                                {
                                    BoundingBoxXYZ rbb = rm.get_BoundingBox(null);
                                    double testZ = rbb != null ? rbb.Min.Z + 1.0 : lp.Point.Z;
                                    try
                                    {
                                        if (rm.IsPointInRoom(new XYZ(lp.Point.X, lp.Point.Y, testZ)))
                                        {
                                            oldIds.Add(el.Id);
                                            break;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }

                        foreach (ElementId id in oldIds)
                        {
                            try { doc.Delete(id); } catch { }
                        }
                    }

                    double tileXFeet = UnitUtils.ConvertToInternalUnits(Math.Max(200, cfg.TileWidthMm), UnitTypeId.Millimeters);
                    double tileYFeet = UnitUtils.ConvertToInternalUnits(Math.Max(200, cfg.TileLengthMm), UnitTypeId.Millimeters);
                    double marginFeet = UnitUtils.ConvertToInternalUnits(Math.Max(50, cfg.WallClearanceMarginMm), UnitTypeId.Millimeters);
                    double defaultCeilHFeet = UnitUtils.ConvertToInternalUnits(cfg.DefaultCeilingHeightMm, UnitTypeId.Millimeters);

                    foreach (CeilingRoomItem item in selectedRooms)
                    {
                        Room room = doc.GetElement(item.RoomId) as Room;
                        if (room == null) continue;
                        Level level = doc.GetElement(room.LevelId) as Level;
                        if (level == null) continue;

                        BoundingBoxXYZ roomBoxCheck = room.get_BoundingBox(null);
                        double checkZ = roomBoxCheck != null ? roomBoxCheck.Min.Z + 1.0 : level.Elevation + 1.0;

                        // Find any existing Ceiling inside this room
                        var existingCeilingsForRoom = new List<Ceiling>();
                        foreach (Ceiling c in allExistingCeilingsInDoc)
                        {
                            if (c == null || !c.IsValidObject) continue;
                            string cmt = c.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
                            if (cmt.StartsWith($"GOAT_CEILING_ROOM_{room.Id.Value}", StringComparison.OrdinalIgnoreCase) ||
                                cmt.StartsWith($"GOAT_FINISH_ROOM_{room.Id.Value}", StringComparison.OrdinalIgnoreCase) ||
                                c.Id == item.ExistingCeilingId)
                            {
                                existingCeilingsForRoom.Add(c);
                                continue;
                            }

                            if (c.LevelId == room.LevelId)
                            {
                                BoundingBoxXYZ cbb = c.get_BoundingBox(null);
                                if (cbb != null)
                                {
                                    XYZ mid = (cbb.Min + cbb.Max) * 0.5;
                                    try
                                    {
                                        if (room.IsPointInRoom(new XYZ(mid.X, mid.Y, checkZ)))
                                            existingCeilingsForRoom.Add(c);
                                    }
                                    catch { }
                                }
                            }
                        }

                        Ceiling roomCeiling = existingCeilingsForRoom.FirstOrDefault();
                        double activeCeilOffsetFeet = defaultCeilHFeet;

                        // 1. Replace old ceiling(s) and create new ceiling if AutoCreateMissingCeiling or SmartReplaceExisting is enabled
                        if ((cfg.AutoCreateMissingCeiling || cfg.SmartReplaceExisting) && defaultCeilingType != null)
                        {
                            foreach (Ceiling oldCeil in existingCeilingsForRoom)
                            {
                                try { doc.Delete(oldCeil.Id); } catch { }
                            }
                            roomCeiling = null;

                            try
                            {
                                IList<IList<BoundarySegment>> loops = room.GetBoundarySegments(boundaryOpts);
                                if (loops != null && loops.Count > 0 && loops[0].Count >= 3)
                                {
                                    CurveLoop cl = BuildClosedLoop(loops[0], level.Elevation);
                                    if (cl != null)
                                    {
                                        roomCeiling = Ceiling.Create(doc, new List<CurveLoop> { cl }, defaultCeilingType.Id, room.LevelId);
                                        if (roomCeiling != null)
                                        {
                                            Parameter hp = roomCeiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                                            if (hp != null && !hp.IsReadOnly)
                                                hp.Set(defaultCeilHFeet);
                                            SetComment(roomCeiling, $"GOAT_CEILING_ROOM_{room.Id.Value} | Ceiling | {room.Number}");
                                            createdCeilings++;
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        else if (roomCeiling != null)
                        {
                            Parameter hp = roomCeiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                            if (hp != null)
                                activeCeilOffsetFeet = hp.AsDouble();
                        }

                        if (roomCeiling != null)
                        {
                            doc.Regenerate();
                        }

                        // Find bottom horizontal planar face of the ceiling if available for Face/Ceiling-hosted families
                        Reference ceilingBottomFaceRef = GetBottomPlanarFaceReference(roomCeiling);

                        // 2. Compute Spatial Grid Array nodes inside room bbox with wall clearance margin
                        BoundingBoxXYZ rbb = room.get_BoundingBox(null);
                        if (rbb == null) continue;

                        double minX = rbb.Min.X + marginFeet;
                        double maxX = rbb.Max.X - marginFeet;
                        double minY = rbb.Min.Y + marginFeet;
                        double maxY = rbb.Max.Y - marginFeet;
                        if (maxX <= minX || maxY <= minY)
                        {
                            // Small room fallback
                            minX = rbb.Min.X + 0.5;
                            maxX = rbb.Max.X - 0.5;
                            minY = rbb.Min.Y + 0.5;
                            maxY = rbb.Max.Y - 0.5;
                        }

                        double spanX = Math.Max(0.1, maxX - minX);
                        double spanY = Math.Max(0.1, maxY - minY);
                        int cols = Math.Max(1, (int)Math.Floor(spanX / tileXFeet) + 1);
                        int rows = Math.Max(1, (int)Math.Floor(spanY / tileYFeet) + 1);

                        double startX = cfg.CenterGridInRoom
                            ? ((minX + maxX) * 0.5 - ((cols - 1) * tileXFeet) * 0.5)
                            : (minX + tileXFeet * 0.5);
                        double startY = cfg.CenterGridInRoom
                            ? ((minY + maxY) * 0.5 - ((rows - 1) * tileYFeet) * 0.5)
                            : (minY + tileYFeet * 0.5);

                        double worldCeilingZ = level.Elevation + activeCeilOffsetFeet;
                        double roomTestZ = rbb.Min.Z + 1.0;
                        string commentTag = $"GOAT_CEILING_ROOM_{room.Id.Value} | Room {room.Number} - {item.RoomName}";

                        int lStepX = Math.Max(1, cfg.LightingStepX);
                        int lStepY = Math.Max(1, cfg.LightingStepY);
                        int dStepX = Math.Max(1, cfg.DiffuserStepX);
                        int dStepY = Math.Max(1, cfg.DiffuserStepY);
                        int sStepX = Math.Max(1, cfg.SprinklerStepX);
                        int sStepY = Math.Max(1, cfg.SprinklerStepY);

                        for (int ix = 0; ix < cols; ix++)
                        {
                            for (int iy = 0; iy < rows; iy++)
                            {
                                double x = startX + ix * tileXFeet;
                                double y = startY + iy * tileYFeet;

                                // Verify point is genuinely inside the room polygon
                                if (!room.IsPointInRoom(new XYZ(x, y, roomTestZ)))
                                    continue;

                                // Verify point does not clash with any structural column
                                if (cfg.AvoidColumns && ClashesWithColumn(x, y, roomTestZ, columnBoxes))
                                    continue;

                                XYZ placePt = new XYZ(x, y, worldCeilingZ);

                                // A. Lighting Fixture node
                                if (cfg.PlaceLighting && lightSym != null && (ix % lStepX == 0) && (iy % lStepY == 0))
                                {
                                    FamilyInstance fi = PlaceSmartCeilingFixture(doc, lightSym, placePt, ceilingBottomFaceRef, roomCeiling, level, activeCeilOffsetFeet);
                                    if (fi != null)
                                    {
                                        if (cfg.WriteRoomTagToComments) SetComment(fi, commentTag);
                                        placedLights++;
                                    }
                                }
                                // B. Air Terminal (Diffuser) node (offset by +1 tile index so it never collides with lighting!)
                                else if (cfg.PlaceDiffusers && diffSym != null && ((ix + 1) % dStepX == 0) && ((iy + 1) % dStepY == 0))
                                {
                                    FamilyInstance fi = PlaceSmartCeilingFixture(doc, diffSym, placePt, ceilingBottomFaceRef, roomCeiling, level, activeCeilOffsetFeet);
                                    if (fi != null)
                                    {
                                        if (cfg.WriteRoomTagToComments) SetComment(fi, commentTag);
                                        placedDiffusers++;
                                    }
                                }
                                // C. Sprinkler / Detector node
                                else if (cfg.PlaceSprinklers && spkSym != null && ((ix + 2) % sStepX == 0) && ((iy + 2) % sStepY == 0))
                                {
                                    FamilyInstance fi = PlaceSmartCeilingFixture(doc, spkSym, placePt, ceilingBottomFaceRef, roomCeiling, level, activeCeilOffsetFeet);
                                    if (fi != null)
                                    {
                                        if (cfg.WriteRoomTagToComments) SetComment(fi, commentTag);
                                        placedSprinklers++;
                                    }
                                }
                            }
                        }
                    }

                    trans.Commit();
                }
                tg.Assimilate();
            }

            Window.UpdateStatus(
                $"✔ Created {createdCeilings} Ceilings · Placed {placedLights} Lights · {placedDiffusers} Diffusers · {placedSprinklers} Sprinklers. (1x Ctrl+Z to Undo)",
                false);
        }

        private static FamilyInstance PlaceSmartCeilingFixture(
            Document doc,
            FamilySymbol symbol,
            XYZ worldPt,
            Reference ceilingFaceRef,
            Ceiling hostCeiling,
            Level level,
            double ceilingOffsetFeet)
        {
            // Strategy 1: Face-based placement on ceiling bottom face
            if (ceilingFaceRef != null)
            {
                try
                {
                    FamilyInstance fi = doc.Create.NewFamilyInstance(ceilingFaceRef, worldPt, XYZ.BasisX, symbol);
                    if (fi != null) return fi;
                }
                catch { }
            }

            // Strategy 2: Element-hosted on Ceiling
            if (hostCeiling != null)
            {
                try
                {
                    FamilyInstance fi = doc.Create.NewFamilyInstance(worldPt, symbol, hostCeiling, level, StructuralType.NonStructural);
                    if (fi != null) return fi;
                }
                catch { }
            }

            // Strategy 3: Level-based / Non-hosted with elevation offset set to Ceiling Height
            try
            {
                FamilyInstance fi = doc.Create.NewFamilyInstance(
                    new XYZ(worldPt.X, worldPt.Y, level.Elevation),
                    symbol,
                    level,
                    StructuralType.NonStructural);

                if (fi != null)
                {
                    Parameter pFree = fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                    if (pFree != null && !pFree.IsReadOnly)
                        pFree.Set(ceilingOffsetFeet);

                    Parameter pElev = fi.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                    if (pElev != null && !pElev.IsReadOnly)
                        pElev.Set(ceilingOffsetFeet);

                    return fi;
                }
            }
            catch { }

            return null;
        }

        private static Reference GetBottomPlanarFaceReference(Ceiling ceiling)
        {
            if (ceiling == null) return null;
            try
            {
                Options opt = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Medium };
                GeometryElement geo = ceiling.get_Geometry(opt);
                if (geo == null) return null;

                foreach (GeometryObject obj in geo)
                {
                    if (obj is Solid solid && solid.Faces.Size > 0)
                    {
                        foreach (Face f in solid.Faces)
                        {
                            if (f is PlanarFace pf && pf.FaceNormal.Z < -0.9 && pf.Reference != null)
                            {
                                return pf.Reference;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool ClashesWithColumn(double x, double y, double z, List<BoundingBoxXYZ> colBoxes)
        {
            double pad = 0.5; // ~150mm clearance around columns
            foreach (var bb in colBoxes)
            {
                if (z >= bb.Min.Z - 2.0 && z <= bb.Max.Z + 2.0)
                {
                    if (x >= bb.Min.X - pad && x <= bb.Max.X + pad &&
                        y >= bb.Min.Y - pad && y <= bb.Max.Y + pad)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static CurveLoop BuildClosedLoop(IList<BoundarySegment> segments, double z)
        {
            var pts = new List<XYZ>();
            foreach (var seg in segments)
            {
                Curve c = seg.GetCurve();
                if (c == null) continue;
                XYZ p = new XYZ(c.GetEndPoint(0).X, c.GetEndPoint(0).Y, z);
                if (pts.Count == 0 || pts[pts.Count - 1].DistanceTo(p) > 0.01)
                    pts.Add(p);
            }
            if (pts.Count > 3 && pts[0].DistanceTo(pts[pts.Count - 1]) <= 0.01)
                pts.RemoveAt(pts.Count - 1);
            if (pts.Count < 3) return null;

            CurveLoop loop = new CurveLoop();
            for (int i = 0; i < pts.Count; i++)
            {
                XYZ a = pts[i];
                XYZ b = pts[(i + 1) % pts.Count];
                if (a.DistanceTo(b) > 0.005)
                    loop.Append(Line.CreateBound(a, b));
            }
            return loop;
        }

        private static FamilySymbol FindSymbol(Document doc, BuiltInCategory bic, string displayName)
        {
            var symbols = new FilteredElementCollector(doc)
                .OfCategory(bic)
                .WhereElementIsElementType()
                .OfType<FamilySymbol>()
                .ToList();
            if (symbols.Count == 0) return null;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var exact = symbols.FirstOrDefault(s => string.Equals($"{s.FamilyName} : {s.Name}", displayName, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;
            }
            return symbols[0];
        }

        private static void SetComment(Element el, string comment)
        {
            Parameter p = el?.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (p != null && !p.IsReadOnly) p.Set(comment);
        }
    }
}
