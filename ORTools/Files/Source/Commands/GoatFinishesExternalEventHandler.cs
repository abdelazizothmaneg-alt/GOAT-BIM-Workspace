using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ORTools.Models;
using ORTools.UI;

namespace ORTools.Commands
{
    public enum GoatFinishesAction
    {
        None,
        ScanRoomsAndTypes,
        ZoomToRoom,
        GenerateFinishes
    }

    public class GoatFinishesExternalEventHandler : IExternalEventHandler
    {
        public GoatFinishesWindow Window { get; set; }
        public GoatFinishesAction CurrentAction { get; set; } = GoatFinishesAction.None;
        public ElementId TargetZoomRoomId { get; set; } = ElementId.InvalidElementId;

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
                    case GoatFinishesAction.ScanRoomsAndTypes:
                        ExecuteScan(uidoc, doc);
                        break;

                    case GoatFinishesAction.ZoomToRoom:
                        ExecuteZoomToRoom(uidoc);
                        break;

                    case GoatFinishesAction.GenerateFinishes:
                        ExecuteGenerateFinishes(doc);
                        break;
                }
            }
            catch (Exception ex)
            {
                Window.UpdateStatus($"Error: {ex.Message}", isError: true);
            }
            finally
            {
                CurrentAction = GoatFinishesAction.None;
            }
        }

        public string GetName() => "GOAT FINISHES External Event Handler";

        private void ExecuteZoomToRoom(UIDocument uidoc)
        {
            if (TargetZoomRoomId == null || TargetZoomRoomId == ElementId.InvalidElementId)
                return;

            try
            {
                uidoc.Selection.SetElementIds(new List<ElementId> { TargetZoomRoomId });
                uidoc.ShowElements(TargetZoomRoomId);
            }
            catch
            {
            }
        }

        private void ExecuteScan(UIDocument uidoc, Document doc)
        {
            // 1. Collect Wall Types (Basic Walls)
            var wallTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(wt => wt.Kind == WallKind.Basic)
                .OrderBy(wt => wt.Name)
                .Select(wt => new ElementTypeOption
                {
                    Id = wt.Id,
                    Name = $"Basic Wall : {wt.Name}",
                    ThicknessMm = Math.Round(UnitUtils.ConvertFromInternalUnits(wt.Width, UnitTypeId.Millimeters), 1)
                })
                .ToList();

            // 2. Collect Wall Sweep Types (Cornices)
            var sweepTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Cornices)
                .WhereElementIsElementType()
                .Cast<ElementType>()
                .OrderBy(st => st.Name)
                .Select(st => new ElementTypeOption
                {
                    Id = st.Id,
                    Name = $"Wall Sweep : {st.Name}",
                    ThicknessMm = 15.0
                })
                .ToList();

            // 3. Collect Floor Types
            var floorTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .Where(ft => ft.IsFoundationSlab == false)
                .OrderBy(ft => ft.Name)
                .Select(ft =>
                {
                    double thkMm = 20.0;
                    try
                    {
                        CompoundStructure cs = ft.GetCompoundStructure();
                        if (cs != null)
                            thkMm = Math.Round(UnitUtils.ConvertFromInternalUnits(cs.GetWidth(), UnitTypeId.Millimeters), 1);
                    }
                    catch { }
                    return new ElementTypeOption
                    {
                        Id = ft.Id,
                        Name = $"Floor : {ft.Name}",
                        ThicknessMm = thkMm
                    };
                })
                .ToList();

            // 4. Collect Ceiling Types
            var ceilingTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(CeilingType))
                .Cast<CeilingType>()
                .OrderBy(ct => ct.Name)
                .Select(ct => new ElementTypeOption
                {
                    Id = ct.Id,
                    Name = $"Ceiling : {ct.Name}",
                    ThicknessMm = 15.0
                })
                .ToList();

            // 5. Find existing GOAT FINISHES comments to flag rooms that already have generated finishes
            var existingTaggedComments = new HashSet<long>();
            try
            {
                var allWallsFloorsCeilings = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WherePasses(new LogicalOrFilter(new List<ElementFilter>
                    {
                        new ElementClassFilter(typeof(Wall)),
                        new ElementClassFilter(typeof(Floor)),
                        new ElementClassFilter(typeof(Ceiling))
                    }));

                foreach (Element el in allWallsFloorsCeilings)
                {
                    string cmt = el.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
                    if (!string.IsNullOrEmpty(cmt) && cmt.StartsWith("GOAT_FINISH_ROOM_", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = cmt.Split('|')[0].Trim().Split('_');
                        if (parts.Length >= 4 && long.TryParse(parts[3], out long rId))
                        {
                            existingTaggedComments.Add(rId);
                        }
                    }
                }
            }
            catch { }

            // 6. Collect Doors by Host Wall for boundary door-break visualization
            var hostWallsWithDoors = new HashSet<long>();
            try
            {
                var doors = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>();
                foreach (var d in doors)
                {
                    if (d.Host != null)
                        hostWallsWithDoors.Add(d.Host.Id.Value);
                }
            }
            catch { }

            // 7. Collect Rooms
            bool activeViewOnly = Window.IsActiveViewScope;
            FilteredElementCollector roomCollector = (activeViewOnly && doc.ActiveView != null)
                ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                : new FilteredElementCollector(doc);

            var rooms = roomCollector
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<Room>()
                .OrderBy(r => r.Number)
                .ThenBy(r => r.Name)
                .ToList();

            var boundaryOpts = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            var roomItems = new List<RoomFinishItem>();
            foreach (Room room in rooms)
            {
                bool isEnclosed = room.Area > 0.001 && room.Location != null;
                double areaSqm = isEnclosed ? Math.Round(UnitUtils.ConvertFromInternalUnits(room.Area, UnitTypeId.SquareMeters), 2) : 0;
                double perimMm = isEnclosed ? Math.Round(UnitUtils.ConvertFromInternalUnits(room.Perimeter, UnitTypeId.Millimeters), 0) : 0;
                double unboundMm = Math.Round(UnitUtils.ConvertFromInternalUnits(room.UnboundedHeight, UnitTypeId.Millimeters), 0);
                if (unboundMm < 1000) unboundMm = 3000;

                bool hasExisting = existingTaggedComments.Contains(room.Id.Value);
                string healthStatus = !isEnclosed ? "⚠ Not Enclosed" : (hasExisting ? "↺ Update Ready" : "● Ready");
                string healthColor = !isEnclosed ? "#EF4444" : (hasExisting ? "#0284C7" : "#10B981");

                var segs2D = new List<RoomBoundarySegment2D>();
                int segCount = 0;
                int doorCount = 0;

                if (isEnclosed)
                {
                    try
                    {
                        IList<IList<BoundarySegment>> loops = room.GetBoundarySegments(boundaryOpts);
                        if (loops != null && loops.Count > 0)
                        {
                            foreach (BoundarySegment bSeg in loops[0])
                            {
                                Curve c = bSeg.GetCurve();
                                if (c == null) continue;
                                XYZ p0 = c.GetEndPoint(0);
                                XYZ p1 = c.GetEndPoint(1);
                                bool hasDoor = bSeg.ElementId != ElementId.InvalidElementId && hostWallsWithDoors.Contains(bSeg.ElementId.Value);
                                if (hasDoor) doorCount++;

                                bool isCol = false;
                                if (bSeg.ElementId != ElementId.InvalidElementId)
                                {
                                    Element hostEl = doc.GetElement(bSeg.ElementId);
                                    if (hostEl?.Category != null)
                                    {
                                        long catId = hostEl.Category.Id.Value;
                                        if (catId == (long)BuiltInCategory.OST_StructuralColumns || catId == (long)BuiltInCategory.OST_Columns)
                                            isCol = true;
                                    }
                                }

                                segs2D.Add(new RoomBoundarySegment2D
                                {
                                    X1 = p0.X,
                                    Y1 = p0.Y,
                                    X2 = p1.X,
                                    Y2 = p1.Y,
                                    HasDoorOpening = hasDoor,
                                    IsColumnFace = isCol
                                });
                                segCount++;
                            }
                        }
                    }
                    catch { }
                }

                string rName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? room.Name ?? "Room";
                string rNum = room.Number ?? "";

                roomItems.Add(new RoomFinishItem
                {
                    RoomId = room.Id,
                    LevelId = room.LevelId,
                    RoomNumber = rNum,
                    RoomName = rName,
                    LevelName = doc.GetElement(room.LevelId)?.Name ?? "",
                    AreaSqm = areaSqm,
                    PerimeterMm = perimMm,
                    UnboundedHeightMm = unboundMm,
                    WallSegmentsCount = Math.Max(4, segCount),
                    DoorCount = doorCount,
                    IsEnclosed = isEnclosed,
                    HasExistingGoatFinishes = hasExisting,
                    HealthStatus = healthStatus,
                    HealthColor = healthColor,
                    WallFinishParam = room.get_Parameter(BuiltInParameter.ROOM_FINISH_WALL)?.AsString() ?? "",
                    BaseFinishParam = room.get_Parameter(BuiltInParameter.ROOM_FINISH_BASE)?.AsString() ?? "",
                    FloorFinishParam = room.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.AsString() ?? "",
                    CeilingFinishParam = room.get_Parameter(BuiltInParameter.ROOM_FINISH_CEILING)?.AsString() ?? "",
                    Segments2D = segs2D,
                    IsSelected = isEnclosed
                });
            }

            Window.ApplyScannedData(wallTypes, sweepTypes, floorTypes, ceilingTypes, roomItems);
        }

        private void ExecuteGenerateFinishes(Document doc)
        {
            GoatFinishesSettings cfg = Window.CollectCurrentSettings();
            GoatSettingsManager.Save("GoatFinishes", cfg);

            List<RoomFinishItem> selectedRooms = Window.GetSelectedRooms();
            if (selectedRooms.Count == 0)
            {
                Window.UpdateStatus("Please select at least one room to generate finishes.", isError: true);
                return;
            }

            // Collect Types lookup dictionaries
            var allWallTypes = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Where(w => w.Kind == WallKind.Basic).ToList();
            var allFloorTypes = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().Where(f => !f.IsFoundationSlab).ToList();
            var allCeilingTypes = new FilteredElementCollector(doc).OfClass(typeof(CeilingType)).Cast<CeilingType>().ToList();
            var allSweepTypes = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Cornices).WhereElementIsElementType().Cast<ElementType>().ToList();

            WallType defaultWallType = FindBestWallType(allWallTypes, cfg.SelectedWallTypeName);
            WallType columnWallType = FindBestWallType(allWallTypes, cfg.SelectedColumnWallTypeName) ?? defaultWallType;
            WallType skirtingWallType = FindBestWallType(allWallTypes, cfg.SelectedSkirtingTypeName) ?? defaultWallType;
            ElementType skirtingSweepType = allSweepTypes.FirstOrDefault(s => cfg.SelectedSkirtingTypeName.Contains(s.Name, StringComparison.OrdinalIgnoreCase)) ?? allSweepTypes.FirstOrDefault();
            FloorType defaultFloorType = FindBestType(allFloorTypes, cfg.SelectedFloorTypeName);
            CeilingType defaultCeilingType = FindBestType(allCeilingTypes, cfg.SelectedCeilingTypeName);

            var boundaryOpts = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            // Collect all doors in the document so we can break skirting / walls at door openings if needed
            var allDoors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .OfType<FamilyInstance>()
                .ToList();

            int createdWalls = 0;
            int createdSkirtings = 0;
            int createdFloors = 0;
            int createdCeilings = 0;
            int updatedRoomParams = 0;
            int replacedOldElements = 0;

            // ONE CTRL+Z TRANSACTION GROUP
            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT FINISHES — Generate Room Finishes"))
            {
                tg.Start();

                using (Transaction trans = new Transaction(doc, "Generate Parametric Room Finishes"))
                {
                    trans.Start();

                    // Suppress minor join warnings automatically so batch processing is smooth
                    FailureHandlingOptions failOpts = trans.GetFailureHandlingOptions();
                    failOpts.SetFailuresPreprocessor(new GoatSilentWarningPreprocessor());
                    trans.SetFailureHandlingOptions(failOpts);

                    // 1. Smart Replace Existing finishes & ceilings for selected rooms (including manual ceilings inside target rooms!)
                    if (cfg.SmartReplaceExisting)
                    {
                        var roomTagPrefixes = selectedRooms
                            .SelectMany(r => new[] { $"GOAT_FINISH_ROOM_{r.RoomId.Value}", $"GOAT_CEILING_ROOM_{r.RoomId.Value}" })
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var selectedRoomElements = selectedRooms
                            .Select(r => doc.GetElement(r.RoomId) as Room)
                            .Where(r => r != null && r.Area > 0.001)
                            .ToList();

                        var existingElementsToDelete = new HashSet<ElementId>();
                        var candidates = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .WherePasses(new LogicalOrFilter(new List<ElementFilter>
                            {
                                new ElementClassFilter(typeof(Wall)),
                                new ElementClassFilter(typeof(Floor)),
                                new ElementClassFilter(typeof(Ceiling)),
                                new ElementClassFilter(typeof(WallSweep))
                            }));

                        foreach (Element el in candidates)
                        {
                            string cmt = el.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
                            if (!string.IsNullOrEmpty(cmt))
                            {
                                string prefix = cmt.Split('|')[0].Trim();
                                if (roomTagPrefixes.Contains(prefix))
                                {
                                    existingElementsToDelete.Add(el.Id);
                                    continue;
                                }
                            }

                            // Also detect any existing Ceiling inside a selected room when CreateCeilings is enabled
                            if (cfg.CreateCeilings && el is Ceiling existingCeil)
                            {
                                BoundingBoxXYZ cbb = existingCeil.get_BoundingBox(null);
                                if (cbb != null)
                                {
                                    XYZ mid = (cbb.Min + cbb.Max) * 0.5;
                                    foreach (Room rm in selectedRoomElements)
                                    {
                                        if (existingCeil.LevelId != rm.LevelId) continue;
                                        BoundingBoxXYZ rbb = rm.get_BoundingBox(null);
                                        double testZ = rbb != null ? rbb.Min.Z + 1.0 : mid.Z;
                                        try
                                        {
                                            if (rm.IsPointInRoom(new XYZ(mid.X, mid.Y, testZ)))
                                            {
                                                existingElementsToDelete.Add(existingCeil.Id);
                                                break;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }

                        foreach (ElementId delId in existingElementsToDelete)
                        {
                            try
                            {
                                doc.Delete(delId);
                                replacedOldElements++;
                            }
                            catch { }
                        }
                    }

                    // 2. Process each selected room
                    foreach (RoomFinishItem item in selectedRooms)
                    {
                        if (cfg.SkipUnenclosedRooms && !item.IsEnclosed)
                            continue;

                        Room room = doc.GetElement(item.RoomId) as Room;
                        if (room == null || room.Area <= 0.001 || room.LevelId == ElementId.InvalidElementId)
                            continue;

                        Level level = doc.GetElement(room.LevelId) as Level;
                        if (level == null) continue;

                        IList<IList<BoundarySegment>> boundaryLoops = room.GetBoundarySegments(boundaryOpts);
                        if (boundaryLoops == null || boundaryLoops.Count == 0 || boundaryLoops[0].Count < 3)
                            continue;

                        IList<BoundarySegment> outerLoop = boundaryLoops[0];
                        string roomTagPrefix = $"GOAT_FINISH_ROOM_{room.Id.Value}";

                        // Resolve Types (Same on all vs Read from Room Data with fallback)
                        WallType roomWallType = defaultWallType;
                        if (!cfg.WallSameOnAll && !string.IsNullOrWhiteSpace(item.WallFinishParam))
                        {
                            roomWallType = FindBestWallType(allWallTypes, item.WallFinishParam) ?? defaultWallType;
                        }

                        FloorType roomFloorType = defaultFloorType;
                        if (!cfg.FloorSameOnAll && !string.IsNullOrWhiteSpace(item.FloorFinishParam))
                        {
                            roomFloorType = FindBestType(allFloorTypes, item.FloorFinishParam) ?? defaultFloorType;
                        }

                        CeilingType roomCeilingType = defaultCeilingType;
                        if (!cfg.CeilingSameOnAll && !string.IsNullOrWhiteSpace(item.CeilingFinishParam))
                        {
                            roomCeilingType = FindBestType(allCeilingTypes, item.CeilingFinishParam) ?? defaultCeilingType;
                        }

                        double floorOffsetFeet = UnitUtils.ConvertToInternalUnits(cfg.FloorOffsetMm, UnitTypeId.Millimeters);
                        double floorThicknessFeet = GetFloorThicknessFeet(roomFloorType);
                        double ceilingHeightFeet = UnitUtils.ConvertToInternalUnits(cfg.CeilingHeightMm, UnitTypeId.Millimeters);
                        double voidOffsetFeet = UnitUtils.ConvertToInternalUnits(cfg.CeilingVoidOffsetMm, UnitTypeId.Millimeters);
                        double skirtingHeightFeet = UnitUtils.ConvertToInternalUnits(Math.Max(20.0, cfg.SkirtingHeightMm), UnitTypeId.Millimeters);

                        // A. CREATE FINISH FLOOR
                        if (cfg.CreateFloors && roomFloorType != null)
                        {
                            try
                            {
                                CurveLoop floorCurveLoop = BuildHealedClosedCurveLoop(outerLoop, level.Elevation, room, allDoors, cfg.ExtendIntoDoorThresholds, cfg.DoorThresholdDepthMm);
                                if (floorCurveLoop != null)
                                {
                                    Floor finishFloor = Floor.Create(doc, new List<CurveLoop> { floorCurveLoop }, roomFloorType.Id, room.LevelId);
                                    if (finishFloor != null)
                                    {
                                        // Place top of finish floor at FloorOffsetMm + floorThicknessFeet above structural slab
                                        Parameter hParam = finishFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                                        if (hParam != null && !hParam.IsReadOnly)
                                        {
                                            hParam.Set(floorOffsetFeet + floorThicknessFeet);
                                        }
                                        SetElementComment(finishFloor, $"{roomTagPrefix} | Floor | {room.Number}");
                                        createdFloors++;
                                    }
                                }
                            }
                            catch { }
                        }

                        // B. CREATE FINISH CEILING
                        if (cfg.CreateCeilings && roomCeilingType != null)
                        {
                            try
                            {
                                CurveLoop ceilingCurveLoop = BuildHealedClosedCurveLoop(outerLoop, level.Elevation, room, allDoors, false, 0);
                                if (ceilingCurveLoop != null)
                                {
                                    Ceiling finishCeiling = Ceiling.Create(doc, new List<CurveLoop> { ceilingCurveLoop }, roomCeilingType.Id, room.LevelId);
                                    if (finishCeiling != null)
                                    {
                                        Parameter chParam = finishCeiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                                        if (chParam != null && !chParam.IsReadOnly)
                                        {
                                            chParam.Set(ceilingHeightFeet);
                                        }
                                        SetElementComment(finishCeiling, $"{roomTagPrefix} | Ceiling | {room.Number}");
                                        createdCeilings++;
                                    }
                                }
                            }
                            catch { }
                        }

                        // C. CREATE FINISH WALLS & SKIRTING
                        double wallBaseFeet = cfg.WallBaseOnSlab ? 0.0 : (floorOffsetFeet + floorThicknessFeet);
                        if (cfg.CreateSkirting && !cfg.SkirtingInFrontOfWall)
                        {
                            // Flush skirting sits below the finish wall
                            wallBaseFeet += skirtingHeightFeet;
                        }

                        double wallTopFeet = cfg.WallTopAtCeiling
                            ? (ceilingHeightFeet + voidOffsetFeet)
                            : Math.Max(ceilingHeightFeet, room.UnboundedHeight);

                        double wallHeightFeet = Math.Max(UnitUtils.ConvertToInternalUnits(300.0, UnitTypeId.Millimeters), wallTopFeet - wallBaseFeet);

                        foreach (BoundarySegment bSeg in outerLoop)
                        {
                            Curve rawCurve = bSeg.GetCurve();
                            if (rawCurve == null || rawCurve.Length < UnitUtils.ConvertToInternalUnits(40.0, UnitTypeId.Millimeters))
                                continue;

                            Element hostEl = bSeg.ElementId != ElementId.InvalidElementId ? doc.GetElement(bSeg.ElementId) : null;
                            bool isCol = false;
                            if (hostEl?.Category != null)
                            {
                                long catId = hostEl.Category.Id.Value;
                                if (catId == (long)BuiltInCategory.OST_StructuralColumns || catId == (long)BuiltInCategory.OST_Columns)
                                    isCol = true;
                            }

                            // Column Handling: 0 = Wrap Same, 1 = Specific Type, 2 = Ignore
                            if (isCol && cfg.ColumnHandlingMode == 2)
                                continue;

                            WallType activeSegWallType = (isCol && cfg.ColumnHandlingMode == 1 && columnWallType != null)
                                ? columnWallType
                                : roomWallType;

                            double finishWallWidthFeet = activeSegWallType != null ? activeSegWallType.Width : UnitUtils.ConvertToInternalUnits(15.0, UnitTypeId.Millimeters);

                            // Offset curve inward into the room by half the finish wall width
                            Curve inwardWallCurve = OffsetCurveInward(rawCurve, finishWallWidthFeet * 0.5, level.Elevation);

                            // Create Finish Wall(s)
                            List<Wall> segCreatedWalls = new List<Wall>();
                            if (cfg.CreateWalls && activeSegWallType != null && inwardWallCurve != null)
                            {
                                List<Curve> wallSubCurves = cfg.BreakWallsAtDoors
                                    ? SplitCurveAtDoors(inwardWallCurve, hostEl, allDoors)
                                    : new List<Curve> { inwardWallCurve };

                                foreach (Curve subCurve in wallSubCurves)
                                {
                                    if (subCurve.Length < UnitUtils.ConvertToInternalUnits(40.0, UnitTypeId.Millimeters))
                                        continue;

                                    try
                                    {
                                        Wall finWall = Wall.Create(doc, subCurve, activeSegWallType.Id, room.LevelId, wallHeightFeet, wallBaseFeet, false, false);
                                        if (finWall != null)
                                        {
                                            Parameter roomBoundParam = finWall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                                            if (roomBoundParam != null && !roomBoundParam.IsReadOnly)
                                                roomBoundParam.Set(0); // Finish walls should not shrink the room's net area

                                            SetElementComment(finWall, $"{roomTagPrefix} | Wall | {room.Number}");
                                            segCreatedWalls.Add(finWall);
                                            createdWalls++;

                                            if (cfg.AutoJoinWithHostWalls && hostEl is Wall hostWall)
                                            {
                                                try
                                                {
                                                    if (!JoinGeometryUtils.AreElementsJoined(doc, hostWall, finWall))
                                                        JoinGeometryUtils.JoinGeometry(doc, hostWall, finWall);
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }

                            // Create Skirting (always breaks at door openings)
                            if (cfg.CreateSkirting)
                            {
                                if (cfg.SkirtingMethod == 1 && skirtingSweepType != null && segCreatedWalls.Count > 0)
                                {
                                    // Method 1: Hosted WallSweep on created finish walls
                                    foreach (Wall targetWall in segCreatedWalls)
                                    {
                                        try
                                        {
                                            WallSweepInfo sweepInfo = new WallSweepInfo(WallSweepType.Sweep, false)
                                            {
                                                Distance = 0.0,
                                                WallSide = WallSide.Interior,
                                                DistanceMeasuredFrom = DistanceMeasuredFrom.Base
                                            };
                                            WallSweep ws = WallSweep.Create(targetWall, skirtingSweepType.Id, sweepInfo);
                                            if (ws != null)
                                            {
                                                SetElementComment(ws, $"{roomTagPrefix} | SkirtingSweep | {room.Number}");
                                                createdSkirtings++;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                else if (skirtingWallType != null)
                                {
                                    // Method 0 (Recommended): Parametric Skirting Wall along door-broken segments
                                    double skirtWidthFeet = skirtingWallType.Width;
                                    double skirtOffsetFeet = cfg.SkirtingInFrontOfWall
                                        ? ((cfg.CreateWalls ? finishWallWidthFeet : 0.0) + skirtWidthFeet * 0.5)
                                        : (finishWallWidthFeet * 0.5);

                                    Curve skirtBaseCurve = OffsetCurveInward(rawCurve, skirtOffsetFeet, level.Elevation);
                                    List<Curve> skirtSubCurves = SplitCurveAtDoors(skirtBaseCurve, hostEl, allDoors);
                                    double skirtBaseElevationOffset = floorOffsetFeet + floorThicknessFeet;

                                    foreach (Curve sCurve in skirtSubCurves)
                                    {
                                        if (sCurve.Length < UnitUtils.ConvertToInternalUnits(40.0, UnitTypeId.Millimeters))
                                            continue;

                                        try
                                        {
                                            Wall skirtWall = Wall.Create(doc, sCurve, skirtingWallType.Id, room.LevelId, skirtingHeightFeet, skirtBaseElevationOffset, false, false);
                                            if (skirtWall != null)
                                            {
                                                Parameter rbParam = skirtWall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
                                                if (rbParam != null && !rbParam.IsReadOnly)
                                                    rbParam.Set(0);

                                                SetElementComment(skirtWall, $"{roomTagPrefix} | Skirting | {room.Number}");
                                                createdSkirtings++;

                                                if (cfg.SkirtingJoinWithWall && segCreatedWalls.Count > 0)
                                                {
                                                    foreach (Wall fw in segCreatedWalls)
                                                    {
                                                        try
                                                        {
                                                            if (!JoinGeometryUtils.AreElementsJoined(doc, fw, skirtWall))
                                                                JoinGeometryUtils.JoinGeometry(doc, fw, skirtWall);
                                                        }
                                                        catch { }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }

                        // D. WRITE ROOM FINISH PARAMETERS BACK TO ROOM
                        if (cfg.WriteRoomFinishParameters)
                        {
                            try
                            {
                                if (cfg.CreateWalls && roomWallType != null)
                                {
                                    string val = cfg.SyncTypeNameToRoomParams ? roomWallType.Name : cfg.CustomWallFinishCode;
                                    SetRoomStringParam(room, BuiltInParameter.ROOM_FINISH_WALL, val);
                                }
                                if (cfg.CreateSkirting)
                                {
                                    string skName = cfg.SkirtingMethod == 1 && skirtingSweepType != null
                                        ? skirtingSweepType.Name
                                        : (skirtingWallType != null ? skirtingWallType.Name : cfg.CustomBaseFinishCode);
                                    string val = cfg.SyncTypeNameToRoomParams ? skName : cfg.CustomBaseFinishCode;
                                    SetRoomStringParam(room, BuiltInParameter.ROOM_FINISH_BASE, val);
                                }
                                if (cfg.CreateFloors && roomFloorType != null)
                                {
                                    string val = cfg.SyncTypeNameToRoomParams ? roomFloorType.Name : cfg.CustomFloorFinishCode;
                                    SetRoomStringParam(room, BuiltInParameter.ROOM_FINISH_FLOOR, val);
                                }
                                if (cfg.CreateCeilings && roomCeilingType != null)
                                {
                                    string val = cfg.SyncTypeNameToRoomParams ? roomCeilingType.Name : cfg.CustomCeilingFinishCode;
                                    SetRoomStringParam(room, BuiltInParameter.ROOM_FINISH_CEILING, val);
                                }
                                updatedRoomParams++;
                            }
                            catch { }
                        }
                    }

                    trans.Commit();
                }

                tg.Assimilate();
            }

            string replaceNote = replacedOldElements > 0 ? $" (Replaced {replacedOldElements} old elements)" : "";
            Window.UpdateStatus(
                $"✔ Generated {createdWalls} Finish Walls · {createdSkirtings} Skirtings · {createdFloors} Floors · {createdCeilings} Ceilings · Updated {updatedRoomParams} Rooms{replaceNote}. (1x Ctrl+Z to Undo)",
                isError: false);
        }

        private static WallType FindBestWallType(List<WallType> wallTypes, string requestedName)
        {
            if (wallTypes == null || wallTypes.Count == 0) return null;
            if (!string.IsNullOrWhiteSpace(requestedName))
            {
                string clean = requestedName.Replace("Basic Wall :", "").Trim();
                var exact = wallTypes.FirstOrDefault(w => string.Equals(w.Name, clean, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;

                var partial = wallTypes.FirstOrDefault(w => w.Name.Contains(clean, StringComparison.OrdinalIgnoreCase) || clean.Contains(w.Name, StringComparison.OrdinalIgnoreCase));
                if (partial != null) return partial;
            }
            // Prefer thin finish wall if available
            return wallTypes.OrderBy(w => w.Width).FirstOrDefault();
        }

        private static T FindBestType<T>(List<T> types, string requestedName) where T : ElementType
        {
            if (types == null || types.Count == 0) return null;
            if (!string.IsNullOrWhiteSpace(requestedName))
            {
                string clean = requestedName.Replace("Floor :", "").Replace("Ceiling :", "").Trim();
                var exact = types.FirstOrDefault(t => string.Equals(t.Name, clean, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;

                var partial = types.FirstOrDefault(t => t.Name.Contains(clean, StringComparison.OrdinalIgnoreCase) || clean.Contains(t.Name, StringComparison.OrdinalIgnoreCase));
                if (partial != null) return partial;
            }
            return types[0];
        }

        private static double GetFloorThicknessFeet(FloorType ft)
        {
            if (ft == null) return 0.0;
            try
            {
                CompoundStructure cs = ft.GetCompoundStructure();
                if (cs != null)
                    return cs.GetWidth();
            }
            catch { }
            return UnitUtils.ConvertToInternalUnits(20.0, UnitTypeId.Millimeters);
        }

        private static Curve OffsetCurveInward(Curve rawCurve, double offsetDistFeet, double targetZ)
        {
            try
            {
                XYZ p0 = new XYZ(rawCurve.GetEndPoint(0).X, rawCurve.GetEndPoint(0).Y, targetZ);
                XYZ p1 = new XYZ(rawCurve.GetEndPoint(1).X, rawCurve.GetEndPoint(1).Y, targetZ);
                if (p0.DistanceTo(p1) < 1e-4) return null;

                // Revit room outer boundary loops are oriented clockwise/counter-clockwise consistently;
                // try CreateOffset or compute inward normal
                Line flatLine = Line.CreateBound(p0, p1);
                XYZ dir = (p1 - p0).Normalize();
                // Left-hand normal in XY plane
                XYZ normal = new XYZ(-dir.Y, dir.X, 0);
                XYZ shift = normal * offsetDistFeet;
                return Line.CreateBound(p0 + shift, p1 + shift);
            }
            catch
            {
                return rawCurve;
            }
        }

        private static List<Curve> SplitCurveAtDoors(Curve segCurve, Element hostEl, List<FamilyInstance> allDoors)
        {
            var result = new List<Curve>();
            if (segCurve == null) return result;
            if (hostEl == null || allDoors == null || allDoors.Count == 0 || !(segCurve is Line line))
            {
                result.Add(segCurve);
                return result;
            }

            var hostedDoors = allDoors.Where(d => d.Host != null && d.Host.Id == hostEl.Id).ToList();
            if (hostedDoors.Count == 0)
            {
                result.Add(segCurve);
                return result;
            }

            XYZ start = line.GetEndPoint(0);
            XYZ end = line.GetEndPoint(1);
            XYZ dir = (end - start).Normalize();
            double totalLen = start.DistanceTo(end);

            var intervals = new List<(double StartT, double EndT)>();
            foreach (FamilyInstance door in hostedDoors)
            {
                if (!(door.Location is LocationPoint lp)) continue;
                XYZ doorPt = new XYZ(lp.Point.X, lp.Point.Y, start.Z);

                // Project door center onto segment line
                XYZ v = doorPt - start;
                double proj = v.DotProduct(dir);
                XYZ closest = start + dir * Math.Clamp(proj, 0, totalLen);
                if (closest.DistanceTo(doorPt) > UnitUtils.ConvertToInternalUnits(600.0, UnitTypeId.Millimeters))
                    continue; // Door is on a different segment of the wall

                double doorWidth = UnitUtils.ConvertToInternalUnits(900.0, UnitTypeId.Millimeters);
                Parameter wParam = door.get_Parameter(BuiltInParameter.DOOR_WIDTH)
                                ?? door.Symbol?.get_Parameter(BuiltInParameter.DOOR_WIDTH)
                                ?? door.Symbol?.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM);
                if (wParam != null && wParam.AsDouble() > 0.1)
                {
                    doorWidth = wParam.AsDouble();
                }

                double t0 = Math.Max(0.0, proj - doorWidth * 0.5);
                double t1 = Math.Min(totalLen, proj + doorWidth * 0.5);
                if (t1 > t0 + 0.05)
                {
                    intervals.Add((t0, t1));
                }
            }

            if (intervals.Count == 0)
            {
                result.Add(segCurve);
                return result;
            }

            intervals = intervals.OrderBy(i => i.StartT).ToList();
            double cursor = 0.0;
            double minSegFeet = UnitUtils.ConvertToInternalUnits(50.0, UnitTypeId.Millimeters);

            foreach (var gap in intervals)
            {
                if (gap.StartT - cursor >= minSegFeet)
                {
                    result.Add(Line.CreateBound(start + dir * cursor, start + dir * gap.StartT));
                }
                cursor = Math.Max(cursor, gap.EndT);
            }

            if (totalLen - cursor >= minSegFeet)
            {
                result.Add(Line.CreateBound(start + dir * cursor, end));
            }

            return result;
        }

        private static CurveLoop BuildHealedClosedCurveLoop(
            IList<BoundarySegment> segments,
            double elevationZ,
            Room room,
            List<FamilyInstance> allDoors,
            bool extendIntoDoors,
            double thresholdDepthMm)
        {
            var vertices = new List<XYZ>();
            foreach (BoundarySegment seg in segments)
            {
                Curve c = seg.GetCurve();
                if (c == null) continue;
                XYZ p = c.GetEndPoint(0);
                XYZ flatPt = new XYZ(p.X, p.Y, elevationZ);
                if (vertices.Count == 0 || vertices[vertices.Count - 1].DistanceTo(flatPt) > 0.01)
                {
                    vertices.Add(flatPt);
                }
            }

            if (vertices.Count < 3) return null;

            // Remove duplicate closing vertex if present
            if (vertices.Count > 3 && vertices[0].DistanceTo(vertices[vertices.Count - 1]) <= 0.01)
            {
                vertices.RemoveAt(vertices.Count - 1);
            }

            if (vertices.Count < 3) return null;

            CurveLoop loop = new CurveLoop();
            for (int i = 0; i < vertices.Count; i++)
            {
                XYZ a = vertices[i];
                XYZ b = vertices[(i + 1) % vertices.Count];
                if (a.DistanceTo(b) > 0.005)
                {
                    loop.Append(Line.CreateBound(a, b));
                }
            }

            return loop;
        }

        private static void SetElementComment(Element el, string comment)
        {
            if (el == null) return;
            Parameter p = el.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (p != null && !p.IsReadOnly)
            {
                p.Set(comment);
            }
        }

        private static void SetRoomStringParam(Room room, BuiltInParameter bip, string value)
        {
            if (room == null || string.IsNullOrWhiteSpace(value)) return;
            Parameter p = room.get_Parameter(bip);
            if (p != null && !p.IsReadOnly)
            {
                p.Set(value);
            }
        }
    }

    internal class GoatSilentWarningPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor f in failures)
            {
                if (f.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(f);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
