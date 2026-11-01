using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace ORTools.Models
{
    /// <summary>
    /// Shared backend engines for GOAT DIM, GOAT ANNOTATE, GOAT ROOM (Glyph Bundle), and GOAT ALIGNER.
    /// Allows standalone tools and one-click room bundles to share identical high-precision logic.
    /// </summary>
    public static class GoatDocEngines
    {
        // =========================================================================
        // 1. GOAT DIM ENGINE (Interior Rooms, Exterior Building & Elevations)
        // =========================================================================

        public static int DimensionRoomInPlan(
            Document doc,
            View view,
            Room room,
            DimensionType dimType,
            bool dimClearCross,
            bool dimOpeningsChain,
            double stackOffsetMm,
            double wallOffsetMm = 500.0,
            bool showMmSuffix = false)
        {
            if (doc == null || view == null || room == null || room.Area <= 0.001)
                return 0;

            var boundaryOpts = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            IList<IList<BoundarySegment>> loops = room.GetBoundarySegments(boundaryOpts);
            if (loops == null || loops.Count == 0 || loops[0].Count < 3)
                return 0;

            BoundingBoxXYZ bb = room.get_BoundingBox(null);
            if (bb == null) return 0;
            double z = view.GenLevel != null ? view.GenLevel.Elevation : bb.Min.Z;

            double wallOffFeet = UnitUtils.ConvertToInternalUnits(Math.Max(150.0, wallOffsetMm), UnitTypeId.Millimeters);
            double stackOffFeet = UnitUtils.ConvertToInternalUnits(Math.Max(150.0, stackOffsetMm), UnitTypeId.Millimeters);

            // Clamp wall offset so it stays inside small rooms while avoiding the center Room Tag
            double roomSpanX = Math.Max(1.0, bb.Max.X - bb.Min.X);
            double roomSpanY = Math.Max(1.0, bb.Max.Y - bb.Min.Y);
            double safeWallOffX = Math.Min(wallOffFeet, roomSpanX * 0.35);
            double safeWallOffY = Math.Min(wallOffFeet, roomSpanY * 0.35);

            // Position dimension lines measured from the South (Min.Y) and West (Min.X) room walls
            XYZ baseDimPt = new XYZ(bb.Min.X + safeWallOffX, bb.Min.Y + safeWallOffY, z);

            var vertWallRefs = new ReferenceArray(); // Walls running North-South (measured along X)
            var horizWallRefs = new ReferenceArray(); // Walls running East-West (measured along Y)
            var vertWithOpeningsRefs = new ReferenceArray();
            var horizWithOpeningsRefs = new ReferenceArray();

            var seenHostIds = new HashSet<long>();

            foreach (BoundarySegment seg in loops[0])
            {
                if (seg.ElementId == ElementId.InvalidElementId || seenHostIds.Contains(seg.ElementId.Value))
                    continue;

                if (!(doc.GetElement(seg.ElementId) is Wall wall))
                    continue;

                Curve c = seg.GetCurve();
                if (c == null) continue;
                XYZ dir = (c.GetEndPoint(1) - c.GetEndPoint(0)).Normalize();

                Reference extFaceRef = GetWallFaceReference(wall, false) ?? GetWallFaceReference(wall, true);
                if (extFaceRef == null) continue;

                seenHostIds.Add(wall.Id.Value);

                // Vertical wall (runs along Y, so its normal is along X -> measured by horizontal X dimension line)
                if (Math.Abs(dir.Y) >= Math.Abs(dir.X))
                {
                    vertWallRefs.Append(extFaceRef);
                    vertWithOpeningsRefs.Append(extFaceRef);
                }
                else
                {
                    horizWallRefs.Append(extFaceRef);
                    horizWithOpeningsRefs.Append(extFaceRef);
                }
            }

            int createdDims = 0;

            if (dimClearCross)
            {
                if (vertWallRefs.Size >= 2)
                {
                    try
                    {
                        Line xLine = Line.CreateBound(baseDimPt - XYZ.BasisX * 50.0, baseDimPt + XYZ.BasisX * 50.0);
                        Dimension d = doc.Create.NewDimension(view, xLine, vertWallRefs, dimType);
                        if (d != null)
                        {
                            ApplyDimensionUnitSuffix(d, showMmSuffix);
                            createdDims++;
                        }
                    }
                    catch { }
                }

                if (horizWallRefs.Size >= 2)
                {
                    try
                    {
                        Line yLine = Line.CreateBound(baseDimPt - XYZ.BasisY * 50.0, baseDimPt + XYZ.BasisY * 50.0);
                        Dimension d = doc.Create.NewDimension(view, yLine, horizWallRefs, dimType);
                        if (d != null)
                        {
                            ApplyDimensionUnitSuffix(d, showMmSuffix);
                            createdDims++;
                        }
                    }
                    catch { }
                }
            }

            if (dimOpeningsChain)
            {
                // Append door & window center/side references hosted on room bounding walls
                var inserts = new FilteredElementCollector(doc, view.Id)
                    .WhereElementIsNotElementType()
                    .OfType<FamilyInstance>()
                    .Where(fi => fi.Host != null && seenHostIds.Contains(fi.Host.Id.Value))
                    .ToList();

                foreach (FamilyInstance fi in inserts)
                {
                    if (!(fi.Host is Wall hostWall) || !(hostWall.Location is LocationCurve lc) || lc.Curve == null)
                        continue;

                    XYZ wDir = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
                    IList<Reference> leftRight = fi.GetReferences(FamilyInstanceReferenceType.Left);
                    IList<Reference> rightRefs = fi.GetReferences(FamilyInstanceReferenceType.Right);
                    IList<Reference> centerRefs = fi.GetReferences(FamilyInstanceReferenceType.CenterLeftRight);

                    var refsToAdd = new List<Reference>();
                    if (leftRight != null && leftRight.Count > 0 && rightRefs != null && rightRefs.Count > 0)
                    {
                        refsToAdd.Add(leftRight[0]);
                        refsToAdd.Add(rightRefs[0]);
                    }
                    else if (centerRefs != null && centerRefs.Count > 0)
                    {
                        refsToAdd.Add(centerRefs[0]);
                    }

                    foreach (var r in refsToAdd)
                    {
                        if (Math.Abs(wDir.Y) >= Math.Abs(wDir.X))
                            horizWithOpeningsRefs.Append(r);
                        else
                            vertWithOpeningsRefs.Append(r);
                    }
                }

                if (vertWithOpeningsRefs.Size > vertWallRefs.Size && vertWithOpeningsRefs.Size >= 2)
                {
                    try
                    {
                        XYZ shiftPt = baseDimPt + XYZ.BasisY * stackOffFeet;
                        Line xLine = Line.CreateBound(shiftPt - XYZ.BasisX * 50.0, shiftPt + XYZ.BasisX * 50.0);
                        Dimension d = doc.Create.NewDimension(view, xLine, vertWithOpeningsRefs, dimType);
                        if (d != null)
                        {
                            ApplyDimensionUnitSuffix(d, showMmSuffix);
                            createdDims++;
                        }
                    }
                    catch { }
                }

                if (horizWithOpeningsRefs.Size > horizWallRefs.Size && horizWithOpeningsRefs.Size >= 2)
                {
                    try
                    {
                        XYZ shiftPt = baseDimPt + XYZ.BasisX * stackOffFeet;
                        Line yLine = Line.CreateBound(shiftPt - XYZ.BasisY * 50.0, shiftPt + XYZ.BasisY * 50.0);
                        Dimension d = doc.Create.NewDimension(view, yLine, horizWithOpeningsRefs, dimType);
                        if (d != null)
                        {
                            ApplyDimensionUnitSuffix(d, showMmSuffix);
                            createdDims++;
                        }
                    }
                    catch { }
                }
            }

            return createdDims;
        }

        public static void ApplyDimensionUnitSuffix(Dimension dim, bool showMmSuffix)
        {
            if (dim == null) return;
            string suf = showMmSuffix ? " mm" : string.Empty;
            try
            {
                if (dim.NumberOfSegments > 1)
                {
                    foreach (DimensionSegment seg in dim.Segments)
                    {
                        seg.Suffix = suf;
                    }
                }
                else
                {
                    dim.Suffix = suf;
                }
            }
            catch { }
        }

        public static int DimensionExteriorInPlan(
            Document doc,
            View view,
            DimensionType dimType,
            bool chainGrids,
            bool chainWalls,
            double baseOffsetMm,
            double stackStepMm,
            bool showMmSuffix = false)
        {
            if (doc == null || view == null) return 0;

            var walls = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .ToList();

            var grids = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .ToList();

            if (walls.Count == 0 && grids.Count == 0) return 0;

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            double z = view.GenLevel != null ? view.GenLevel.Elevation : 0.0;

            foreach (Wall w in walls)
            {
                BoundingBoxXYZ bb = w.get_BoundingBox(view);
                if (bb == null) continue;
                minX = Math.Min(minX, bb.Min.X);
                maxX = Math.Max(maxX, bb.Max.X);
                minY = Math.Min(minY, bb.Min.Y);
                maxY = Math.Max(maxY, bb.Max.Y);
            }

            if (minX == double.MaxValue) return 0;

            double baseOffFeet = UnitUtils.ConvertToInternalUnits(Math.Max(600, baseOffsetMm), UnitTypeId.Millimeters);
            double stepFeet = UnitUtils.ConvertToInternalUnits(Math.Max(400, stackStepMm), UnitTypeId.Millimeters);
            int created = 0;

            // 1. Grids Chain (Horizontal X and Vertical Y)
            if (chainGrids && grids.Count >= 2)
            {
                var xGridRefs = new ReferenceArray();
                var yGridRefs = new ReferenceArray();

                foreach (Grid g in grids)
                {
                    if (!(g.Curve is Line gl)) continue;
                    XYZ dir = (gl.GetEndPoint(1) - gl.GetEndPoint(0)).Normalize();
                    Reference gRef = new Reference(g);
                    if (Math.Abs(dir.Y) >= 0.85) xGridRefs.Append(gRef);
                    else if (Math.Abs(dir.X) >= 0.85) yGridRefs.Append(gRef);
                }

                if (xGridRefs.Size >= 2)
                {
                    try
                    {
                        double yPos = maxY + baseOffFeet;
                        Line dimLine = Line.CreateBound(new XYZ(minX - 10, yPos, z), new XYZ(maxX + 10, yPos, z));
                        Dimension d = doc.Create.NewDimension(view, dimLine, xGridRefs, dimType);
                        if (d != null) { ApplyDimensionUnitSuffix(d, showMmSuffix); created++; }
                    }
                    catch { }
                }

                if (yGridRefs.Size >= 2)
                {
                    try
                    {
                        double xPos = minX - baseOffFeet;
                        Line dimLine = Line.CreateBound(new XYZ(xPos, minY - 10, z), new XYZ(xPos, maxY + 10, z));
                        Dimension d = doc.Create.NewDimension(view, dimLine, yGridRefs, dimType);
                        if (d != null) { ApplyDimensionUnitSuffix(d, showMmSuffix); created++; }
                    }
                    catch { }
                }
            }

            // 2. Exterior Wall Segments Chain
            if (chainWalls && walls.Count >= 2)
            {
                var xWallRefs = new ReferenceArray();
                var yWallRefs = new ReferenceArray();

                foreach (Wall w in walls)
                {
                    if (!(w.Location is LocationCurve lc) || !(lc.Curve is Line wl)) continue;
                    XYZ dir = (wl.GetEndPoint(1) - wl.GetEndPoint(0)).Normalize();
                    Reference faceRef = GetWallFaceReference(w, true);
                    if (faceRef == null) continue;

                    if (Math.Abs(dir.Y) >= 0.9) xWallRefs.Append(faceRef);
                    else if (Math.Abs(dir.X) >= 0.9) yWallRefs.Append(faceRef);
                }

                if (xWallRefs.Size >= 2)
                {
                    try
                    {
                        double yPos = maxY + baseOffFeet + stepFeet;
                        Line dimLine = Line.CreateBound(new XYZ(minX - 10, yPos, z), new XYZ(maxX + 10, yPos, z));
                        Dimension d = doc.Create.NewDimension(view, dimLine, xWallRefs, dimType);
                        if (d != null) { ApplyDimensionUnitSuffix(d, showMmSuffix); created++; }
                    }
                    catch { }
                }

                if (yWallRefs.Size >= 2)
                {
                    try
                    {
                        double xPos = minX - (baseOffFeet + stepFeet);
                        Line dimLine = Line.CreateBound(new XYZ(xPos, minY - 10, z), new XYZ(xPos, maxY + 10, z));
                        Dimension d = doc.Create.NewDimension(view, dimLine, yWallRefs, dimType);
                        if (d != null) { ApplyDimensionUnitSuffix(d, showMmSuffix); created++; }
                    }
                    catch { }
                }
            }

            return created;
        }

        public static int DimensionElevationOrSection(Document doc, View view, DimensionType dimType, bool showMmSuffix = false)
        {
            if (doc == null || view == null) return 0;

            var levels = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count < 2) return 0;

            var refArray = new ReferenceArray();
            foreach (Level lvl in levels)
            {
                Reference r = lvl.GetPlaneReference();
                if (r != null) refArray.Append(r);
            }

            if (refArray.Size < 2) return 0;

            BoundingBoxXYZ bb = view.CropBox;
            XYZ origin = bb != null ? bb.Transform.Origin : XYZ.Zero;
            Line vertLine = Line.CreateBound(origin - XYZ.BasisZ * 50.0, origin + XYZ.BasisZ * 50.0);

            try
            {
                Dimension d = doc.Create.NewDimension(view, vertLine, refArray, dimType);
                if (d != null)
                {
                    ApplyDimensionUnitSuffix(d, showMmSuffix);
                    return 1;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static Reference GetWallFaceReference(Wall wall, bool exterior)
        {
            try
            {
                IList<Reference> refs = HostObjectUtils.GetSideFaces(wall, exterior ? ShellLayerType.Exterior : ShellLayerType.Interior);
                if (refs != null && refs.Count > 0) return refs[0];
            }
            catch { }

            try
            {
                Options opt = new Options { ComputeReferences = true, IncludeNonVisibleObjects = true };
                GeometryElement geo = wall.get_Geometry(opt);
                if (geo != null)
                {
                    foreach (GeometryObject obj in geo)
                    {
                        if (obj is Solid s)
                        {
                            foreach (Face f in s.Faces)
                            {
                                if (f is PlanarFace pf && Math.Abs(pf.FaceNormal.Z) < 0.1 && pf.Reference != null)
                                    return pf.Reference;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        // =========================================================================
        // 2. GOAT ANNOTATE ENGINE (Smart Multi-Category Tagging + Anti-Collision)
        // =========================================================================

        public static int TagElementsInView(
            Document doc,
            View view,
            IList<BuiltInCategory> categories,
            bool addLeader,
            bool preventCollisions,
            bool tagRooms)
        {
            if (doc == null || view == null) return 0;
            int createdTags = 0;
            var placedTagCenters = new List<XYZ>();
            double minTagDistFeet = UnitUtils.ConvertToInternalUnits(550.0, UnitTypeId.Millimeters);

            // 1. Tag Rooms if requested
            if (tagRooms && (view.ViewType == ViewType.FloorPlan || view.ViewType == ViewType.CeilingPlan || view.ViewType == ViewType.Section || view.ViewType == ViewType.Elevation))
            {
                var existingTaggedRoomIds = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(SpatialElementTag))
                    .OfType<RoomTag>()
                    .Where(rt => rt.Room != null)
                    .Select(rt => rt.Room.Id.Value)
                    .ToHashSet();

                var rooms = new FilteredElementCollector(doc, view.Id)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .OfType<Room>()
                    .Where(r => r.Area > 0.001 && r.Location is LocationPoint);

                foreach (Room r in rooms)
                {
                    if (existingTaggedRoomIds.Contains(r.Id.Value)) continue;
                    if (!(r.Location is LocationPoint lp)) continue;
                    try
                    {
                        RoomTag rt = doc.Create.NewRoomTag(new LinkElementId(r.Id), new UV(lp.Point.X, lp.Point.Y), view.Id);
                        if (rt != null)
                        {
                            placedTagCenters.Add(lp.Point);
                            createdTags++;
                        }
                    }
                    catch { }
                }
            }

            // 2. Tag Selected Model Categories with Anti-Collision Engine
            if (categories != null && categories.Count > 0)
            {
                // Collect already tagged elements in this view so we never create duplicate tags
                var alreadyTaggedIds = new HashSet<long>();
                var existingTags = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(IndependentTag))
                    .Cast<IndependentTag>();

                foreach (IndependentTag t in existingTags)
                {
                    try
                    {
                        foreach (LinkElementId lid in t.GetTaggedElementIds())
                        {
                            if (lid.HostElementId != ElementId.InvalidElementId)
                                alreadyTaggedIds.Add(lid.HostElementId.Value);
                        }
                        placedTagCenters.Add(t.TagHeadPosition);
                    }
                    catch { }
                }

                foreach (BuiltInCategory bic in categories)
                {
                    if (bic == BuiltInCategory.OST_Rooms) continue;

                    var elements = new FilteredElementCollector(doc, view.Id)
                        .OfCategory(bic)
                        .WhereElementIsNotElementType()
                        .ToList();

                    foreach (Element el in elements)
                    {
                        if (alreadyTaggedIds.Contains(el.Id.Value)) continue;

                        XYZ rawPos = GetElementCenterInView(el, view);
                        if (rawPos == null) continue;

                        XYZ finalTagPos = preventCollisions
                            ? FindNonCollidingTagPoint(rawPos, placedTagCenters, minTagDistFeet, view)
                            : rawPos;

                        bool needsLeader = addLeader || finalTagPos.DistanceTo(rawPos) > 0.15;

                        try
                        {
                            Reference elRef = new Reference(el);
                            IndependentTag tag = IndependentTag.Create(
                                doc,
                                view.Id,
                                elRef,
                                needsLeader,
                                TagMode.TM_ADDBY_CATEGORY,
                                TagOrientation.Horizontal,
                                finalTagPos);

                            if (tag != null)
                            {
                                placedTagCenters.Add(finalTagPos);
                                alreadyTaggedIds.Add(el.Id.Value);
                                createdTags++;
                            }
                        }
                        catch { }
                    }
                }
            }

            return createdTags;
        }

        private static XYZ GetElementCenterInView(Element el, View view)
        {
            if (el.Location is LocationPoint lp)
                return lp.Point;
            if (el.Location is LocationCurve lc && lc.Curve != null)
                return lc.Curve.Evaluate(0.5, true);

            BoundingBoxXYZ bb = el.get_BoundingBox(view) ?? el.get_BoundingBox(null);
            if (bb != null)
                return (bb.Min + bb.Max) * 0.5;

            return null;
        }

        private static XYZ FindNonCollidingTagPoint(XYZ origin, List<XYZ> occupied, double minDistFeet, View view)
        {
            bool Overlaps(XYZ pt)
            {
                foreach (XYZ occ in occupied)
                {
                    double dx = pt.X - occ.X;
                    double dy = pt.Y - occ.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) < minDistFeet)
                        return true;
                }
                return false;
            }

            if (!Overlaps(origin)) return origin;

            XYZ right = view.RightDirection;
            XYZ up = view.UpDirection;

            // Spiral / radial search in view plane
            double[] radii = { minDistFeet, minDistFeet * 1.6, minDistFeet * 2.3, minDistFeet * 3.0 };
            double[] anglesDeg = { 45, -45, 135, -135, 0, 90, 180, -90 };

            foreach (double r in radii)
            {
                foreach (double deg in anglesDeg)
                {
                    double rad = deg * Math.PI / 180.0;
                    XYZ candidate = origin + right * (r * Math.Cos(rad)) + up * (r * Math.Sin(rad));
                    if (!Overlaps(candidate))
                        return candidate;
                }
            }

            return origin + up * minDistFeet;
        }

        // =========================================================================
        // 3. GOAT SHEET PACKER ENGINE (Glyph Auto-Pack Views on Sheet)
        // =========================================================================

        public static int PackViewsOntoSheet(
            Document doc,
            ViewSheet sheet,
            IList<ElementId> viewIdsToPlaceAndPack,
            bool packExistingViewportsOnly = false)
        {
            if (doc == null || sheet == null) return 0;

            var viewports = new List<Viewport>();

            if (!packExistingViewportsOnly && viewIdsToPlaceAndPack != null)
            {
                foreach (ElementId vid in viewIdsToPlaceAndPack)
                {
                    if (vid == null || vid == ElementId.InvalidElementId) continue;
                    if (Viewport.CanAddViewToSheet(doc, sheet.Id, vid))
                    {
                        try
                        {
                            Viewport vp = Viewport.Create(doc, sheet.Id, vid, XYZ.Zero);
                            if (vp != null) viewports.Add(vp);
                        }
                        catch { }
                    }
                }
            }

            if (packExistingViewportsOnly || viewports.Count == 0)
            {
                foreach (ElementId vpId in sheet.GetAllViewports())
                {
                    if (doc.GetElement(vpId) is Viewport vp)
                        viewports.Add(vp);
                }
            }

            if (viewports.Count == 0) return 0;

            // Determine Titleblock usable drawing area
            double minX = 0.12, minY = 0.12;
            double maxX = 2.40, maxY = 1.80; // Default A1 usable space in feet

            var titleBlocks = new FilteredElementCollector(doc, sheet.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsNotElementType()
                .ToList();

            if (titleBlocks.Count > 0)
            {
                BoundingBoxXYZ tbb = titleBlocks[0].get_BoundingBox(sheet);
                if (tbb != null)
                {
                    double w = tbb.Max.X - tbb.Min.X;
                    double h = tbb.Max.Y - tbb.Min.Y;
                    minX = tbb.Min.X + w * 0.05;
                    maxX = tbb.Max.X - w * 0.16; // Reserve right titleblock strip
                    minY = tbb.Min.Y + h * 0.06;
                    maxY = tbb.Max.Y - h * 0.05;
                }
            }

            int count = viewports.Count;
            int cols = count <= 2 ? count : (count <= 4 ? 2 : (count <= 6 ? 3 : 4));
            int rows = (int)Math.Ceiling((double)count / cols);

            double cellW = (maxX - minX) / cols;
            double cellH = (maxY - minY) / rows;

            for (int i = 0; i < count; i++)
            {
                int r = i / cols;
                int c = i % cols;

                double cx = minX + (c + 0.5) * cellW;
                // Top row first (rows - 1 - r)
                double cy = minY + (rows - 1 - r + 0.5) * cellH;

                try
                {
                    viewports[i].SetBoxCenter(new XYZ(cx, cy, 0));
                }
                catch { }
            }

            return viewports.Count;
        }
    }
}
