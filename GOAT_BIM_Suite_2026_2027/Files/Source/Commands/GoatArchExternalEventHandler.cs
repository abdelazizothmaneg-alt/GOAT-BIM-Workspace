using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ORTools.UI;

namespace ORTools.Commands
{
    public enum GoatArchAction
    {
        None,
        CreateArchInWall,
        CreateParametricWardrobe
    }

    public class GoatArchExternalEventHandler : IExternalEventHandler
    {
        public GoatArchWindow Window { get; set; }
        public GoatArchAction CurrentAction { get; set; } = GoatArchAction.None;

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
                    case GoatArchAction.CreateArchInWall:
                        ExecuteCreateArch(uidoc, doc);
                        break;

                    case GoatArchAction.CreateParametricWardrobe:
                        ExecuteCreateWardrobe(uidoc, doc);
                        break;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                Window.UpdateStatus("Placement cancelled by user.", false);
            }
            catch (Exception ex)
            {
                Window.UpdateStatus($"Error: {ex.Message}", true);
            }
            finally
            {
                CurrentAction = GoatArchAction.None;
            }
        }

        public string GetName() => "GOAT ARCH & JOINERY External Event Handler";

        private void ExecuteCreateArch(UIDocument uidoc, Document doc)
        {
            var cfg = Window.CollectArchParameters();
            Wall targetWall = null;
            XYZ centerPt = XYZ.Zero;
            XYZ wallDir = XYZ.BasisX;
            XYZ wallNormal = XYZ.BasisY;
            double wallThicknessFeet = UnitUtils.ConvertToInternalUnits(cfg.DepthMm, UnitTypeId.Millimeters);

            // Check if a wall is already selected, otherwise prompt user to pick a wall or point
            var selIds = uidoc.Selection.GetElementIds();
            foreach (ElementId id in selIds)
            {
                if (doc.GetElement(id) is Wall w)
                {
                    targetWall = w;
                    break;
                }
            }

            if (targetWall == null)
            {
                Window.UpdateStatus("Select a host wall or pick a point in the active view...", false);
                try
                {
                    Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Select Host Wall for GOAT ARCH (or ESC to place at origin)");
                    targetWall = doc.GetElement(pickedRef.ElementId) as Wall;
                    centerPt = pickedRef.GlobalPoint ?? XYZ.Zero;
                }
                catch
                {
                    // Fallback: pick point
                    centerPt = uidoc.Selection.PickPoint("Pick insertion point for GOAT ARCH");
                }
            }

            if (targetWall != null && targetWall.Location is LocationCurve lc && lc.Curve is Line wallLine)
            {
                wallDir = (wallLine.GetEndPoint(1) - wallLine.GetEndPoint(0)).Normalize();
                wallNormal = new XYZ(-wallDir.Y, wallDir.X, 0).Normalize();
                wallThicknessFeet = targetWall.Width;

                if (centerPt.IsAlmostEqualTo(XYZ.Zero))
                {
                    centerPt = (wallLine.GetEndPoint(0) + wallLine.GetEndPoint(1)) * 0.5;
                }
                else
                {
                    // Project picked point onto wall centerline at base elevation
                    XYZ start = wallLine.GetEndPoint(0);
                    double proj = (centerPt - start).DotProduct(wallDir);
                    centerPt = new XYZ(start.X + wallDir.X * proj, start.Y + wallDir.Y * proj, start.Z);
                }
            }

            double spanFeet = UnitUtils.ConvertToInternalUnits(cfg.SpanMm, UnitTypeId.Millimeters);
            double springHeightFeet = UnitUtils.ConvertToInternalUnits(cfg.SpringingHeightMm, UnitTypeId.Millimeters);
            double riseFeet = UnitUtils.ConvertToInternalUnits(cfg.RiseMm, UnitTypeId.Millimeters);
            double ringThkFeet = UnitUtils.ConvertToInternalUnits(cfg.RingThicknessMm, UnitTypeId.Millimeters);

            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ARCH — Generate Architectural Arch"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Create Arch Solid & Cut Opening"))
                {
                    trans.Start();

                    // 1. Cut opening in host wall if requested
                    if (cfg.CutHostWall && targetWall != null)
                    {
                        try
                        {
                            XYZ p1 = centerPt - wallDir * (spanFeet * 0.5);
                            XYZ p2 = centerPt + wallDir * (spanFeet * 0.5) + XYZ.BasisZ * (springHeightFeet + riseFeet);
                            doc.Create.NewOpening(targetWall, p1, p2);
                        }
                        catch { }
                    }

                    // 2. Build 3D Extrusion for the Arch Ring + Spandrel infill
                    List<XYZ> innerPts2D = GoatArchWindow.SampleArchCurvePoints(cfg.ArchTypeIndex, spanFeet, springHeightFeet, riseFeet, 24);
                    List<XYZ> outerPts2D = GoatArchWindow.SampleArchCurvePoints(cfg.ArchTypeIndex, spanFeet + ringThkFeet * 2.0, springHeightFeet, riseFeet + ringThkFeet, 24);

                    var loopPts = new List<XYZ>();
                    foreach (var p in innerPts2D)
                    {
                        loopPts.Add(centerPt - wallNormal * (wallThicknessFeet * 0.5) + wallDir * p.X + XYZ.BasisZ * p.Y);
                    }
                    for (int i = outerPts2D.Count - 1; i >= 0; i--)
                    {
                        var p = outerPts2D[i];
                        loopPts.Add(centerPt - wallNormal * (wallThicknessFeet * 0.5) + wallDir * p.X + XYZ.BasisZ * p.Y);
                    }

                    CurveLoop archLoop = new CurveLoop();
                    for (int i = 0; i < loopPts.Count; i++)
                    {
                        XYZ a = loopPts[i];
                        XYZ b = loopPts[(i + 1) % loopPts.Count];
                        if (a.DistanceTo(b) > 0.005)
                        {
                            archLoop.Append(Line.CreateBound(a, b));
                        }
                    }

                    Solid archSolid = GeometryCreationUtilities.CreateExtrusionGeometry(
                        new List<CurveLoop> { archLoop },
                        wallNormal,
                        wallThicknessFeet);

                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.ApplicationId = "GOATBIM_ArchGenerator";
                    ds.ApplicationDataId = Guid.NewGuid().ToString();
                    ds.SetShape(new List<GeometryObject> { archSolid });
                    ds.Name = $"GOAT Arch ({cfg.ArchTypeName} {cfg.SpanMm:0}x{cfg.SpringingHeightMm + cfg.RiseMm:0})";

                    Parameter cmt = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (cmt != null && !cmt.IsReadOnly)
                    {
                        cmt.Set($"GOAT_ARCH | {cfg.ArchTypeName} | Span={cfg.SpanMm:0}mm");
                    }

                    trans.Commit();
                }
                tg.Assimilate();
            }

            Window.UpdateStatus($"✔ Created {cfg.ArchTypeName} Arch ({cfg.SpanMm:0}mm span). (1x Ctrl+Z to Undo)", false);
        }

        private void ExecuteCreateWardrobe(UIDocument uidoc, Document doc)
        {
            var cfg = Window.CollectWardrobeParameters();

            XYZ basePt = XYZ.Zero;
            try
            {
                basePt = uidoc.Selection.PickPoint("Pick insertion point (bottom-left front corner) for GOAT Wardrobe / Joinery");
            }
            catch
            {
                basePt = XYZ.Zero;
            }

            double wFeet = UnitUtils.ConvertToInternalUnits(cfg.WidthMm, UnitTypeId.Millimeters);
            double hFeet = UnitUtils.ConvertToInternalUnits(cfg.HeightMm, UnitTypeId.Millimeters);
            double dFeet = UnitUtils.ConvertToInternalUnits(cfg.DepthMm, UnitTypeId.Millimeters);
            double plinthFeet = UnitUtils.ConvertToInternalUnits(cfg.PlinthMm, UnitTypeId.Millimeters);
            double panelFeet = UnitUtils.ConvertToInternalUnits(cfg.PanelThicknessMm, UnitTypeId.Millimeters);

            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ARCH — Generate Parametric Wardrobe"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Create Parametric Wardrobe Joinery"))
                {
                    trans.Start();

                    var solids = new List<GeometryObject>();

                    // Helper to create a box solid from local min (x0, y0, z0) to max (x1, y1, z1)
                    void AddBox(double x0, double y0, double z0, double x1, double y1, double z1)
                    {
                        if (x1 - x0 <= 0.005 || y1 - y0 <= 0.005 || z1 - z0 <= 0.005) return;
                        XYZ p0 = basePt + new XYZ(x0, y0, z0);
                        XYZ p1 = basePt + new XYZ(x1, y0, z0);
                        XYZ p2 = basePt + new XYZ(x1, y1, z0);
                        XYZ p3 = basePt + new XYZ(x0, y1, z0);

                        CurveLoop loop = new CurveLoop();
                        loop.Append(Line.CreateBound(p0, p1));
                        loop.Append(Line.CreateBound(p1, p2));
                        loop.Append(Line.CreateBound(p2, p3));
                        loop.Append(Line.CreateBound(p3, p0));

                        Solid box = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { loop }, XYZ.BasisZ, z1 - z0);
                        if (box != null) solids.Add(box);
                    }

                    // 1. Plinth Base
                    if (plinthFeet > 0.01)
                    {
                        AddBox(0, 0.08, 0, wFeet, dFeet, plinthFeet);
                    }

                    // 2. Bottom, Top, Left, Right, Back panels
                    AddBox(0, 0, plinthFeet, wFeet, dFeet, plinthFeet + panelFeet); // Bottom
                    AddBox(0, 0, hFeet - panelFeet, wFeet, dFeet, hFeet);           // Top
                    AddBox(0, 0, plinthFeet + panelFeet, panelFeet, dFeet, hFeet - panelFeet); // Left
                    AddBox(wFeet - panelFeet, 0, plinthFeet + panelFeet, wFeet, dFeet, hFeet - panelFeet); // Right
                    AddBox(panelFeet, dFeet - panelFeet, plinthFeet + panelFeet, wFeet - panelFeet, dFeet, hFeet - panelFeet); // Back

                    // 3. Vertical Bay Dividers & Shelves
                    int bays = Math.Max(1, cfg.BayCount);
                    double innerW = wFeet - 2 * panelFeet;
                    double bayW = innerW / bays;
                    double innerStartZ = plinthFeet + panelFeet;
                    double innerEndZ = hFeet - panelFeet;
                    double innerH = innerEndZ - innerStartZ;

                    for (int b = 1; b < bays; b++)
                    {
                        double divX = panelFeet + b * bayW - panelFeet * 0.5;
                        AddBox(divX, 0, innerStartZ, divX + panelFeet, dFeet - panelFeet, innerEndZ);
                    }

                    // 4. Shelves per Bay
                    int shelves = Math.Max(0, cfg.ShelvesPerBay);
                    for (int s = 1; s <= shelves; s++)
                    {
                        double shelfZ = innerStartZ + (innerH / (shelves + 1)) * s;
                        AddBox(panelFeet, 0.05, shelfZ, wFeet - panelFeet, dFeet - panelFeet, shelfZ + panelFeet);
                    }

                    // 5. Optional Doors (Closed / Semi-open front panels)
                    if (cfg.DoorStyleIndex == 0 || cfg.DoorStyleIndex == 1)
                    {
                        int doorPanels = Math.Max(2, bays * 2);
                        double doorW = wFeet / doorPanels;
                        double gap = UnitUtils.ConvertToInternalUnits(3.0, UnitTypeId.Millimeters);
                        for (int d = 0; d < doorPanels; d++)
                        {
                            double dx0 = d * doorW + gap;
                            double dx1 = (d + 1) * doorW - gap;
                            AddBox(dx0, -panelFeet, plinthFeet, dx1, 0, hFeet);
                        }
                    }

                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Casework));
                    ds.ApplicationId = "GOATBIM_WardrobeBuilder";
                    ds.ApplicationDataId = Guid.NewGuid().ToString();
                    ds.SetShape(solids);
                    ds.Name = $"GOAT Wardrobe ({cfg.WidthMm:0}x{cfg.HeightMm:0}x{cfg.DepthMm:0}mm - {bays} Bays)";

                    Parameter cmt = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (cmt != null && !cmt.IsReadOnly)
                    {
                        cmt.Set($"GOAT_JOINERY | {cfg.WidthMm:0}x{cfg.HeightMm:0}x{cfg.DepthMm:0}mm | {bays} Bays");
                    }

                    trans.Commit();
                }
                tg.Assimilate();
            }

            Window.UpdateStatus($"✔ Created Parametric Wardrobe ({cfg.WidthMm:0}×{cfg.HeightMm:0}×{cfg.DepthMm:0}mm, {cfg.BayCount} Bays). (1x Ctrl+Z to Undo)", false);
        }
    }
}
