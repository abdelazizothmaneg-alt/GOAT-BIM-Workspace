using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Models;
using ORTools.ViewModels;

namespace ORTools.Commands
{
    public enum OpeningActionType
    {
        None,
        HarvestOpeningTypes,
        RunPreCheckReport,
        GenerateOpeningDetails
    }

    public class OrOpeningExternalEventHandler : IExternalEventHandler
    {
        public OrOpeningViewModel ViewModel { get; set; }
        public OpeningActionType CurrentAction { get; set; } = OpeningActionType.None;

        public void Execute(UIApplication uiapp)
        {
            if (ViewModel == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null) return;
            Document doc = uidoc.Document;

            try
            {
                switch (CurrentAction)
                {
                    case OpeningActionType.HarvestOpeningTypes:
                        ExecuteHarvest(uidoc, doc);
                        break;
                    case OpeningActionType.RunPreCheckReport:
                        ExecutePreCheck(doc);
                        break;
                    case OpeningActionType.GenerateOpeningDetails:
                        ExecuteGenerate(doc);
                        break;
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                CurrentAction = OpeningActionType.None;
            }
        }

        private void PopulateProjectMetadata(Document doc)
        {
            // 1. ViewFamilyTypes
            var vfts = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .OrderBy(x => x.Name)
                .ToList();

            ViewModel.PlanViewTypes.Clear();
            ViewModel.SectionViewTypes.Clear();
            ViewModel.ElevationViewTypes.Clear();
            ViewModel.IsoViewTypes.Clear();

            foreach (var vft in vfts)
            {
                var item = new ComboOptionItem { Id = vft.Id, Name = vft.Name };
                if (vft.ViewFamily == ViewFamily.FloorPlan || vft.ViewFamily == ViewFamily.StructuralPlan || vft.ViewFamily == ViewFamily.Section)
                    ViewModel.PlanViewTypes.Add(item);
                if (vft.ViewFamily == ViewFamily.Section || vft.ViewFamily == ViewFamily.Detail)
                    ViewModel.SectionViewTypes.Add(item);
                if (vft.ViewFamily == ViewFamily.Elevation || vft.ViewFamily == ViewFamily.Section)
                    ViewModel.ElevationViewTypes.Add(item);
                if (vft.ViewFamily == ViewFamily.ThreeDimensional)
                    ViewModel.IsoViewTypes.Add(item);
            }

            if (ViewModel.PlanViewTypes.Count > 0 && ViewModel.SelectedPlanViewType == null)
                ViewModel.SelectedPlanViewType = ViewModel.PlanViewTypes[0];
            if (ViewModel.SectionViewTypes.Count > 0 && ViewModel.SelectedSectionViewType == null)
                ViewModel.SelectedSectionViewType = ViewModel.SectionViewTypes[0];
            if (ViewModel.ElevationViewTypes.Count > 0 && ViewModel.SelectedElevationViewType == null)
                ViewModel.SelectedElevationViewType = ViewModel.ElevationViewTypes[0];
            if (ViewModel.IsoViewTypes.Count > 0 && ViewModel.SelectedIsoViewType == null)
                ViewModel.SelectedIsoViewType = ViewModel.IsoViewTypes[0];

            // 2. View Templates
            ViewModel.ViewTemplates.Clear();
            var noneTemplate = new ComboOptionItem { Id = ElementId.InvalidElementId, Name = "<None>" };
            ViewModel.ViewTemplates.Add(noneTemplate);

            var templates = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate)
                .OrderBy(v => v.Name)
                .ToList();

            foreach (var t in templates)
                ViewModel.ViewTemplates.Add(new ComboOptionItem { Id = t.Id, Name = t.Name });

            if (ViewModel.SelectedPlanTemplate == null) ViewModel.SelectedPlanTemplate = noneTemplate;
            if (ViewModel.SelectedSectionTemplate == null) ViewModel.SelectedSectionTemplate = noneTemplate;
            if (ViewModel.SelectedElevationTemplate == null) ViewModel.SelectedElevationTemplate = noneTemplate;
            if (ViewModel.SelectedIsoTemplate == null) ViewModel.SelectedIsoTemplate = noneTemplate;

            // 3. Title Blocks
            ViewModel.TitleBlocks.Clear();
            var tbs = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .OrderBy(x => x.FamilyName)
                .ThenBy(x => x.Name)
                .ToList();

            foreach (var tb in tbs)
                ViewModel.TitleBlocks.Add(new ComboOptionItem { Id = tb.Id, Name = $"{tb.FamilyName} : {tb.Name}" });

            if (ViewModel.TitleBlocks.Count > 0 && ViewModel.SelectedTitleBlock == null)
                ViewModel.SelectedTitleBlock = ViewModel.TitleBlocks[0];

            // 4. Dimension Types
            ViewModel.DimensionTypes.Clear();
            var defaultDim = new ComboOptionItem { Id = ElementId.InvalidElementId, Name = "<Default: Linear>" };
            ViewModel.DimensionTypes.Add(defaultDim);

            var dimTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .Where(d => d.StyleType == DimensionStyleType.Linear || d.StyleType == DimensionStyleType.LinearFixed)
                .OrderBy(d => d.Name)
                .ToList();

            foreach (var dt in dimTypes)
                ViewModel.DimensionTypes.Add(new ComboOptionItem { Id = dt.Id, Name = dt.Name });

            if (ViewModel.SelectedPlanDimType == null) ViewModel.SelectedPlanDimType = defaultDim;
            if (ViewModel.SelectedSectionDimType == null) ViewModel.SelectedSectionDimType = defaultDim;
            if (ViewModel.SelectedElevationDimType == null) ViewModel.SelectedElevationDimType = defaultDim;
        }

        private string DetectFrameAndLeafSubcategories(Document doc, FamilySymbol sym, FamilyInstance sampleInst)
        {
            try
            {
                var frameWords = (ViewModel.FrameSubcategoryKeywords ?? "Frame, Mullion, Jamb")
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLowerInvariant()).Where(s => s.Length > 0).ToList();

                var leafWords = (ViewModel.LeafSubcategoryKeywords ?? "Panel, Leaf, Glass, Sash")
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLowerInvariant()).Where(s => s.Length > 0).ToList();

                bool foundFrame = false;
                bool foundLeaf = false;

                if (sym.Category != null && sym.Category.SubCategories != null)
                {
                    foreach (Category subCat in sym.Category.SubCategories)
                    {
                        string n = (subCat.Name ?? "").ToLowerInvariant();
                        if (frameWords.Any(w => n.Contains(w))) foundFrame = true;
                        if (leafWords.Any(w => n.Contains(w))) foundLeaf = true;
                    }
                }

                if (foundFrame && foundLeaf) return "✓ frame + leaf";
                if (foundFrame) return "✓ frame only";
                if (foundLeaf) return "✓ leaf/panel only";
                return "⚠ no frame/leaf sub...";
            }
            catch
            {
                return "✓ frame + leaf";
            }
        }

        private void ExecuteHarvest(UIDocument uidoc, Document doc)
        {
            ViewModel.ProgressValue = 0;
            PopulateProjectMetadata(doc);

            BuiltInCategory bic = ViewModel.IsDoorMode ? BuiltInCategory.OST_Doors : BuiltInCategory.OST_Windows;

            var selIds = uidoc.Selection.GetElementIds();
            var selectedInstances = selIds.Select(id => doc.GetElement(id))
                .OfType<FamilyInstance>()
                .Where(fi => fi.Category != null && fi.Category.Id.Value == (long)bic)
                .ToList();

            int selTypeCount = selectedInstances.Select(x => x.GetTypeId()).Distinct().Count();

            List<FamilyInstance> instances;
            if (ViewModel.FromSelection && selectedInstances.Count > 0)
            {
                instances = selectedInstances;
            }
            else
            {
                instances = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .ToList();
            }

            var typeMap = new Dictionary<ElementId, List<FamilyInstance>>();
            foreach (var inst in instances)
            {
                ElementId tid = inst.GetTypeId();
                if (tid == null || tid == ElementId.InvalidElementId) continue;
                if (!typeMap.TryGetValue(tid, out var list))
                {
                    list = new List<FamilyInstance>();
                    typeMap[tid] = list;
                }
                list.Add(inst);
            }

            var typeItems = new List<OpeningTypeItem>();
            int autoNum = 1;

            foreach (var kvp in typeMap)
            {
                FamilySymbol sym = doc.GetElement(kvp.Key) as FamilySymbol;
                if (sym == null) continue;

                var instList = kvp.Value;
                string fullName = $"{sym.FamilyName} : {sym.Name}";

                // Read mark according to section 2 settings
                string mark = "";
                if (ViewModel.ReadMarkFromSource == "Mark" && instList.Count > 0)
                {
                    Parameter pInstMark = instList[0].get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                    if (pInstMark != null && pInstMark.HasValue) mark = pInstMark.AsString() ?? "";
                }
                else if (ViewModel.ReadMarkFromSource == "Custom Parameter" && !string.IsNullOrWhiteSpace(ViewModel.CustomMarkParameterName))
                {
                    Parameter pCust = sym.LookupParameter(ViewModel.CustomMarkParameterName)
                                   ?? (instList.Count > 0 ? instList[0].LookupParameter(ViewModel.CustomMarkParameterName) : null);
                    if (pCust != null && pCust.HasValue) mark = pCust.AsString() ?? pCust.AsValueString() ?? "";
                }
                else
                {
                    Parameter pMark = sym.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK) ?? sym.LookupParameter("Type Mark");
                    if (pMark != null && pMark.HasValue) mark = pMark.AsString() ?? pMark.AsValueString() ?? "";
                }

                bool isAuto = false;
                if (string.IsNullOrWhiteSpace(mark) && ViewModel.AutoNumberEmptyMarks)
                {
                    int digits = Math.Max(1, Math.Min(6, ViewModel.AutoDigits));
                    string prefix = ViewModel.AutoPrefix ?? (ViewModel.IsDoorMode ? "D" : "W");
                    mark = $"{prefix}{autoNum.ToString($"D{digits}")}";
                    autoNum++;
                    isAuto = true;
                }

                double wFeet = GetWidthFeet(sym);
                double hFeet = GetHeightFeet(sym);
                string dims = $"{wFeet * 304.8:0} x {hFeet * 304.8:0} mm";

                var item = new OpeningTypeItem
                {
                    TypeId = kvp.Key,
                    FamilyName = sym.FamilyName ?? "Family",
                    TypeName = sym.Name ?? "Type",
                    FamilyAndTypeName = fullName,
                    InstanceCount = instList.Count,
                    Mark = mark,
                    IsAutoNumbered = isAuto,
                    Dimensions = dims,
                    DimensionsDetectionStatus = DetectFrameAndLeafSubcategories(doc, sym, instList.FirstOrDefault()),
                    IsSelected = true
                };

                // Populate ReferenceInstances dropdown (LevelName · ElementId)
                foreach (var fi in instList)
                {
                    string lvlName = "00 - Ground Floor";
                    if (fi.LevelId != ElementId.InvalidElementId)
                    {
                        Level lvl = doc.GetElement(fi.LevelId) as Level;
                        if (lvl != null) lvlName = lvl.Name;
                    }
                    item.ReferenceInstances.Add(new ReferenceInstanceItem
                    {
                        InstanceId = fi.Id,
                        LevelName = lvlName,
                        DisplayText = $"{lvlName} · {fi.Id.Value}"
                    });
                }

                if (item.ReferenceInstances.Count > 0)
                    item.SelectedReferenceInstance = item.ReferenceInstances[0];

                typeItems.Add(item);
            }

            ViewModel.UpdateOpeningTypes(typeItems, selTypeCount);
            ViewModel.ProgressValue = 100;
            ViewModel.StatusMessage = $"Scanned {typeItems.Count} unique {(ViewModel.IsDoorMode ? "door" : "window")} types ({instances.Count} instances).";
        }

        private void ExecutePreCheck(Document doc)
        {
            int totalTypes = ViewModel.OpeningTypes.Count;
            int fullFrameLeaf = ViewModel.OpeningTypes.Count(x => x.DimensionsDetectionStatus.StartsWith("✓"));
            int missingSubcat = totalTypes - fullFrameLeaf;
            int autoNumbered = ViewModel.OpeningTypes.Count(x => x.IsAutoNumbered);

            string report = $"OR Tools — Family Frame/Leaf & Reference Health Report:\n\n" +
                            $"• Total Opening Types: {totalTypes}\n" +
                            $"• Types with Frame + Leaf Subcategories: {fullFrameLeaf}\n" +
                            $"• Types missing Frame/Leaf Subcategories: {missingSubcat}\n" +
                            $"• Auto-Numbered Empty Marks (Orange): {autoNumbered}\n\n" +
                            (missingSubcat == 0
                                ? "✓ All opening families contain clean Frame & Leaf subcategories!"
                                : "ℹ Note: Types flagged with '⚠ no frame/leaf sub...' will use overall bounding box and parameter fallback labels.");

            TaskDialog.Show("OR Tools — Pre-Check Report", report);
            ViewModel.StatusMessage = "Frame/Leaf Pre-Check report completed.";
        }

        private void ComputeVerticalExtents(Document doc, FamilyInstance inst, Element hostWall, XYZ center, double hFeet, double cropOffsetFeet, out double minZRel, out double maxZRel)
        {
            double bottomOffsetFeet = ViewModel.BottomOffsetMm / 304.8;
            double topOffsetFeet = ViewModel.TopOffsetMm / 304.8;

            minZRel = -hFeet * 0.5 - cropOffsetFeet;
            maxZRel = hFeet * 0.5 + cropOffsetFeet;

            // Default fallback to Host Wall vertical bounds if available
            if (hostWall != null)
            {
                BoundingBoxXYZ wallBb = hostWall.get_BoundingBox(null);
                if (wallBb != null)
                {
                    minZRel = (wallBb.Min.Z - center.Z) + bottomOffsetFeet;
                    maxZRel = (wallBb.Max.Z - center.Z) + topOffsetFeet;
                }
            }

            // If user selected Structural Slab Below / Above (Index 0)
            if (ViewModel.BottomExtentModeIndex == 0 || ViewModel.TopExtentModeIndex == 0)
            {
                var floors = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .WhereElementIsNotElementType()
                    .ToList();

                double? bestSlabBelowBottomZ = null;
                double? bestSlabAboveTopZ = null;

                foreach (var fl in floors)
                {
                    if (ViewModel.SlabFilterModeIndex == 0)
                    {
                        Parameter pStruct = fl.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
                        if (pStruct != null && pStruct.HasValue && pStruct.AsInteger() != 1) continue;
                    }
                    else if (ViewModel.SlabFilterModeIndex == 2 && !string.IsNullOrWhiteSpace(ViewModel.SlabFilterText))
                    {
                        Element flType = doc.GetElement(fl.GetTypeId());
                        string tName = flType != null ? flType.Name : "";
                        if (tName.IndexOf(ViewModel.SlabFilterText, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }

                    BoundingBoxXYZ fBb = fl.get_BoundingBox(null);
                    if (fBb == null) continue;

                    // Check XY overlap with opening center (within 6 feet)
                    if (center.X < fBb.Min.X - 6.0 || center.X > fBb.Max.X + 6.0 ||
                        center.Y < fBb.Min.Y - 6.0 || center.Y > fBb.Max.Y + 6.0)
                        continue;

                    if (fBb.Max.Z <= center.Z)
                    {
                        if (bestSlabBelowBottomZ == null || fBb.Min.Z > bestSlabBelowBottomZ.Value)
                            bestSlabBelowBottomZ = fBb.Min.Z;
                    }
                    else if (fBb.Min.Z >= center.Z)
                    {
                        if (bestSlabAboveTopZ == null || fBb.Max.Z < bestSlabAboveTopZ.Value)
                            bestSlabAboveTopZ = fBb.Max.Z;
                    }
                }

                if (ViewModel.BottomExtentModeIndex == 0 && bestSlabBelowBottomZ.HasValue)
                    minZRel = (bestSlabBelowBottomZ.Value - center.Z) + bottomOffsetFeet;
                else if (ViewModel.BottomExtentModeIndex == 2)
                    minZRel = -hFeet * 0.5 + bottomOffsetFeet;

                if (ViewModel.TopExtentModeIndex == 0 && bestSlabAboveTopZ.HasValue)
                    maxZRel = (bestSlabAboveTopZ.Value - center.Z) + topOffsetFeet;
                else if (ViewModel.TopExtentModeIndex == 2)
                    maxZRel = hFeet * 0.5 + topOffsetFeet;
            }
        }

        private void GetSheetQuadrantCenters(Document doc, ViewSheet sheet, out XYZ pt1, out XYZ pt2, out XYZ pt3, out XYZ pt4)
        {
            double minX = 0.0, minY = 0.0, maxX = 2.75, maxY = 1.95;

            if (sheet != null)
            {
                Element tb = new FilteredElementCollector(doc, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsNotElementType()
                    .FirstOrDefault();

                if (tb != null)
                {
                    BoundingBoxXYZ bb = tb.get_BoundingBox(sheet) ?? tb.get_BoundingBox(null);
                    if (bb != null && (bb.Max.X - bb.Min.X) > 0.2 && (bb.Max.Y - bb.Min.Y) > 0.2)
                    {
                        minX = bb.Min.X; minY = bb.Min.Y; maxX = bb.Max.X; maxY = bb.Max.Y;
                    }
                    else
                    {
                        Parameter pW = tb.get_Parameter(BuiltInParameter.SHEET_WIDTH);
                        Parameter pH = tb.get_Parameter(BuiltInParameter.SHEET_HEIGHT);
                        LocationPoint lp = tb.Location as LocationPoint;
                        XYZ origin = lp != null ? lp.Point : XYZ.Zero;
                        if (pW != null && pH != null && pW.AsDouble() > 0.2 && pH.AsDouble() > 0.2)
                        {
                            minX = origin.X; minY = origin.Y;
                            maxX = origin.X + pW.AsDouble(); maxY = origin.Y + pH.AsDouble();
                        }
                    }
                }
            }

            double totalW = maxX - minX;
            double totalH = maxY - minY;

            double usableLeft   = minX + totalW * 0.05;
            double usableRight  = minX + totalW * 0.82;
            double usableBottom = minY + totalH * 0.08;
            double usableTop    = minY + totalH * 0.92;

            double uW = usableRight - usableLeft;
            double uH = usableTop - usableBottom;

            if (ViewModel.SheetLayoutModeIndex == 2) // Single horizontal row
            {
                double midY = usableBottom + uH * 0.50;
                pt1 = new XYZ(usableLeft + uW * 0.14, midY, 0);
                pt2 = new XYZ(usableLeft + uW * 0.38, midY, 0);
                pt3 = new XYZ(usableLeft + uW * 0.62, midY, 0);
                pt4 = new XYZ(usableLeft + uW * 0.86, midY, 0);
            }
            else if (ViewModel.SheetLayoutModeIndex == 1) // Elevation + Section top · Plan + 3D bottom
            {
                pt1 = new XYZ(usableLeft + uW * 0.26, usableBottom + uH * 0.28, 0); // Plan bottom-left
                pt2 = new XYZ(usableLeft + uW * 0.26, usableBottom + uH * 0.72, 0); // Elev top-left
                pt3 = new XYZ(usableLeft + uW * 0.74, usableBottom + uH * 0.72, 0); // Sec top-right
                pt4 = new XYZ(usableLeft + uW * 0.74, usableBottom + uH * 0.28, 0); // 3D bottom-right
            }
            else // Default Auto: Plan + Elevation top · Section + 3D bottom
            {
                pt1 = new XYZ(usableLeft + uW * 0.26, usableBottom + uH * 0.72, 0);
                pt2 = new XYZ(usableLeft + uW * 0.74, usableBottom + uH * 0.72, 0);
                pt3 = new XYZ(usableLeft + uW * 0.26, usableBottom + uH * 0.28, 0);
                pt4 = new XYZ(usableLeft + uW * 0.74, usableBottom + uH * 0.28, 0);
            }
        }

        private void PlaceAndCenterViewport(Document doc, ViewSheet sheet, View view, XYZ targetCenter)
        {
            if (sheet == null || view == null) return;
            if (!Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id)) return;

            Viewport vp = Viewport.Create(doc, sheet.Id, view.Id, targetCenter);
            if (vp != null)
            {
                doc.Regenerate();
                try { vp.SetBoxCenter(targetCenter); } catch { }
            }
        }

        private void ApplyViewSettingsAndClean(Document doc, View view, int scale, ComboOptionItem templateOpt, bool is3D, Element openingInst, Element hostWall)
        {
            if (view == null) return;
            try { view.Scale = Math.Max(1, scale); } catch { }
            try { view.DetailLevel = ViewDetailLevel.Fine; } catch { }

            if (templateOpt != null && templateOpt.Id != ElementId.InvalidElementId)
            {
                try { view.ViewTemplateId = templateOpt.Id; } catch { }
            }

            try
            {
                view.CropBoxActive = true;
                view.CropBoxVisible = false;
            }
            catch { }

            if (is3D)
            {
                foreach (BuiltInCategory cat in new[] {
                    BuiltInCategory.OST_SectionBox,
                    BuiltInCategory.OST_Levels,
                    BuiltInCategory.OST_Grids,
                    BuiltInCategory.OST_Cameras,
                    BuiltInCategory.OST_Viewers
                })
                {
                    try
                    {
                        ElementId catId = new ElementId(cat);
                        if (view.CanCategoryBeHidden(catId))
                            view.SetCategoryHidden(catId, true);
                    }
                    catch { }
                }

                // 3D isolate mode: 0 = Opening + host wall, 1 = Opening only, 2 = Full 3D section box context
                if (ViewModel.Isolate3DModeIndex == 0 || ViewModel.Isolate3DModeIndex == 1)
                {
                    try
                    {
                        var visibleElems = new FilteredElementCollector(doc, view.Id)
                            .WhereElementIsNotElementType()
                            .ToElementIds();

                        var toHide = new List<ElementId>();
                        foreach (ElementId eid in visibleElems)
                        {
                            if (openingInst != null && eid == openingInst.Id) continue;
                            if (ViewModel.Isolate3DModeIndex == 0 && hostWall != null && eid == hostWall.Id) continue;
                            Element el = doc.GetElement(eid);
                            if (el == null || el.Category == null) continue;
                            if (el.CanBeHidden(view))
                                toHide.Add(eid);
                        }
                        if (toHide.Count > 0) view.HideElements(toHide);
                    }
                    catch { }
                }
            }
            else
            {
                foreach (BuiltInCategory cat in new[] { BuiltInCategory.OST_Viewers, BuiltInCategory.OST_Cameras })
                {
                    try
                    {
                        ElementId catId = new ElementId(cat);
                        if (view.CanCategoryBeHidden(catId))
                            view.SetCategoryHidden(catId, true);
                    }
                    catch { }
                }
            }
        }

        private void PlaceAutomaticDimensions(Document doc, View view, FamilyInstance inst, FamilySymbol sym, XYZ center, XYZ viewRight, XYZ viewUp, double wFeet, double hFeet, bool isPlan, bool isSection, bool isElevation)
        {
            if (!ViewModel.EnableStandardDimensions || view == null || inst == null) return;
            try
            {
                // Place clean Linear Dimension or Reference-Plane Dimension on the opening
                var opt = new Options { ComputeReferences = true, IncludeNonVisibleObjects = true, View = view };
                GeometryElement geom = inst.get_Geometry(opt);
                if (geom == null) return;

                var leftRightRefs = new ReferenceArray();
                var topBotRefs = new ReferenceArray();

                // Collect valid family Reference Planes / Edges
                foreach (var r in inst.GetReferences(FamilyInstanceReferenceType.Left)) leftRightRefs.Append(r);
                foreach (var r in inst.GetReferences(FamilyInstanceReferenceType.Right)) leftRightRefs.Append(r);
                foreach (var r in inst.GetReferences(FamilyInstanceReferenceType.Bottom)) topBotRefs.Append(r);
                foreach (var r in inst.GetReferences(FamilyInstanceReferenceType.Top)) topBotRefs.Append(r);

                double paperOffsetFeet = (ViewModel.FirstStringSpacingMm / 304.8) * view.Scale;

                if ((isPlan || isElevation) && leftRightRefs.Size >= 2)
                {
                    XYZ dimPt = center + viewUp * (hFeet * 0.5 + paperOffsetFeet);
                    Line dimLine = Line.CreateBound(dimPt - viewRight * (wFeet * 0.5), dimPt + viewRight * (wFeet * 0.5));
                    doc.Create.NewDimension(view, dimLine, leftRightRefs);
                }

                if ((isSection || isElevation) && topBotRefs.Size >= 2)
                {
                    XYZ dimPt = center + viewRight * (wFeet * 0.5 + paperOffsetFeet);
                    Line dimLine = Line.CreateBound(dimPt - viewUp * (hFeet * 0.5), dimPt + viewUp * (hFeet * 0.5));
                    doc.Create.NewDimension(view, dimLine, topBotRefs);
                }
            }
            catch { }
        }

        private void ExecuteGenerate(Document doc)
        {
            var selected = ViewModel.OpeningTypes.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                TaskDialog.Show("OR Tools", "Please select at least one opening type.");
                return;
            }

            ElementId defaultSectionTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeSection);
            ElementId sectionTypeId = (ViewModel.SelectedSectionViewType != null && ViewModel.SelectedSectionViewType.Id != ElementId.InvalidElementId)
                ? ViewModel.SelectedSectionViewType.Id : defaultSectionTypeId;

            ElementId vft3DId = (ViewModel.SelectedIsoViewType != null && ViewModel.SelectedIsoViewType.Id != ElementId.InvalidElementId)
                ? ViewModel.SelectedIsoViewType.Id
                : new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                    .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional)?.Id ?? ElementId.InvalidElementId;

            ElementId titleBlockId = (ViewModel.SelectedTitleBlock != null && ViewModel.SelectedTitleBlock.Id != ElementId.InvalidElementId)
                ? ViewModel.SelectedTitleBlock.Id
                : new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType().FirstElementId();

            double cropOffsetFeet = Math.Max(0.2, ViewModel.CropOffsetMm / 304.8);
            double sectionCutOffsetFeet = ViewModel.SectionCutOffsetMm / 304.8;

            int done = 0;
            int createdSheets = 0;

            using (Transaction tx = new Transaction(doc, $"OR {(ViewModel.IsDoorMode ? "D VIEW" : "W VIEW")} Parametric 4-View Generator"))
            {
                tx.Start();

                foreach (var item in selected)
                {
                    FamilySymbol sym = doc.GetElement(item.TypeId) as FamilySymbol;
                    if (sym == null) continue;

                    // Pick the user's chosen Reference Instance (Level · ID) from Section 1
                    FamilyInstance inst = null;
                    if (item.SelectedReferenceInstance != null && item.SelectedReferenceInstance.InstanceId != ElementId.InvalidElementId)
                        inst = doc.GetElement(item.SelectedReferenceInstance.InstanceId) as FamilyInstance;

                    if (inst == null) continue;
                    Element hostWall = inst.Host;

                    // Section 2: Write new / edited marks back to opening types
                    if (ViewModel.WriteMarksBackToProject && !string.IsNullOrWhiteSpace(item.Mark))
                    {
                        Parameter pMark = sym.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK) ?? sym.LookupParameter("Type Mark");
                        if (pMark != null && !pMark.IsReadOnly)
                        {
                            try { pMark.Set(item.Mark); item.IsAutoNumbered = false; } catch { }
                        }
                    }

                    BoundingBoxXYZ instBb = inst.get_BoundingBox(null);
                    XYZ center;
                    if (ViewModel.OpeningCentreModeIndex == 1 && inst.Location is LocationPoint lp)
                        center = lp.Point;
                    else
                        center = instBb != null ? (instBb.Min + instBb.Max) * 0.5 : ((inst.Location as LocationPoint)?.Point ?? XYZ.Zero);

                    XYZ facing = inst.FacingOrientation.IsZeroLength() ? XYZ.BasisY : inst.FacingOrientation.Normalize();
                    XYZ hand = inst.HandOrientation.IsZeroLength() ? XYZ.BasisX : inst.HandOrientation.Normalize();

                    // Look from: 0 = Exterior side (facing), 1 = Interior side
                    if (ViewModel.LookFromIndex == 1)
                    {
                        facing = -facing;
                        hand = -hand;
                    }

                    double w = GetWidthFeet(sym);
                    double h = GetHeightFeet(sym);
                    double depth = 2.0;

                    ComputeVerticalExtents(doc, inst, hostWall, center, h, cropOffsetFeet, out double minZRel, out double maxZRel);

                    // 1. Create Sheet (Section 6)
                    ViewSheet sheet = null;
                    if (ViewModel.CreateSheets && titleBlockId != ElementId.InvalidElementId)
                    {
                        sheet = ViewSheet.Create(doc, titleBlockId);
                        string sNum = FormatTokenString(ViewModel.SheetNumberPattern, item, "");
                        string sName = FormatTokenString(ViewModel.SheetNamePattern, item, "");
                        SetUniqueSheetNumber(doc, sheet, sNum);
                        sheet.Name = sName;
                        createdSheets++;
                        doc.Regenerate();
                    }

                    GetSheetQuadrantCenters(doc, sheet, out XYZ ptPlan, out XYZ ptElev, out XYZ ptSec, out XYZ ptIso);

                    // 2. Plan View
                    if (ViewModel.IncludePlan && defaultSectionTypeId != ElementId.InvalidElementId)
                    {
                        Transform tPlan = Transform.Identity;
                        tPlan.Origin = center;
                        tPlan.BasisX = hand;
                        tPlan.BasisY = facing;
                        tPlan.BasisZ = -XYZ.BasisZ;

                        BoundingBoxXYZ boxPlan = new BoundingBoxXYZ { Transform = tPlan };
                        boxPlan.Min = new XYZ(-w * 0.5 - cropOffsetFeet, -depth, -h * 0.5);
                        boxPlan.Max = new XYZ(w * 0.5 + cropOffsetFeet, depth, h * 0.5);

                        ViewSection planSec = ViewSection.CreateSection(doc, defaultSectionTypeId, boxPlan);
                        ApplyViewSettingsAndClean(doc, planSec, ViewModel.PlanScale, ViewModel.SelectedPlanTemplate, is3D: false, openingInst: inst, hostWall: hostWall);
                        SetViewNameWithConflictRule(doc, planSec, FormatTokenString(ViewModel.NamingPattern, item, ViewModel.PlanSuffix));
                        PlaceAutomaticDimensions(doc, planSec, inst, sym, center, hand, facing, w, depth, isPlan: true, isSection: false, isElevation: false);

                        PlaceAndCenterViewport(doc, sheet, planSec, ptPlan);
                    }

                    // 3. Elevation View
                    if (ViewModel.IncludeElevation && defaultSectionTypeId != ElementId.InvalidElementId)
                    {
                        Transform tElev = Transform.Identity;
                        tElev.Origin = center;
                        tElev.BasisX = hand;
                        tElev.BasisY = XYZ.BasisZ;
                        tElev.BasisZ = -facing;

                        BoundingBoxXYZ boxElev = new BoundingBoxXYZ { Transform = tElev };
                        boxElev.Min = new XYZ(-w * 0.5 - cropOffsetFeet, minZRel, -depth);
                        boxElev.Max = new XYZ(w * 0.5 + cropOffsetFeet, maxZRel, depth);

                        ViewSection elevSec = ViewSection.CreateSection(doc, defaultSectionTypeId, boxElev);
                        ApplyViewSettingsAndClean(doc, elevSec, ViewModel.ElevationScale, ViewModel.SelectedElevationTemplate, is3D: false, openingInst: inst, hostWall: hostWall);
                        SetViewNameWithConflictRule(doc, elevSec, FormatTokenString(ViewModel.NamingPattern, item, ViewModel.ElevationSuffix));
                        PlaceAutomaticDimensions(doc, elevSec, inst, sym, center, hand, XYZ.BasisZ, w, h, isPlan: false, isSection: false, isElevation: true);

                        PlaceAndCenterViewport(doc, sheet, elevSec, ptElev);
                    }

                    // 4. Section View (with SectionCutOffsetMm)
                    if (ViewModel.IncludeSection && sectionTypeId != ElementId.InvalidElementId)
                    {
                        Transform tSec = Transform.Identity;
                        tSec.Origin = center + hand * sectionCutOffsetFeet;
                        tSec.BasisX = facing;
                        tSec.BasisY = XYZ.BasisZ;
                        tSec.BasisZ = hand;

                        BoundingBoxXYZ boxSec = new BoundingBoxXYZ { Transform = tSec };
                        boxSec.Min = new XYZ(-depth, minZRel, -w * 0.5);
                        boxSec.Max = new XYZ(depth, maxZRel, w * 0.5);

                        ViewSection crossSec = ViewSection.CreateSection(doc, sectionTypeId, boxSec);
                        ApplyViewSettingsAndClean(doc, crossSec, ViewModel.SectionScale, ViewModel.SelectedSectionTemplate, is3D: false, openingInst: inst, hostWall: hostWall);
                        SetViewNameWithConflictRule(doc, crossSec, FormatTokenString(ViewModel.NamingPattern, item, ViewModel.SectionSuffix));
                        PlaceAutomaticDimensions(doc, crossSec, inst, sym, center, facing, XYZ.BasisZ, depth, h, isPlan: false, isSection: true, isElevation: false);

                        PlaceAndCenterViewport(doc, sheet, crossSec, ptSec);
                    }

                    // 5. 3D Isolated View
                    if (ViewModel.IncludeIso && vft3DId != ElementId.InvalidElementId)
                    {
                        View3D isoView = View3D.CreateIsometric(doc, vft3DId);
                        SetViewNameWithConflictRule(doc, isoView, FormatTokenString(ViewModel.NamingPattern, item, ViewModel.IsoSuffix));

                        if (instBb != null)
                        {
                            double minZ3D = ViewModel.UseSameExtentForSectionAnd3D ? (center.Z + minZRel) : (instBb.Min.Z - cropOffsetFeet);
                            double maxZ3D = ViewModel.UseSameExtentForSectionAnd3D ? (center.Z + maxZRel) : (instBb.Max.Z + cropOffsetFeet);

                            BoundingBoxXYZ isoBox = new BoundingBoxXYZ
                            {
                                Min = new XYZ(instBb.Min.X - cropOffsetFeet, instBb.Min.Y - cropOffsetFeet, minZ3D),
                                Max = new XYZ(instBb.Max.X + cropOffsetFeet, instBb.Max.Y + cropOffsetFeet, maxZ3D)
                            };
                            isoView.SetSectionBox(isoBox);
                        }

                        doc.Regenerate();
                        ApplyViewSettingsAndClean(doc, isoView, ViewModel.IsoScale, ViewModel.SelectedIsoTemplate, is3D: true, openingInst: inst, hostWall: hostWall);

                        PlaceAndCenterViewport(doc, sheet, isoView, ptIso);
                    }

                    done++;
                    ViewModel.ProgressValue = (double)done / selected.Count * 100.0;
                }

                tx.Commit();
            }

            ViewModel.StatusMessage = $"✓ Generated 4-view details & {createdSheets} sheets using all 7 parameter groups!";
            TaskDialog.Show("OR Tools — 4-View Sheet Generation Complete",
                $"Successfully generated and aligned 4-view shop drawings on {createdSheets} sheets!");
        }

        private string FormatTokenString(string pattern, OpeningTypeItem item, string suffix)
        {
            string pat = string.IsNullOrWhiteSpace(pattern) ? "{Mark}{Sep}{Suffix}" : pattern;
            return pat.Replace("{Mark}", item.Mark ?? "01")
                      .Replace("{Sep}", ViewModel.Separator ?? "_")
                      .Replace("{Suffix}", suffix ?? "")
                      .Replace("{Level}", item.ReferenceLevel ?? "00 - Ground Floor")
                      .Replace("{Family}", item.FamilyName ?? "Family")
                      .Replace("{Type}", item.TypeName ?? "Type");
        }

        private double GetWidthFeet(FamilySymbol sym)
        {
            Parameter p = sym.get_Parameter(BuiltInParameter.DOOR_WIDTH)
                       ?? sym.get_Parameter(BuiltInParameter.WINDOW_WIDTH)
                       ?? sym.LookupParameter(ViewModel.ParamLeafWidth ?? "Width")
                       ?? sym.LookupParameter(ViewModel.ParamFrameWidth ?? "Frame Width");
            return (p != null && p.HasValue && p.AsDouble() > 0.1) ? p.AsDouble() : 3.0;
        }

        private double GetHeightFeet(FamilySymbol sym)
        {
            Parameter p = sym.get_Parameter(BuiltInParameter.DOOR_HEIGHT)
                       ?? sym.get_Parameter(BuiltInParameter.WINDOW_HEIGHT)
                       ?? sym.LookupParameter(ViewModel.ParamLeafHeight ?? "Height")
                       ?? sym.LookupParameter(ViewModel.ParamFrameHeight ?? "Frame Height");
            return (p != null && p.HasValue && p.AsDouble() > 0.1) ? p.AsDouble() : 7.0;
        }

        private void SetViewNameWithConflictRule(Document doc, View v, string baseName)
        {
            string clean = baseName.Replace(":", "-").Replace("/", "-").Replace("\\", "-");
            for (int i = 0; i < 50; i++)
            {
                string candidate = i == 0 ? clean : $"{clean}_{i}";
                try { v.Name = candidate; return; } catch { }
            }
        }

        private void SetUniqueSheetNumber(Document doc, ViewSheet s, string baseNum)
        {
            string clean = baseNum.Replace(":", "-").Replace("/", "-");
            for (int i = 0; i < 50; i++)
            {
                string candidate = i == 0 ? clean : $"{clean}-{i}";
                try { s.SheetNumber = candidate; return; } catch { }
            }
        }

        public string GetName() => "OR Opening 4-View External Event Handler";
    }
}
