using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Models;
using ORTools.UI;

namespace ORTools.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class OrGridCommand : IExternalCommand
    {
        private Dictionary<string, Grid> existingGridsMap = new Dictionary<string, Grid>(StringComparer.OrdinalIgnoreCase);
        private Document _doc = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            _doc = doc;

            View activeView = doc.ActiveView;
            if (!(activeView is ViewPlan))
            {
                TaskDialog.Show("OR GRID", "Please open a Floor Plan or Ceiling Plan view to create grids.");
                return Result.Cancelled;
            }

            // Populate existing grids map
            existingGridsMap.Clear();
            var allGrids = new FilteredElementCollector(doc)
                .OfClass(typeof(Grid))
                .Cast<Grid>();
            foreach (var g in allGrids)
            {
                if (!string.IsNullOrEmpty(g.Name) && !existingGridsMap.ContainsKey(g.Name))
                {
                    existingGridsMap[g.Name] = g;
                }
            }

            OrGridWindow window = new OrGridWindow(doc);
            if (window.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            XYZ basePoint = null;
            try
            {
                basePoint = uidoc.Selection.PickPoint("OR GRID: Pick base point in plan view (or press Esc to cancel)");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception)
            {
                return Result.Cancelled;
            }

            if (basePoint == null) return Result.Cancelled;

            using (Transaction tx = new Transaction(doc, "Create OR Grids"))
            {
                tx.Start();
                try
                {
                    double ext = window.ExtensionMm / 304.8; // mm to feet
                    double viewZ = basePoint.Z;

                    // 1. Horizontal Grids (stacked upward along Y)
                    double currentH = 0.0;
                    foreach (var hItem in window.HorizontalRows)
                    {
                        currentH += hItem.FromPreviousMm / 304.8;
                        XYZ p1 = new XYZ(basePoint.X - ext, basePoint.Y + currentH, viewZ);
                        XYZ p2 = new XYZ(basePoint.X + window.TotalWidthHorizontalMm / 304.8 + ext, basePoint.Y + currentH, viewZ);
                        
                        Line line = Line.CreateBound(p1, p2);
                        ProcessGrid(hItem.Name, line);
                    }

                    // 2. Vertical Grids (stacked rightward along X)
                    double currentV = 0.0;
                    foreach (var vItem in window.VerticalRows)
                    {
                        currentV += vItem.FromPreviousMm / 304.8;
                        XYZ p1 = new XYZ(basePoint.X + currentV, basePoint.Y - ext, viewZ);
                        XYZ p2 = new XYZ(basePoint.X + currentV, basePoint.Y + window.TotalHeightVerticalMm / 304.8 + ext, viewZ);
                        
                        Line line = Line.CreateBound(p1, p2);
                        ProcessGrid(vItem.Name, line);
                    }

                    // 3. Special Grids
                    if (window.IsSpecialEnabled)
                    {
                        XYZ specialBase = basePoint;
                        double currentS = 0.0;
                        double lengthFt = window.SpecialLengthMm / 304.8;
                        double rotRad = window.SpecialRotationDeg * Math.PI / 180.0;

                        foreach (var sItem in window.SpecialRows)
                        {
                            currentS += sItem.FromPreviousMm / 304.8;
                            if (window.IsLinearArray)
                            {
                                XYZ offsetDir = new XYZ(Math.Cos(rotRad + Math.PI/2), Math.Sin(rotRad + Math.PI/2), 0).Normalize();
                                XYZ startOffset = specialBase + offsetDir * currentS;
                                XYZ dir = new XYZ(Math.Cos(rotRad), Math.Sin(rotRad), 0).Normalize();
                                XYZ p1 = startOffset;
                                XYZ p2 = startOffset + dir * lengthFt;
                                Line line = Line.CreateBound(p1, p2);
                                ProcessGrid(sItem.Name, line);
                            }
                            else // Radial Array
                            {
                                double angleDeg = window.SpecialRotationDeg + currentS * (180.0 / Math.PI);
                                double rad = angleDeg * Math.PI / 180.0;
                                XYZ dir = new XYZ(Math.Cos(rad), Math.Sin(rad), 0).Normalize();
                                XYZ p1 = specialBase;
                                XYZ p2 = specialBase + dir * lengthFt;
                                Line line = Line.CreateBound(p1, p2);
                                ProcessGrid(sItem.Name, line);
                            }
                        }
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
                    message = ex.Message;
                    return Result.Failed;
                }
            }

            TaskDialog.Show("OR GRID", "Successfully generated all parametric grids with clean names!");
            return Result.Succeeded;
        }

        private void ProcessGrid(string desiredName, Line line)
        {
            string cleanName = string.IsNullOrWhiteSpace(desiredName) ? "G" : desiredName.Trim();

            if (existingGridsMap.ContainsKey(cleanName))
            {
                try
                {
                    var oldG = existingGridsMap[cleanName];
                    if (oldG != null && oldG.IsValidObject)
                    {
                        _doc.Delete(oldG.Id);
                    }
                }
                catch { }
                existingGridsMap.Remove(cleanName);
            }

            Grid targetGrid = Grid.Create(_doc, line);
            try
            {
                targetGrid.Name = cleanName;
                existingGridsMap[cleanName] = targetGrid;
            }
            catch { }
        }
    }
}
