using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ORTools.Models;
using ORTools.ViewModels;

namespace ORTools.Commands
{
    public enum OrParameterActionType
    {
        None,
        HarvestParameters,
        FilterByValue,
        SelectInRevit,
        ApplyPrefixSuffix,
        OverwriteValue,
        ClearValues,
        ColorSplashActiveView,
        ResetColorSplash,
        InjectRoomAndLevelData
    }

    public class OrParameterExternalEventHandler : IExternalEventHandler
    {
        public OrParameterViewModel ViewModel { get; set; }
        public OrParameterActionType CurrentAction { get; set; } = OrParameterActionType.None;

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
                    case OrParameterActionType.HarvestParameters:
                        ExecuteHarvest(doc);
                        break;

                    case OrParameterActionType.FilterByValue:
                        ExecuteFilterByValue(doc, selectInRevit: false, uidoc: uidoc);
                        break;

                    case OrParameterActionType.SelectInRevit:
                        ExecuteFilterByValue(doc, selectInRevit: true, uidoc: uidoc);
                        break;

                    case OrParameterActionType.ApplyPrefixSuffix:
                        ExecuteApplyPrefixSuffix(doc);
                        break;

                    case OrParameterActionType.OverwriteValue:
                        ExecuteOverwriteValue(doc);
                        break;

                    case OrParameterActionType.ClearValues:
                        ExecuteClearValues(doc);
                        break;

                    case OrParameterActionType.ColorSplashActiveView:
                        ExecuteColorSplash(doc);
                        break;

                    case OrParameterActionType.ResetColorSplash:
                        ExecuteResetColorSplash(doc);
                        break;

                    case OrParameterActionType.InjectRoomAndLevelData:
                        ExecuteInjectRoomData(doc);
                        break;
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                CurrentAction = OrParameterActionType.None;
            }
        }

        private void ExecuteHarvest(Document doc)
        {
            ViewModel.ProgressValue = 0;
            ViewModel.StatusMessage = "Scanning model elements and harvesting parameters...";

            FilteredElementCollector collector = ViewModel.IsActiveViewScope && doc.ActiveView != null
                ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                : new FilteredElementCollector(doc);

            var elements = collector
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null && e.Parameters != null && e.Parameters.Size > 0)
                .ToList();

            var paramDict = new Dictionary<string, OrParameterItemModel>(StringComparer.OrdinalIgnoreCase);
            var visitedTypesPerParam = new Dictionary<string, HashSet<ElementId>>(StringComparer.OrdinalIgnoreCase);

            int total = elements.Count;
            int processed = 0;

            foreach (Element el in elements)
            {
                processed++;
                // 1. Harvest Instance Parameters
                foreach (Parameter p in el.Parameters)
                {
                    if (p?.Definition == null || string.IsNullOrWhiteSpace(p.Definition.Name)) continue;
                    if (p.IsReadOnly) continue;
                    if (p.StorageType == StorageType.None) continue;

                    string pName = p.Definition.Name.Trim();
                    string key = $"{pName}|Instance";

                    if (!paramDict.TryGetValue(key, out var item))
                    {
                        item = new OrParameterItemModel
                        {
                            Name = pName,
                            IsInstance = true,
                            DataStorageType = p.StorageType,
                            IsReadOnly = p.IsReadOnly
                        };
                        paramDict[key] = item;
                    }
                    item.OwnerElementIds.Add(el.Id);
                }

                // 2. Harvest Type Parameters
                ElementId typeId = el.GetTypeId();
                if (typeId != null && typeId != ElementId.InvalidElementId)
                {
                    Element typeEl = doc.GetElement(typeId);
                    if (typeEl != null && typeEl.Parameters != null)
                    {
                        foreach (Parameter tp in typeEl.Parameters)
                        {
                            if (tp?.Definition == null || string.IsNullOrWhiteSpace(tp.Definition.Name)) continue;
                            if (tp.IsReadOnly) continue;
                            if (tp.StorageType == StorageType.None) continue;

                            string tpName = tp.Definition.Name.Trim();
                            string tKey = $"{tpName}|Type";

                            if (!paramDict.TryGetValue(tKey, out var tItem))
                            {
                                tItem = new OrParameterItemModel
                                {
                                    Name = tpName,
                                    IsInstance = false,
                                    DataStorageType = tp.StorageType,
                                    IsReadOnly = tp.IsReadOnly
                                };
                                paramDict[tKey] = tItem;
                                visitedTypesPerParam[tKey] = new HashSet<ElementId>();
                            }

                            if (!tItem.TypeToInstanceIdsMap.TryGetValue(typeId, out var instList))
                            {
                                instList = new List<ElementId>();
                                tItem.TypeToInstanceIdsMap[typeId] = instList;
                            }
                            instList.Add(el.Id);

                            if (visitedTypesPerParam[tKey].Add(typeId))
                            {
                                tItem.OwnerElementIds.Add(typeId);
                            }
                        }
                    }
                }
            }

            var sortedList = paramDict.Values
                .Select(x => { x.ElementCount = x.OwnerElementIds.Count; return x; })
                .OrderBy(x => x.Name)
                .ThenBy(x => x.IsInstance ? 0 : 1)
                .ToList();

            ViewModel.UpdateHarvestedParameters(sortedList);
            ViewModel.ProgressValue = 100;
            ViewModel.StatusMessage = $"Harvested {sortedList.Count} editable parameters from {total} elements.";
        }

        private List<ElementId> GetTargetElements(Document doc, OrParameterItemModel paramItem, out List<ElementId> selectableInstanceIds)
        {
            selectableInstanceIds = new List<ElementId>();
            var matchedTargetIds = new List<ElementId>();
            if (paramItem == null) return matchedTargetIds;

            string filterVal = ViewModel.ValueFilterText ?? string.Empty;
            bool hasFilter = !string.IsNullOrEmpty(filterVal);
            StringComparison cmp = ViewModel.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (ElementId id in paramItem.OwnerElementIds)
            {
                Element el = doc.GetElement(id);
                if (el == null) continue;
                Parameter p = el.LookupParameter(paramItem.Name);
                if (p == null) continue;

                if (hasFilter)
                {
                    string currentStr = GetParameterDisplayString(doc, p);
                    if (currentStr == null || currentStr.IndexOf(filterVal, cmp) < 0)
                        continue;
                }

                matchedTargetIds.Add(id);

                if (paramItem.IsInstance)
                {
                    selectableInstanceIds.Add(id);
                }
                else if (paramItem.TypeToInstanceIdsMap.TryGetValue(id, out var instIds))
                {
                    selectableInstanceIds.AddRange(instIds);
                }
            }

            return matchedTargetIds;
        }

        private string GetParameterDisplayString(Document doc, Parameter p)
        {
            if (p == null) return string.Empty;
            switch (p.StorageType)
            {
                case StorageType.String:
                    return p.AsString() ?? string.Empty;
                case StorageType.Integer:
                    return p.AsValueString() ?? p.AsInteger().ToString(CultureInfo.InvariantCulture);
                case StorageType.Double:
                    return p.AsValueString() ?? p.AsDouble().ToString("0.###", CultureInfo.InvariantCulture);
                case StorageType.ElementId:
                    ElementId eid = p.AsElementId();
                    if (eid == null || eid == ElementId.InvalidElementId) return string.Empty;
                    Element refEl = doc.GetElement(eid);
                    return refEl != null ? refEl.Name : eid.Value.ToString(CultureInfo.InvariantCulture);
                default:
                    return string.Empty;
            }
        }

        private void ExecuteFilterByValue(Document doc, bool selectInRevit, UIDocument uidoc)
        {
            var selectedParam = ViewModel.SelectedParameter;
            if (selectedParam == null)
            {
                ViewModel.StatusMessage = "Please select a parameter from the left list first.";
                return;
            }

            var matchedTargets = GetTargetElements(doc, selectedParam, out var selectableInstances);
            selectedParam.ElementCount = matchedTargets.Count;
            ViewModel.TotalItemsCount = selectableInstances.Count > 0 ? selectableInstances.Count : matchedTargets.Count;
            ViewModel.ProgressValue = 100;

            if (selectInRevit && selectableInstances.Count > 0)
            {
                uidoc.Selection.SetElementIds(selectableInstances);
                ViewModel.StatusMessage = $"Selected {selectableInstances.Count} matching elements in Revit.";
            }
            else
            {
                ViewModel.StatusMessage = $"Found {matchedTargets.Count} matching items (Instances: {selectableInstances.Count}).";
            }
        }

        private void ExecuteApplyPrefixSuffix(Document doc)
        {
            var selectedParam = ViewModel.SelectedParameter;
            if (selectedParam == null) return;

            if (!selectedParam.IsStringParameter)
            {
                TaskDialog.Show("OR PARAMETER", "Prefix & Suffix can only be applied to String (Text) parameters.");
                return;
            }

            string prefix = ViewModel.PrefixText ?? string.Empty;
            string suffix = ViewModel.SuffixText ?? string.Empty;
            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
            {
                ViewModel.StatusMessage = "Please enter a Prefix or Suffix to add.";
                return;
            }

            var targets = GetTargetElements(doc, selectedParam, out _);
            if (targets.Count == 0)
            {
                ViewModel.StatusMessage = "No matching elements found to update.";
                return;
            }

            int updated = 0;
            int total = targets.Count;
            ViewModel.ProgressValue = 0;

            using (Transaction tx = new Transaction(doc, $"OR Parameter: Prefix/Suffix on {selectedParam.Name}"))
            {
                tx.Start();
                for (int i = 0; i < total; i++)
                {
                    Element el = doc.GetElement(targets[i]);
                    Parameter p = el?.LookupParameter(selectedParam.Name);
                    if (p != null && !p.IsReadOnly && p.StorageType == StorageType.String)
                    {
                        string currentVal = p.AsString() ?? string.Empty;
                        string newVal = prefix + currentVal + suffix;
                        p.Set(newVal);
                        updated++;
                    }
                    ViewModel.ProgressValue = (double)(i + 1) / total * 100.0;
                }
                tx.Commit();
            }

            ViewModel.TotalItemsCount = updated;
            ViewModel.StatusMessage = $"Added Prefix/Suffix to {updated} items in a single transaction.";
        }

        private void ExecuteOverwriteValue(Document doc)
        {
            var selectedParam = ViewModel.SelectedParameter;
            if (selectedParam == null) return;

            string rawInput = ViewModel.OverwriteValueText ?? string.Empty;
            var targets = GetTargetElements(doc, selectedParam, out _);
            if (targets.Count == 0)
            {
                ViewModel.StatusMessage = "No matching elements found to overwrite.";
                return;
            }

            int updated = 0;
            int total = targets.Count;
            ViewModel.ProgressValue = 0;

            using (Transaction tx = new Transaction(doc, $"OR Parameter: Overwrite {selectedParam.Name}"))
            {
                tx.Start();
                for (int i = 0; i < total; i++)
                {
                    Element el = doc.GetElement(targets[i]);
                    Parameter p = el?.LookupParameter(selectedParam.Name);
                    if (p != null && !p.IsReadOnly)
                    {
                        bool ok = TrySetParameterFromString(p, rawInput);
                        if (ok) updated++;
                    }
                    ViewModel.ProgressValue = (double)(i + 1) / total * 100.0;
                }
                tx.Commit();
            }

            ViewModel.TotalItemsCount = updated;
            ViewModel.StatusMessage = $"Overwrote {updated} items with '{rawInput}' in a single transaction.";
        }

        private bool TrySetParameterFromString(Parameter p, string input)
        {
            try
            {
                switch (p.StorageType)
                {
                    case StorageType.String:
                        return p.Set(input ?? string.Empty);

                    case StorageType.Integer:
                        if (int.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int iVal))
                            return p.Set(iVal);
                        if (input.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase) || input.Trim().Equals("True", StringComparison.OrdinalIgnoreCase))
                            return p.Set(1);
                        if (input.Trim().Equals("No", StringComparison.OrdinalIgnoreCase) || input.Trim().Equals("False", StringComparison.OrdinalIgnoreCase))
                            return p.Set(0);
                        return false;

                    case StorageType.Double:
                        if (p.SetValueString(input)) return true;
                        if (double.TryParse(input.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal))
                            return p.Set(dVal);
                        return false;

                    case StorageType.ElementId:
                        if (long.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long idVal))
                            return p.Set(new ElementId(idVal));
                        return false;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ExecuteClearValues(Document doc)
        {
            var selectedParam = ViewModel.SelectedParameter;
            if (selectedParam == null) return;

            var targets = GetTargetElements(doc, selectedParam, out _);
            if (targets.Count == 0)
            {
                ViewModel.StatusMessage = "No matching elements found to clear.";
                return;
            }

            int cleared = 0;
            int total = targets.Count;
            ViewModel.ProgressValue = 0;

            using (Transaction tx = new Transaction(doc, $"OR Parameter: Clear {selectedParam.Name}"))
            {
                tx.Start();
                for (int i = 0; i < total; i++)
                {
                    Element el = doc.GetElement(targets[i]);
                    Parameter p = el?.LookupParameter(selectedParam.Name);
                    if (p != null && !p.IsReadOnly)
                    {
                        try
                        {
                            switch (p.StorageType)
                            {
                                case StorageType.String:
                                    if (p.Set(string.Empty)) cleared++;
                                    break;
                                case StorageType.ElementId:
                                    if (p.Set(ElementId.InvalidElementId)) cleared++;
                                    break;
                                case StorageType.Integer:
                                    if (p.Set(0)) cleared++;
                                    break;
                                case StorageType.Double:
                                    if (p.Set(0.0)) cleared++;
                                    break;
                            }
                        }
                        catch { }
                    }
                    ViewModel.ProgressValue = (double)(i + 1) / total * 100.0;
                }
                tx.Commit();
            }

            ViewModel.TotalItemsCount = cleared;
            ViewModel.StatusMessage = $"Cleared values on {cleared} items in a single transaction.";
        }

        private void ExecuteColorSplash(Document doc)
        {
            var selectedParam = ViewModel.SelectedParameter;
            if (selectedParam == null || doc.ActiveView == null) return;

            GetTargetElements(doc, selectedParam, out var selectableInstances);
            if (selectableInstances.Count == 0)
            {
                ViewModel.StatusMessage = "No matching instances to color splash in Active View.";
                return;
            }

            ElementId solidPatternId = ElementId.InvalidElementId;
            var fillPatterns = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>();
            foreach (var fp in fillPatterns)
            {
                if (fp.GetFillPattern().IsSolidFill)
                {
                    solidPatternId = fp.Id;
                    break;
                }
            }

            Color[] palette = new[]
            {
                new Color(16, 185, 129),
                new Color(56, 189, 248),
                new Color(245, 158, 11),
                new Color(239, 68, 68),
                new Color(168, 85, 247),
                new Color(236, 72, 153),
                new Color(34, 197, 94),
                new Color(59, 130, 246),
                new Color(249, 115, 22),
                new Color(20, 184, 166)
            };

            var valColorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            int colorIdx = 0;
            int coloredCount = 0;

            using (Transaction tx = new Transaction(doc, $"GOAT PARAMETER: Color Splash by {selectedParam.Name}"))
            {
                tx.Start();
                foreach (ElementId instId in selectableInstances)
                {
                    Element el = doc.GetElement(instId);
                    if (el == null) continue;

                    Parameter p = selectedParam.IsInstance
                        ? el.LookupParameter(selectedParam.Name)
                        : doc.GetElement(el.GetTypeId())?.LookupParameter(selectedParam.Name);

                    string val = GetParameterDisplayString(doc, p);
                    if (string.IsNullOrWhiteSpace(val)) val = "<Empty>";

                    if (!valColorMap.TryGetValue(val, out Color col))
                    {
                        col = palette[colorIdx % palette.Length];
                        valColorMap[val] = col;
                        colorIdx++;
                    }

                    var ogs = new OverrideGraphicSettings();
                    ogs.SetProjectionLineColor(col);
                    ogs.SetCutLineColor(col);
                    if (solidPatternId != ElementId.InvalidElementId)
                    {
                        ogs.SetSurfaceForegroundPatternId(solidPatternId);
                        ogs.SetSurfaceForegroundPatternColor(col);
                        ogs.SetCutForegroundPatternId(solidPatternId);
                        ogs.SetCutForegroundPatternColor(col);
                    }
                    doc.ActiveView.SetElementOverrides(instId, ogs);
                    coloredCount++;
                }
                tx.Commit();
            }

            ViewModel.StatusMessage = $"Color-splashed {coloredCount} elements across {valColorMap.Count} unique values of '{selectedParam.Name}'.";
        }

        private void ExecuteResetColorSplash(Document doc)
        {
            if (doc.ActiveView == null) return;
            var elems = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .WhereElementIsNotElementType()
                .ToElementIds();

            using (Transaction tx = new Transaction(doc, "GOAT PARAMETER: Reset Color Splash"))
            {
                tx.Start();
                var emptyOgs = new OverrideGraphicSettings();
                foreach (var id in elems)
                {
                    try { doc.ActiveView.SetElementOverrides(id, emptyOgs); } catch { }
                }
                tx.Commit();
            }

            ViewModel.StatusMessage = "Reset all Color Splash graphic overrides in Active View.";
        }

        private void ExecuteInjectRoomData(Document doc)
        {
            var selectedParam = ViewModel.SelectedParameter;
            if (selectedParam == null || !selectedParam.IsStringParameter) return;

            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Area > 0.01)
                .ToList();

            GetTargetElements(doc, selectedParam, out var selectableInstances);
            int injected = 0;

            using (Transaction tx = new Transaction(doc, $"GOAT PARAMETER: Inject Room & Level into {selectedParam.Name}"))
            {
                tx.Start();
                foreach (ElementId id in selectableInstances)
                {
                    Element el = doc.GetElement(id);
                    if (el == null) continue;
                    Parameter p = el.LookupParameter(selectedParam.Name);
                    if (p == null || p.IsReadOnly || p.StorageType != StorageType.String) continue;

                    string lvlName = doc.GetElement(el.LevelId)?.Name ?? string.Empty;
                    Room hostRoom = null;

                    if (el is FamilyInstance fi)
                    {
                        try { hostRoom = fi.Room ?? fi.ToRoom ?? fi.FromRoom; } catch { }
                    }

                    if (hostRoom == null)
                    {
                        XYZ pt = null;
                        if (el.Location is LocationPoint lp) pt = lp.Point;
                        else if (el.Location is LocationCurve lc && lc.Curve != null) pt = lc.Curve.Evaluate(0.5, true);
                        else
                        {
                            BoundingBoxXYZ bb = el.get_BoundingBox(null);
                            if (bb != null) pt = (bb.Min + bb.Max) * 0.5;
                        }

                        if (pt != null)
                        {
                            foreach (var rm in rooms)
                            {
                                try
                                {
                                    if (rm.IsPointInRoom(pt)) { hostRoom = rm; break; }
                                }
                                catch { }
                            }
                        }
                    }

                    string rmText = hostRoom != null
                        ? $"{hostRoom.Number} - {hostRoom.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? hostRoom.Name}"
                        : string.Empty;

                    string combined = !string.IsNullOrEmpty(rmText) && !string.IsNullOrEmpty(lvlName)
                        ? $"{rmText} ({lvlName})"
                        : (!string.IsNullOrEmpty(rmText) ? rmText : lvlName);

                    if (!string.IsNullOrWhiteSpace(combined))
                    {
                        p.Set(combined);
                        injected++;
                    }
                }
                tx.Commit();
            }

            ViewModel.StatusMessage = $"Injected spatial Host Room & Level into '{selectedParam.Name}' on {injected} elements.";
        }

        public string GetName() => "GOAT Parameter Bulk Editor External Event Handler";
    }
}
