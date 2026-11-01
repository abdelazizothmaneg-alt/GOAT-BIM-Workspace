using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Models;
using ORTools.UI;

namespace ORTools.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class OrLevelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                if (uiDoc == null || uiDoc.Document == null)
                {
                    TaskDialog.Show("OR LEVEL", "Please open a valid Revit project before running OR LEVEL.");
                    return Result.Cancelled;
                }

                Document doc = uiDoc.Document;

                OrLevelWindow window = new OrLevelWindow(doc);
                WindowInteropHelper helper = new WindowInteropHelper(window)
                {
                    Owner = commandData.Application.MainWindowHandle
                };

                bool? dialogResult = window.ShowDialog();
                return dialogResult == true ? Result.Succeeded : Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("OR LEVEL - Error", ex.ToString());
                return Result.Failed;
            }
        }

        public static (int CreatedLevels, int UpdatedLevels, int CreatedFloorPlans) ExecuteCreateOrUpdateLevelsFromParameters(
            Document doc,
            IEnumerable<LevelParameterModel> levelParameters,
            ElementId selectedLevelTypeId,
            bool globalAddToFloorPlans)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (levelParameters == null) throw new ArgumentNullException(nameof(levelParameters));

            Dictionary<string, Level> existingLevelsMap = new Dictionary<string, Level>(StringComparer.OrdinalIgnoreCase);
            Level sampleExistingLevel = null;

            var allProjectLevels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            foreach (var lvl in allProjectLevels)
            {
                if (!existingLevelsMap.ContainsKey(lvl.Name))
                {
                    existingLevelsMap[lvl.Name] = lvl;
                }
                if (sampleExistingLevel == null)
                {
                    sampleExistingLevel = lvl;
                }
            }

            var elevationViews = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && (v.ViewType == ViewType.Elevation || v.ViewType == ViewType.Section))
                .ToList();

            ViewFamilyType floorPlanViewType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

            int createdCount = 0;
            int updatedCount = 0;
            int plansCount = 0;

            using (Transaction trans = new Transaction(doc, "OR LEVEL - Batch Create or Update Levels"))
            {
                trans.Start();
                try
                {
                    foreach (LevelParameterModel param in levelParameters)
                    {
                        if (!param.CreateLevelParameter)
                            continue;

                        string requestedName = (param.LevelNameParameter ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(requestedName))
                            continue;

                        double elevationInInternalFeet = UnitUtils.ConvertToInternalUnits(
                            param.ElevationParameterMeters,
                            UnitTypeId.Meters
                        );

                        Level targetLevel = null;

                        if (existingLevelsMap.ContainsKey(requestedName))
                        {
                            targetLevel = existingLevelsMap[requestedName];
                            targetLevel.Elevation = elevationInInternalFeet;
                            updatedCount++;
                        }
                        else
                        {
                            targetLevel = Level.Create(doc, elevationInInternalFeet);

                            Parameter nameParam = targetLevel.get_Parameter(BuiltInParameter.DATUM_TEXT);
                            if (nameParam != null && !nameParam.IsReadOnly)
                            {
                                nameParam.Set(requestedName);
                            }
                            else
                            {
                                targetLevel.Name = requestedName;
                            }

                            existingLevelsMap[requestedName] = targetLevel;
                            createdCount++;
                        }

                        if (selectedLevelTypeId != null && selectedLevelTypeId != ElementId.InvalidElementId)
                        {
                            try
                            {
                                targetLevel.ChangeTypeId(selectedLevelTypeId);
                            }
                            catch
                            {
                            }
                        }

                        // Align Datum Extents using DatumPlane.SetCurveInView (singular)
                        if (sampleExistingLevel != null && sampleExistingLevel.Id != targetLevel.Id && elevationViews.Count > 0)
                        {
                            foreach (View elevView in elevationViews)
                            {
                                try
                                {
                                    IList<Curve> sampleCurves = sampleExistingLevel.GetCurvesInView(DatumExtentType.Model, elevView);
                                    if (sampleCurves != null && sampleCurves.Count > 0)
                                    {
                                        Curve sampleCurve = sampleCurves[0];
                                        XYZ p1 = sampleCurve.GetEndPoint(0);
                                        XYZ p2 = sampleCurve.GetEndPoint(1);
                                        double targetZ = targetLevel.Elevation;

                                        XYZ newP1 = new XYZ(p1.X, p1.Y, targetZ);
                                        XYZ newP2 = new XYZ(p2.X, p2.Y, targetZ);

                                        Line alignedLine = Line.CreateBound(newP1, newP2);
                                        targetLevel.SetCurveInView(DatumExtentType.Model, elevView, alignedLine);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }

                        bool shouldCreateFloorPlan = globalAddToFloorPlans && param.AddToFloorPlansParameter;
                        if (shouldCreateFloorPlan && floorPlanViewType != null)
                        {
                            bool viewExists = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewPlan))
                                .Cast<ViewPlan>()
                                .Any(vp => vp.GenLevel != null && vp.GenLevel.Id == targetLevel.Id && vp.ViewType == ViewType.FloorPlan);

                            if (!viewExists)
                            {
                                ViewPlan floorPlanView = ViewPlan.Create(doc, floorPlanViewType.Id, targetLevel.Id);
                                try
                                {
                                    floorPlanView.Name = requestedName;
                                }
                                catch
                                {
                                }
                                plansCount++;
                            }
                        }
                    }

                    trans.Commit();
                }
                catch
                {
                    if (trans.HasStarted())
                        trans.RollBack();
                    throw;
                }
            }

            return (createdCount, updatedCount, plansCount);
        }
    }
}
