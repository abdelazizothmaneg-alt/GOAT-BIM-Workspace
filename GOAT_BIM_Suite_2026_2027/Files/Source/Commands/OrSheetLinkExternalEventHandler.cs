using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Models;
using ORTools.ViewModels;

namespace ORTools.Commands
{
    public enum OrSheetLinkActionType
    {
        None,
        ScanCategoriesAndSchedules,
        LoadParametersForSelection,
        ExtractDataToGrid,
        PushGridChangesToRevit,
        SelectHighlightedElementsInRevit
    }

    public class OrSheetLinkExternalEventHandler : IExternalEventHandler
    {
        public OrSheetLinkViewModel ViewModel { get; set; }
        public OrSheetLinkActionType CurrentAction { get; set; } = OrSheetLinkActionType.None;

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
                    case OrSheetLinkActionType.ScanCategoriesAndSchedules:
                        ExecuteScanCategoriesAndSchedules(doc);
                        break;

                    case OrSheetLinkActionType.LoadParametersForSelection:
                        ExecuteLoadParameters(doc);
                        break;

                    case OrSheetLinkActionType.ExtractDataToGrid:
                        ExecuteExtractDataToGrid(doc);
                        break;

                    case OrSheetLinkActionType.PushGridChangesToRevit:
                        ExecutePushChangesToRevit(doc);
                        break;

                    case OrSheetLinkActionType.SelectHighlightedElementsInRevit:
                        ExecuteSelectInRevit(uidoc);
                        break;
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                CurrentAction = OrSheetLinkActionType.None;
            }
        }

        private List<Element> GetElementsForCurrentSelection(Document doc)
        {
            var selectedCat = ViewModel.SelectedSourceItem;
            if (selectedCat == null) return new List<Element>();

            if (selectedCat.IsSchedule && selectedCat.ScheduleId != null && selectedCat.ScheduleId != ElementId.InvalidElementId)
            {
                var schedElems = new FilteredElementCollector(doc, selectedCat.ScheduleId)
                    .WhereElementIsNotElementType()
                    .ToList();
                return schedElems;
            }

            FilteredElementCollector collector = ViewModel.IsActiveViewScope && doc.ActiveView != null
                ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                : new FilteredElementCollector(doc);

            if (selectedCat.CategoryId != null && selectedCat.CategoryId != ElementId.InvalidElementId)
            {
                return collector
                    .OfCategoryId(selectedCat.CategoryId)
                    .WhereElementIsNotElementType()
                    .ToList();
            }

            return new List<Element>();
        }

        private void ExecuteScanCategoriesAndSchedules(Document doc)
        {
            ViewModel.ProgressValue = 0;
            ViewModel.StatusMessage = "Scanning model categories and schedules...";

            var categoriesList = new List<SheetLinkCategoryItem>();
            var schedulesList = new List<SheetLinkCategoryItem>();

            // 1. Collect all non-template ViewSchedules
            var schedules = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .Where(s => !s.IsTemplate && !s.IsTitleblockRevisionSchedule && !s.IsInternalKeynoteSchedule)
                .OrderBy(s => s.Name)
                .ToList();

            foreach (var sch in schedules)
            {
                schedulesList.Add(new SheetLinkCategoryItem
                {
                    Name = sch.Name,
                    IsSchedule = true,
                    ScheduleId = sch.Id,
                    CategoryId = sch.Definition?.CategoryId ?? ElementId.InvalidElementId,
                    ElementCount = 0
                });
            }

            // 2. Collect Categories that actually have elements in the chosen scope
            FilteredElementCollector collector = ViewModel.IsActiveViewScope && doc.ActiveView != null
                ? new FilteredElementCollector(doc, doc.ActiveView.Id)
                : new FilteredElementCollector(doc);

            var elements = collector
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null && (e.Category.HasMaterialQuantities || e.Category.CategoryType == CategoryType.Model || e.Category.Id.Value == (long)BuiltInCategory.OST_Sheets || e.Category.Id.Value == (long)BuiltInCategory.OST_Rooms))
                .ToList();

            var grouped = elements
                .Where(e => e.Category != null && !string.IsNullOrWhiteSpace(e.Category.Name))
                .GroupBy(e => e.Category.Id)
                .Select(g => new SheetLinkCategoryItem
                {
                    Name = g.First().Category.Name,
                    CategoryId = g.Key,
                    IsSchedule = false,
                    ElementCount = g.Count()
                })
                .OrderBy(c => c.Name)
                .ToList();

            categoriesList.AddRange(grouped);

            ViewModel.UpdateSources(categoriesList, schedulesList);
            ViewModel.ProgressValue = 100;
            ViewModel.StatusMessage = $"Found {categoriesList.Count} Categories and {schedulesList.Count} Schedules.";
        }

        private void ExecuteLoadParameters(Document doc)
        {
            var elems = GetElementsForCurrentSelection(doc);
            var paramMap = new Dictionary<string, SheetLinkParameterColumn>(StringComparer.OrdinalIgnoreCase);

            foreach (Element el in elems)
            {
                // Instance parameters
                foreach (Parameter p in el.Parameters)
                {
                    if (p?.Definition == null || string.IsNullOrWhiteSpace(p.Definition.Name)) continue;
                    if (p.StorageType == StorageType.None) continue;

                    string pName = p.Definition.Name.Trim();
                    if (!paramMap.ContainsKey(pName))
                    {
                        paramMap[pName] = new SheetLinkParameterColumn
                        {
                            Name = pName,
                            IsInstance = true,
                            IsReadOnly = p.IsReadOnly,
                            DataStorageType = p.StorageType
                        };
                    }
                }

                // Type parameters
                ElementId tid = el.GetTypeId();
                if (tid != null && tid != ElementId.InvalidElementId)
                {
                    Element typeEl = doc.GetElement(tid);
                    if (typeEl != null)
                    {
                        foreach (Parameter tp in typeEl.Parameters)
                        {
                            if (tp?.Definition == null || string.IsNullOrWhiteSpace(tp.Definition.Name)) continue;
                            if (tp.StorageType == StorageType.None) continue;

                            string tpName = $"[Type] {tp.Definition.Name.Trim()}";
                            if (!paramMap.ContainsKey(tpName))
                            {
                                paramMap[tpName] = new SheetLinkParameterColumn
                                {
                                    Name = tpName,
                                    IsInstance = false,
                                    IsReadOnly = tp.IsReadOnly,
                                    DataStorageType = tp.StorageType
                                };
                            }
                        }
                    }
                }
            }

            var sortedCols = paramMap.Values
                .OrderBy(x => x.IsReadOnly ? 1 : 0)
                .ThenBy(x => x.Name)
                .ToList();

            var defaultNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Mark", "Number", "Name", "Sheet Number", "Sheet Name", "Comments",
                "Department", "Base Finish", "Ceiling Finish", "Wall Finish", "Floor Finish",
                "Level", "Unconnected Height", "[Type] Type Name", "[Type] Type Mark"
            };

            int autoSelected = 0;
            foreach (var col in sortedCols)
            {
                if (defaultNames.Contains(col.Name))
                {
                    col.IsSelected = true;
                    autoSelected++;
                }
            }

            if (autoSelected == 0)
            {
                foreach (var col in sortedCols.Where(c => !c.IsReadOnly).Take(5))
                    col.IsSelected = true;
            }

            ViewModel.UpdateAvailableParameters(sortedCols, elems.Count);
            ExecuteExtractDataToGrid(doc);
        }

        private void ExecuteExtractDataToGrid(Document doc)
        {
            var elems = GetElementsForCurrentSelection(doc);
            var selectedCols = ViewModel.SelectedParameters.ToList();

            DataTable dt = new DataTable("OR_SheetLink_Data");

            DataColumn colId = new DataColumn("ElementId", typeof(string)) { ReadOnly = true };
            DataColumn colCat = new DataColumn("Category", typeof(string)) { ReadOnly = true };
            DataColumn colFam = new DataColumn("Family & Type", typeof(string)) { ReadOnly = true };
            dt.Columns.Add(colId);
            dt.Columns.Add(colCat);
            dt.Columns.Add(colFam);

            foreach (var pCol in selectedCols)
            {
                if (!dt.Columns.Contains(pCol.Name))
                {
                    DataColumn dc = new DataColumn(pCol.Name, typeof(string))
                    {
                        ReadOnly = pCol.IsReadOnly && !ViewModel.AllowEditReadOnlyInPreview
                    };
                    dt.Columns.Add(dc);
                }
            }

            foreach (Element el in elems)
            {
                DataRow row = dt.NewRow();
                row["ElementId"] = el.Id.Value.ToString(CultureInfo.InvariantCulture);
                row["Category"] = el.Category?.Name ?? "-";
                row["Family & Type"] = el.Name ?? "-";

                Element typeEl = null;
                ElementId tid = el.GetTypeId();
                if (tid != null && tid != ElementId.InvalidElementId)
                    typeEl = doc.GetElement(tid);

                foreach (var pCol in selectedCols)
                {
                    Parameter p = null;
                    if (pCol.Name.StartsWith("[Type] ", StringComparison.OrdinalIgnoreCase))
                    {
                        string cleanTypeParamName = pCol.Name.Substring(7);
                        p = typeEl?.LookupParameter(cleanTypeParamName);
                    }
                    else
                    {
                        p = el.LookupParameter(pCol.Name);
                    }

                    row[pCol.Name] = ReadParameterAsString(doc, p);
                }

                dt.Rows.Add(row);
            }

            ViewModel.UpdateDataTable(dt);
            ViewModel.ProgressValue = 100;
            ViewModel.StatusMessage = $"Loaded {dt.Rows.Count} elements x {selectedCols.Count} parameters into live spreadsheet.";
        }

        private string ReadParameterAsString(Document doc, Parameter p)
        {
            if (p == null || !p.HasValue) return string.Empty;
            switch (p.StorageType)
            {
                case StorageType.String:
                    return p.AsString() ?? string.Empty;
                case StorageType.Integer:
                    return p.AsValueString() ?? p.AsInteger().ToString(CultureInfo.InvariantCulture);
                case StorageType.Double:
                    return p.AsValueString() ?? (p.AsDouble() * 304.8).ToString("0.##", CultureInfo.InvariantCulture);
                case StorageType.ElementId:
                    ElementId eid = p.AsElementId();
                    if (eid == null || eid == ElementId.InvalidElementId) return string.Empty;
                    Element refEl = doc.GetElement(eid);
                    return refEl != null ? refEl.Name : eid.Value.ToString(CultureInfo.InvariantCulture);
                default:
                    return string.Empty;
            }
        }

        private void ExecutePushChangesToRevit(Document doc)
        {
            DataTable dt = ViewModel.LiveDataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                ViewModel.StatusMessage = "No table data to push to Revit.";
                return;
            }

            int updatedCells = 0;
            int updatedElements = 0;
            int createdElements = 0;
            int totalRows = dt.Rows.Count;
            ViewModel.ProgressValue = 0;

            using (Transaction tx = new Transaction(doc, "GOAT SHEETLINK: Sync Spreadsheet Changes to Model"))
            {
                tx.Start();

                for (int r = 0; r < totalRows; r++)
                {
                    DataRow row = dt.Rows[r];
                    string idStr = row["ElementId"]?.ToString()?.Trim();
                    Element el = null;

                    if (string.IsNullOrEmpty(idStr) || idStr.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                    {
                        string catLabel = ViewModel.SelectedSourceItem?.Name ?? row["Category"]?.ToString() ?? string.Empty;
                        if (catLabel.IndexOf("Sheet", StringComparison.OrdinalIgnoreCase) >= 0 || dt.Columns.Contains("Sheet Number"))
                        {
                            ViewSheet newSheet = ViewSheet.CreatePlaceholder(doc);
                            el = newSheet;
                            createdElements++;
                            row.Table.Columns["ElementId"].ReadOnly = false;
                            row["ElementId"] = el.Id.Value.ToString(CultureInfo.InvariantCulture);
                            row.Table.Columns["ElementId"].ReadOnly = true;
                        }
                        else if (catLabel.IndexOf("Room", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Phase phase = doc.Phases.Cast<Phase>().LastOrDefault();
                            if (phase != null)
                            {
                                var newRoom = doc.Create.NewRoom(phase);
                                el = newRoom;
                                createdElements++;
                                row.Table.Columns["ElementId"].ReadOnly = false;
                                row["ElementId"] = el.Id.Value.ToString(CultureInfo.InvariantCulture);
                                row.Table.Columns["ElementId"].ReadOnly = true;
                            }
                        }
                    }
                    else if (long.TryParse(idStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long idVal))
                    {
                        el = doc.GetElement(new ElementId(idVal));
                    }

                    if (el == null) continue;

                    Element typeEl = null;
                    ElementId tid = el.GetTypeId();
                    if (tid != null && tid != ElementId.InvalidElementId)
                        typeEl = doc.GetElement(tid);

                    bool elemModified = false;

                    foreach (DataColumn col in dt.Columns)
                    {
                        string colName = col.ColumnName;
                        if (colName == "ElementId" || colName == "Category" || colName == "Family & Type")
                            continue;

                        string newVal = row[colName]?.ToString() ?? string.Empty;
                        Parameter p = null;

                        if (colName.StartsWith("[Type] ", StringComparison.OrdinalIgnoreCase))
                        {
                            string cleanName = colName.Substring(7);
                            p = typeEl?.LookupParameter(cleanName);
                        }
                        else
                        {
                            p = el.LookupParameter(colName);
                        }

                        if (p == null || p.IsReadOnly) continue;

                        string oldVal = ReadParameterAsString(doc, p);
                        if (!string.Equals(oldVal, newVal, StringComparison.Ordinal))
                        {
                            if (WriteStringToParameter(p, newVal))
                            {
                                updatedCells++;
                                elemModified = true;
                            }
                        }
                    }

                    if (elemModified) updatedElements++;
                    ViewModel.ProgressValue = (double)(r + 1) / totalRows * 100.0;
                }

                tx.Commit();
            }

            ViewModel.StatusMessage = $"Successfully synced {updatedCells} modified cells across {updatedElements} elements (+{createdElements} new elements created) into Revit!";
            string summaryMsg = "Successfully pushed spreadsheet changes to Revit model!"
                + Environment.NewLine + Environment.NewLine
                + "New Elements Created: " + createdElements
                + Environment.NewLine
                + "Elements Updated: " + updatedElements
                + Environment.NewLine
                + "Parameter Cells Modified: " + updatedCells;
            TaskDialog.Show("GOAT SHEETLINK — Sync Complete", summaryMsg);
        }

        private bool WriteStringToParameter(Parameter p, string val)
        {
            try
            {
                switch (p.StorageType)
                {
                    case StorageType.String:
                        return p.Set(val ?? string.Empty);

                    case StorageType.Integer:
                        if (int.TryParse(val.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                            return p.Set(iv);
                        if (val.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase) || val.Trim().Equals("True", StringComparison.OrdinalIgnoreCase))
                            return p.Set(1);
                        if (val.Trim().Equals("No", StringComparison.OrdinalIgnoreCase) || val.Trim().Equals("False", StringComparison.OrdinalIgnoreCase))
                            return p.Set(0);
                        return false;

                    case StorageType.Double:
                        if (p.SetValueString(val)) return true;
                        if (double.TryParse(val.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dv))
                            return p.Set(dv / 304.8);
                        return false;

                    case StorageType.ElementId:
                        if (string.IsNullOrWhiteSpace(val))
                            return p.Set(ElementId.InvalidElementId);
                        if (long.TryParse(val.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long eid))
                            return p.Set(new ElementId(eid));
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

        private void ExecuteSelectInRevit(UIDocument uidoc)
        {
            var ids = ViewModel.GetHighlightedElementIds();
            if (ids.Count > 0)
            {
                uidoc.Selection.SetElementIds(ids);
                ViewModel.StatusMessage = $"Selected {ids.Count} elements in Revit.";
            }
        }

        public string GetName() => "OR SheetLink Bidirectional Excel/Model Sync Handler";
    }
}
