using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Commands;
using ORTools.Models;

namespace ORTools.ViewModels
{
    public class OrSheetLinkViewModel : INotifyPropertyChanged
    {
        private readonly OrSheetLinkExternalEventHandler _handler;
        private readonly ExternalEvent _externalEvent;

        private List<SheetLinkCategoryItem> _allCategories = new List<SheetLinkCategoryItem>();
        private List<SheetLinkCategoryItem> _allSchedules = new List<SheetLinkCategoryItem>();
        private List<SheetLinkParameterColumn> _allAvailableParameters = new List<SheetLinkParameterColumn>();

        private bool _isCategoriesMode = true;
        private bool _isSchedulesMode = false;
        private bool _isWholeModelScope = true;
        private bool _isActiveViewScope = false;
        private bool _allowEditReadOnlyInPreview = false;

        private string _sourceSearchText = string.Empty;
        private string _paramSearchText = string.Empty;
        private string _gridFilterText = string.Empty;
        private string _findText = string.Empty;
        private string _replaceText = string.Empty;
        private string _targetColumnForFill = string.Empty;
        private string _bulkFillValueText = string.Empty;

        private SheetLinkCategoryItem _selectedSourceItem;
        private SheetLinkParameterColumn _selectedAvailableParameter;
        private SheetLinkParameterColumn _selectedActiveParameter;

        private DataTable _liveDataTable = new DataTable();
        private DataView _liveDataView;

        private double _progressValue = 0.0;
        private string _statusMessage = "Ready — Select a Category or Schedule to link with Excel / Live Grid.";

        public ObservableCollection<SheetLinkCategoryItem> FilteredSourceItems { get; } = new ObservableCollection<SheetLinkCategoryItem>();
        public ObservableCollection<SheetLinkParameterColumn> AvailableParameters { get; } = new ObservableCollection<SheetLinkParameterColumn>();
        public ObservableCollection<SheetLinkParameterColumn> SelectedParameters { get; } = new ObservableCollection<SheetLinkParameterColumn>();
        public ObservableCollection<string> EditableColumnNames { get; } = new ObservableCollection<string>();

        public DataTable LiveDataTable => _liveDataTable;
        public DataView LiveDataView
        {
            get => _liveDataView;
            set { _liveDataView = value; OnPropertyChanged(nameof(LiveDataView)); OnPropertyChanged(nameof(RowCountLabel)); }
        }

        public string RowCountLabel => $"Rows: {(_liveDataView != null ? _liveDataView.Count : 0)}  |  Columns: {SelectedParameters.Count + 3}";

        public bool IsCategoriesMode
        {
            get => _isCategoriesMode;
            set
            {
                if (_isCategoriesMode != value)
                {
                    _isCategoriesMode = value;
                    OnPropertyChanged(nameof(IsCategoriesMode));
                    if (value)
                    {
                        _isSchedulesMode = false;
                        OnPropertyChanged(nameof(IsSchedulesMode));
                        RefreshSourceFilter();
                    }
                }
            }
        }

        public bool IsSchedulesMode
        {
            get => _isSchedulesMode;
            set
            {
                if (_isSchedulesMode != value)
                {
                    _isSchedulesMode = value;
                    OnPropertyChanged(nameof(IsSchedulesMode));
                    if (value)
                    {
                        _isCategoriesMode = false;
                        OnPropertyChanged(nameof(IsCategoriesMode));
                        RefreshSourceFilter();
                    }
                }
            }
        }

        public bool IsWholeModelScope
        {
            get => _isWholeModelScope;
            set
            {
                if (_isWholeModelScope != value)
                {
                    _isWholeModelScope = value;
                    OnPropertyChanged(nameof(IsWholeModelScope));
                    if (value)
                    {
                        _isActiveViewScope = false;
                        OnPropertyChanged(nameof(IsActiveViewScope));
                        TriggerExternal(OrSheetLinkActionType.ScanCategoriesAndSchedules);
                    }
                }
            }
        }

        public bool IsActiveViewScope
        {
            get => _isActiveViewScope;
            set
            {
                if (_isActiveViewScope != value)
                {
                    _isActiveViewScope = value;
                    OnPropertyChanged(nameof(IsActiveViewScope));
                    if (value)
                    {
                        _isWholeModelScope = false;
                        OnPropertyChanged(nameof(IsWholeModelScope));
                        TriggerExternal(OrSheetLinkActionType.ScanCategoriesAndSchedules);
                    }
                }
            }
        }

        public bool AllowEditReadOnlyInPreview
        {
            get => _allowEditReadOnlyInPreview;
            set { _allowEditReadOnlyInPreview = value; OnPropertyChanged(nameof(AllowEditReadOnlyInPreview)); }
        }

        public string SourceSearchText
        {
            get => _sourceSearchText;
            set { _sourceSearchText = value; OnPropertyChanged(nameof(SourceSearchText)); RefreshSourceFilter(); }
        }

        public string ParamSearchText
        {
            get => _paramSearchText;
            set { _paramSearchText = value; OnPropertyChanged(nameof(ParamSearchText)); RefreshAvailableParameterFilter(); }
        }

        public string GridFilterText
        {
            get => _gridFilterText;
            set
            {
                _gridFilterText = value;
                OnPropertyChanged(nameof(GridFilterText));
                ApplyGridRowFilter();
            }
        }

        public string FindText
        {
            get => _findText;
            set { _findText = value; OnPropertyChanged(nameof(FindText)); }
        }

        public string ReplaceText
        {
            get => _replaceText;
            set { _replaceText = value; OnPropertyChanged(nameof(ReplaceText)); }
        }

        public string TargetColumnForFill
        {
            get => _targetColumnForFill;
            set { _targetColumnForFill = value; OnPropertyChanged(nameof(TargetColumnForFill)); }
        }

        public string BulkFillValueText
        {
            get => _bulkFillValueText;
            set { _bulkFillValueText = value; OnPropertyChanged(nameof(BulkFillValueText)); }
        }

        public SheetLinkCategoryItem SelectedSourceItem
        {
            get => _selectedSourceItem;
            set
            {
                if (_selectedSourceItem != value)
                {
                    _selectedSourceItem = value;
                    OnPropertyChanged(nameof(SelectedSourceItem));
                    if (value != null)
                        TriggerExternal(OrSheetLinkActionType.LoadParametersForSelection);
                }
            }
        }

        public SheetLinkParameterColumn SelectedAvailableParameter
        {
            get => _selectedAvailableParameter;
            set { _selectedAvailableParameter = value; OnPropertyChanged(nameof(SelectedAvailableParameter)); }
        }

        public SheetLinkParameterColumn SelectedActiveParameter
        {
            get => _selectedActiveParameter;
            set { _selectedActiveParameter = value; OnPropertyChanged(nameof(SelectedActiveParameter)); }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(nameof(ProgressValue)); OnPropertyChanged(nameof(ProgressText)); }
        }

        public string ProgressText => $"Completed {ProgressValue:0}%";

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        // Commands
        public ICommand RefreshModelCommand { get; }
        public ICommand AddParameterColumnCommand { get; }
        public ICommand RemoveParameterColumnCommand { get; }
        public ICommand AddAllParametersCommand { get; }
        public ICommand ClearParametersCommand { get; }
        public ICommand MoveColumnUpCommand { get; }
        public ICommand MoveColumnDownCommand { get; }
        public ICommand ReloadGridFromRevitCommand { get; }
        public ICommand PushChangesToRevitCommand { get; }
        public ICommand ExportToExcelCsvCommand { get; }
        public ICommand ExportToExcelXmlCommand { get; }
        public ICommand ExportToExcelXlsxCommand { get; }
        public ICommand ImportFromExcelCsvCommand { get; }
        public ICommand AddNewElementRowCommand { get; }
        public ICommand ApplyFindReplaceInGridCommand { get; }
        public ICommand ApplyBulkFillColumnCommand { get; }
        public ICommand SelectAllRowsInRevitCommand { get; }

        public OrSheetLinkViewModel(OrSheetLinkExternalEventHandler handler, ExternalEvent extEvent)
        {
            _handler = handler;
            _externalEvent = extEvent;
            _handler.ViewModel = this;

            RefreshModelCommand = new RelayCommand(_ => TriggerExternal(OrSheetLinkActionType.ScanCategoriesAndSchedules));
            AddParameterColumnCommand = new RelayCommand(_ => AddSelectedParameter());
            RemoveParameterColumnCommand = new RelayCommand(_ => RemoveSelectedParameter());
            AddAllParametersCommand = new RelayCommand(_ => AddAllEditableParameters());
            ClearParametersCommand = new RelayCommand(_ => ClearSelectedParameters());
            MoveColumnUpCommand = new RelayCommand(_ => MoveActiveParameter(-1));
            MoveColumnDownCommand = new RelayCommand(_ => MoveActiveParameter(1));
            ReloadGridFromRevitCommand = new RelayCommand(_ => TriggerExternal(OrSheetLinkActionType.ExtractDataToGrid));
            PushChangesToRevitCommand = new RelayCommand(_ => TriggerExternal(OrSheetLinkActionType.PushGridChangesToRevit));
            ExportToExcelCsvCommand = new RelayCommand(_ => ExportGridToCsv(openAfterExport: true));
            ExportToExcelXmlCommand = new RelayCommand(_ => ExportGridToExcelXml());
            ExportToExcelXlsxCommand = new RelayCommand(_ => ExportGridToNativeXlsx());
            ImportFromExcelCsvCommand = new RelayCommand(_ => ImportGridFromCsv());
            AddNewElementRowCommand = new RelayCommand(_ => ExecuteAddNewElementRow());
            ApplyFindReplaceInGridCommand = new RelayCommand(_ => ExecuteFindReplaceInGrid());
            ApplyBulkFillColumnCommand = new RelayCommand(_ => ExecuteBulkFillColumn());
            SelectAllRowsInRevitCommand = new RelayCommand(_ => TriggerExternal(OrSheetLinkActionType.SelectHighlightedElementsInRevit));
        }

        public void TriggerExternal(OrSheetLinkActionType action)
        {
            _handler.CurrentAction = action;
            _externalEvent.Raise();
        }

        public void UpdateSources(List<SheetLinkCategoryItem> categories, List<SheetLinkCategoryItem> schedules)
        {
            _allCategories = categories ?? new List<SheetLinkCategoryItem>();
            _allSchedules = schedules ?? new List<SheetLinkCategoryItem>();
            RefreshSourceFilter();
        }

        private void RefreshSourceFilter()
        {
            string q = (SourceSearchText ?? string.Empty).Trim().ToLowerInvariant();
            var srcList = IsCategoriesMode ? _allCategories : _allSchedules;

            FilteredSourceItems.Clear();
            foreach (var it in srcList)
            {
                if (string.IsNullOrEmpty(q) || it.Name.ToLowerInvariant().Contains(q))
                    FilteredSourceItems.Add(it);
            }

            if (FilteredSourceItems.Count > 0 && (SelectedSourceItem == null || !FilteredSourceItems.Contains(SelectedSourceItem)))
            {
                var preferred = FilteredSourceItems.FirstOrDefault(x =>
                    x.Name.IndexOf("Sheets", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.Name.IndexOf("Rooms", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.Name.IndexOf("Doors", StringComparison.OrdinalIgnoreCase) >= 0) ?? FilteredSourceItems[0];

                SelectedSourceItem = preferred;
            }
        }

        public void UpdateAvailableParameters(List<SheetLinkParameterColumn> cols, int elemCount)
        {
            _allAvailableParameters = cols ?? new List<SheetLinkParameterColumn>();
            SelectedParameters.Clear();
            EditableColumnNames.Clear();

            foreach (var c in _allAvailableParameters.Where(x => x.IsSelected))
            {
                SelectedParameters.Add(c);
                if (!c.IsReadOnly) EditableColumnNames.Add(c.Name);
            }

            if (EditableColumnNames.Count > 0)
                TargetColumnForFill = EditableColumnNames[0];

            RefreshAvailableParameterFilter();
        }

        private void RefreshAvailableParameterFilter()
        {
            string q = (ParamSearchText ?? string.Empty).Trim().ToLowerInvariant();
            AvailableParameters.Clear();

            foreach (var c in _allAvailableParameters)
            {
                if (SelectedParameters.Contains(c)) continue;
                if (string.IsNullOrEmpty(q) || c.Name.ToLowerInvariant().Contains(q))
                    AvailableParameters.Add(c);
            }
        }

        private void AddSelectedParameter()
        {
            if (SelectedAvailableParameter == null) return;
            var col = SelectedAvailableParameter;
            if (!SelectedParameters.Contains(col))
            {
                col.IsSelected = true;
                SelectedParameters.Add(col);
                if (!col.IsReadOnly && !EditableColumnNames.Contains(col.Name))
                    EditableColumnNames.Add(col.Name);
                RefreshAvailableParameterFilter();
                TriggerExternal(OrSheetLinkActionType.ExtractDataToGrid);
            }
        }

        private void RemoveSelectedParameter()
        {
            if (SelectedActiveParameter == null) return;
            var col = SelectedActiveParameter;
            col.IsSelected = false;
            SelectedParameters.Remove(col);
            EditableColumnNames.Remove(col.Name);
            RefreshAvailableParameterFilter();
            TriggerExternal(OrSheetLinkActionType.ExtractDataToGrid);
        }

        private void AddAllEditableParameters()
        {
            foreach (var col in _allAvailableParameters.Where(x => !x.IsReadOnly))
            {
                if (!SelectedParameters.Contains(col))
                {
                    col.IsSelected = true;
                    SelectedParameters.Add(col);
                    if (!EditableColumnNames.Contains(col.Name))
                        EditableColumnNames.Add(col.Name);
                }
            }
            RefreshAvailableParameterFilter();
            TriggerExternal(OrSheetLinkActionType.ExtractDataToGrid);
        }

        private void ClearSelectedParameters()
        {
            foreach (var col in SelectedParameters) col.IsSelected = false;
            SelectedParameters.Clear();
            EditableColumnNames.Clear();
            RefreshAvailableParameterFilter();
            TriggerExternal(OrSheetLinkActionType.ExtractDataToGrid);
        }

        private void MoveActiveParameter(int delta)
        {
            if (SelectedActiveParameter == null) return;
            int idx = SelectedParameters.IndexOf(SelectedActiveParameter);
            int newIdx = idx + delta;
            if (idx >= 0 && newIdx >= 0 && newIdx < SelectedParameters.Count)
            {
                var item = SelectedActiveParameter;
                SelectedParameters.Move(idx, newIdx);
                SelectedActiveParameter = item;
                TriggerExternal(OrSheetLinkActionType.ExtractDataToGrid);
            }
        }

        public void UpdateDataTable(DataTable dt)
        {
            _liveDataTable = dt ?? new DataTable();
            LiveDataView = _liveDataTable.DefaultView;
            ApplyGridRowFilter();
        }

        private void ApplyGridRowFilter()
        {
            if (_liveDataView == null || _liveDataTable == null) return;
            string q = (GridFilterText ?? string.Empty).Trim().Replace("'", "''");
            if (string.IsNullOrEmpty(q))
            {
                _liveDataView.RowFilter = string.Empty;
            }
            else
            {
                var clauses = new List<string>();
                foreach (DataColumn col in _liveDataTable.Columns)
                {
                    if (col.ColumnName.Contains("[") || col.ColumnName.Contains("]"))
                        continue;
                    clauses.Add($"CONVERT([{col.ColumnName}], 'System.String') LIKE '%{q}%'");
                }
                _liveDataView.RowFilter = clauses.Count > 0 ? string.Join(" OR ", clauses) : string.Empty;
            }
            OnPropertyChanged(nameof(RowCountLabel));
        }

        private void ExecuteFindReplaceInGrid()
        {
            if (_liveDataView == null || string.IsNullOrEmpty(FindText))
            {
                StatusMessage = "Enter text in 'Find' box to replace across the active table.";
                return;
            }

            int count = 0;
            foreach (DataRowView drv in _liveDataView)
            {
                DataRow row = drv.Row;
                foreach (DataColumn col in _liveDataTable.Columns)
                {
                    if (col.ReadOnly) continue;
                    string val = row[col.ColumnName]?.ToString() ?? string.Empty;
                    if (val.IndexOf(FindText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        row[col.ColumnName] = val.Replace(FindText, ReplaceText ?? string.Empty);
                        count++;
                    }
                }
            }
            StatusMessage = $"Replaced {count} occurrences in live table. Click 'Sync Changes to Revit' to commit.";
        }

        private void ExecuteBulkFillColumn()
        {
            if (_liveDataView == null || string.IsNullOrWhiteSpace(TargetColumnForFill) || !_liveDataTable.Columns.Contains(TargetColumnForFill))
            {
                StatusMessage = "Please select a valid editable column to bulk fill.";
                return;
            }

            DataColumn col = _liveDataTable.Columns[TargetColumnForFill];
            if (col.ReadOnly)
            {
                StatusMessage = $"Column '{TargetColumnForFill}' is Read-Only.";
                return;
            }

            int filled = 0;
            int idx = 1;
            foreach (DataRowView drv in _liveDataView)
            {
                string val = (BulkFillValueText ?? string.Empty).Replace("{#}", idx.ToString("D2", CultureInfo.InvariantCulture));
                drv.Row[TargetColumnForFill] = val;
                filled++;
                idx++;
            }
            StatusMessage = $"Filled {filled} rows in column '{TargetColumnForFill}'. Click 'Sync Changes to Revit' to commit.";
        }

        private void ExportGridToCsv(bool openAfterExport)
        {
            if (_liveDataTable == null || _liveDataTable.Columns.Count == 0)
            {
                StatusMessage = "No table data to export.";
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                string catName = SelectedSourceItem?.Name ?? "ModelData";
                foreach (char c in Path.GetInvalidFileNameChars()) catName = catName.Replace(c, '_');

                sfd.Title = "OR SheetLink — Export to Excel CSV (UTF-8 BOM)";
                sfd.Filter = "Excel CSV File (*.csv)|*.csv";
                sfd.FileName = $"OR_SheetLink_{catName}_{DateTime.Now:yyyyMMdd_HHmm}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    var headers = _liveDataTable.Columns.Cast<DataColumn>().Select(c => EscapeCsv(c.ColumnName));
                    sb.AppendLine(string.Join(",", headers));

                    foreach (DataRowView drv in _liveDataView)
                    {
                        var cells = _liveDataTable.Columns.Cast<DataColumn>().Select(c => EscapeCsv(drv.Row[c.ColumnName]?.ToString() ?? string.Empty));
                        sb.AppendLine(string.Join(",", cells));
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                    ProgressValue = 100;
                    StatusMessage = $"Exported {_liveDataView.Count} rows to {sfd.FileName}";

                    if (openAfterExport)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = sfd.FileName,
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }
                }
            }
        }

        private void ExportGridToExcelXml()
        {
            if (_liveDataTable == null || _liveDataTable.Columns.Count == 0) return;

            using (var sfd = new SaveFileDialog())
            {
                string catName = SelectedSourceItem?.Name ?? "ModelData";
                foreach (char c in Path.GetInvalidFileNameChars()) catName = catName.Replace(c, '_');

                sfd.Title = "OR SheetLink — Export Styled Excel Workbook (.xls XML)";
                sfd.Filter = "Microsoft Excel Workbook (*.xls)|*.xls";
                sfd.FileName = $"OR_SheetLink_{catName}_{DateTime.Now:yyyyMMdd_HHmm}.xls";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("<?xml version='1.0' encoding='UTF-8'?>");
                    sb.AppendLine("<?mso-application progid='Excel.Sheet'?>");
                    sb.AppendLine("<Workbook xmlns='urn:schemas-microsoft-com:office:spreadsheet' xmlns:ss='urn:schemas-microsoft-com:office:spreadsheet'>");
                    sb.AppendLine("<Styles>");
                    sb.AppendLine("<Style ss:ID='Header'><Font ss:Bold='1' ss:Color='#FFFFFF'/><Interior ss:Color='#0F172A' ss:Pattern='Solid'/></Style>");
                    sb.AppendLine("<Style ss:ID='ReadOnlyCell'><Font ss:Color='#475569'/><Interior ss:Color='#F1F5F9' ss:Pattern='Solid'/></Style>");
                    sb.AppendLine("<Style ss:ID='EditableCell'><Font ss:Color='#0F172A'/><Interior ss:Color='#FFFFFF' ss:Pattern='Solid'/></Style>");
                    sb.AppendLine("</Styles>");
                    sb.AppendLine($"<Worksheet ss:Name='{EscapeXml(catName)}'><Table>");

                    sb.Append("<Row>");
                    foreach (DataColumn col in _liveDataTable.Columns)
                        sb.Append($"<Cell ss:StyleID='Header'><Data ss:Type='String'>{EscapeXml(col.ColumnName)}</Data></Cell>");
                    sb.AppendLine("</Row>");

                    foreach (DataRowView drv in _liveDataView)
                    {
                        sb.Append("<Row>");
                        foreach (DataColumn col in _liveDataTable.Columns)
                        {
                            string stId = col.ReadOnly ? "ReadOnlyCell" : "EditableCell";
                            string val = drv.Row[col.ColumnName]?.ToString() ?? string.Empty;
                            sb.Append($"<Cell ss:StyleID='{stId}'><Data ss:Type='String'>{EscapeXml(val)}</Data></Cell>");
                        }
                        sb.AppendLine("</Row>");
                    }

                    sb.AppendLine("</Table></Worksheet></Workbook>");
                    File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                    ProgressValue = 100;
                    StatusMessage = $"Exported styled Excel workbook to {sfd.FileName}";

                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }
        }

        private void ExecuteAddNewElementRow()
        {
            if (_liveDataTable == null || _liveDataTable.Columns.Count == 0)
            {
                StatusMessage = "Please select a Category (e.g. Sheets or Rooms) before adding a new row.";
                return;
            }

            DataRow newRow = _liveDataTable.NewRow();
            newRow["ElementId"] = "NEW";
            newRow["Category"] = SelectedSourceItem?.Name ?? "New Element";
            newRow["Family & Type"] = "Placeholder / Unplaced";
            _liveDataTable.Rows.Add(newRow);
            OnPropertyChanged(nameof(RowCountLabel));
            StatusMessage = "Added 'NEW' row at the bottom of the grid. Fill its parameters (e.g. Sheet Number / Name or Room Number / Name) and click Sync!";
        }

        private void ExportGridToNativeXlsx()
        {
            if (_liveDataTable == null || _liveDataTable.Columns.Count == 0)
            {
                StatusMessage = "No table data to export.";
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                string catName = SelectedSourceItem?.Name ?? "ModelData";
                foreach (char c in Path.GetInvalidFileNameChars()) catName = catName.Replace(c, '_');

                sfd.Title = "GOAT SHEETLINK — Export Native OpenXML Workbook (.xlsx)";
                sfd.Filter = "Microsoft Excel OpenXML Workbook (*.xlsx)|*.xlsx";
                sfd.FileName = $"GOAT_SheetLink_{catName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // Build (Instances) and (Types) GoatExcelSheet objects
                    var instSheet = new GoatExcelSheet { Name = $"{catName} (Instances)" };
                    var instHeaders = new List<GoatExcelCell>();
                    foreach (DataColumn dc in _liveDataTable.Columns)
                    {
                        instHeaders.Add(new GoatExcelCell(dc.ColumnName, GoatCellStyle.HeaderNavy));
                        instSheet.ColumnWidths.Add(dc.ColumnName == "Family & Type" ? 28 : 18);
                    }
                    instSheet.Rows.Add(instHeaders);

                    foreach (DataRowView drv in _liveDataView)
                    {
                        var rCells = new List<GoatExcelCell>();
                        foreach (DataColumn dc in _liveDataTable.Columns)
                        {
                            string v = drv.Row[dc.ColumnName]?.ToString() ?? string.Empty;
                            var st = dc.ReadOnly ? GoatCellStyle.ReadOnlyGrey : GoatCellStyle.NormalEditable;
                            rCells.Add(new GoatExcelCell(v, st));
                        }
                        instSheet.Rows.Add(rCells);
                    }

                    var typeSheet = new GoatExcelSheet { Name = $"{catName} (Types)" };
                    var typeCols = _liveDataTable.Columns.Cast<DataColumn>()
                        .Where(c => c.ColumnName.StartsWith("[Type] ", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var typeHeaders = new List<GoatExcelCell>
                    {
                        new GoatExcelCell("Family & Type", GoatCellStyle.HeaderTeal),
                        new GoatExcelCell("Instance Count", GoatCellStyle.HeaderTeal)
                    };
                    typeSheet.ColumnWidths.Add(30);
                    typeSheet.ColumnWidths.Add(16);
                    foreach (var tc in typeCols)
                    {
                        typeHeaders.Add(new GoatExcelCell(tc.ColumnName, GoatCellStyle.HeaderTeal));
                        typeSheet.ColumnWidths.Add(20);
                    }
                    typeSheet.Rows.Add(typeHeaders);

                    var groupedByType = _liveDataTable.AsEnumerable()
                        .GroupBy(r => r["Family & Type"]?.ToString() ?? "-");

                    foreach (var g in groupedByType)
                    {
                        var trCells = new List<GoatExcelCell>
                        {
                            new GoatExcelCell(g.Key, GoatCellStyle.ReadOnlyGrey),
                            new GoatExcelCell(g.Count().ToString(CultureInfo.InvariantCulture), GoatCellStyle.ReadOnlyGrey, isNumeric: true)
                        };
                        DataRow first = g.First();
                        foreach (var tc in typeCols)
                        {
                            string tv = first[tc.ColumnName]?.ToString() ?? string.Empty;
                            trCells.Add(new GoatExcelCell(tv, tc.ReadOnly ? GoatCellStyle.ReadOnlyGrey : GoatCellStyle.NormalEditable));
                        }
                        typeSheet.Rows.Add(trCells);
                    }

                    GoatExcelEngine.WriteXlsx(sfd.FileName, new List<GoatExcelSheet> { instSheet, typeSheet });
                    ProgressValue = 100;
                    StatusMessage = $"Exported Native .XLSX with (Instances) & (Types) sheets to {sfd.FileName}";

                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }
        }

        private void ImportGridFromCsv()
        {
            if (_liveDataTable == null || _liveDataTable.Columns.Count == 0)
            {
                StatusMessage = "Please load a Category or Schedule into the grid first before importing.";
                return;
            }

            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "GOAT SHEETLINK — Import Modified Excel (.XLSX or .CSV) File";
                ofd.Filter = "Excel & CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|All Files (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (ofd.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        var sheets = GoatExcelEngine.ReadXlsx(ofd.FileName);
                        if (sheets.Count == 0)
                        {
                            StatusMessage = "No worksheets found in the selected .XLSX file.";
                            return;
                        }

                        var firstSheetRows = sheets.Values.First();
                        if (firstSheetRows.Count < 2)
                        {
                            StatusMessage = "Selected .XLSX worksheet has no data rows.";
                            return;
                        }

                        var xlsxHeaders = firstSheetRows[0];
                        int idColXlsx = xlsxHeaders.FindIndex(h => h.Equals("ElementId", StringComparison.OrdinalIgnoreCase));
                        int updatedCellsXlsx = 0;
                        int newRowsXlsx = 0;

                        var rowMapXlsx = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
                        foreach (DataRow r in _liveDataTable.Rows)
                        {
                            string eid = r["ElementId"]?.ToString();
                            if (!string.IsNullOrEmpty(eid) && !eid.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                                rowMapXlsx[eid] = r;
                        }

                        for (int rIdx = 1; rIdx < firstSheetRows.Count; rIdx++)
                        {
                            var vals = firstSheetRows[rIdx];
                            string eid = (idColXlsx >= 0 && idColXlsx < vals.Count) ? vals[idColXlsx]?.Trim() : string.Empty;
                            DataRow targetRow = null;

                            if (string.IsNullOrEmpty(eid) || eid.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                            {
                                targetRow = _liveDataTable.NewRow();
                                targetRow["ElementId"] = "NEW";
                                targetRow["Category"] = SelectedSourceItem?.Name ?? "New Element";
                                targetRow["Family & Type"] = "Placeholder / Unplaced";
                                _liveDataTable.Rows.Add(targetRow);
                                newRowsXlsx++;
                            }
                            else if (!rowMapXlsx.TryGetValue(eid, out targetRow))
                            {
                                continue;
                            }

                            for (int c = 0; c < xlsxHeaders.Count && c < vals.Count; c++)
                            {
                                string colName = xlsxHeaders[c];
                                if (colName.Equals("ElementId", StringComparison.OrdinalIgnoreCase)) continue;
                                if (!_liveDataTable.Columns.Contains(colName)) continue;
                                DataColumn dc = _liveDataTable.Columns[colName];
                                if (dc.ReadOnly) continue;

                                string newVal = vals[c] ?? string.Empty;
                                if (!string.Equals(targetRow[colName]?.ToString() ?? string.Empty, newVal, StringComparison.Ordinal))
                                {
                                    targetRow[colName] = newVal;
                                    updatedCellsXlsx++;
                                }
                            }
                        }

                        OnPropertyChanged(nameof(RowCountLabel));
                        ProgressValue = 100;
                        StatusMessage = $"Imported {updatedCellsXlsx} modified cells and {newRowsXlsx} new rows from .XLSX! Click 'Sync Changes to Revit' to commit.";
                        return;
                    }

                    var lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8);
                    if (lines.Length < 2)
                    {
                        StatusMessage = "Selected CSV file is empty or has no data rows.";
                        return;
                    }

                    var headers = ParseCsvLine(lines[0]);
                    int idColIdx = headers.FindIndex(h => h.Equals("ElementId", StringComparison.OrdinalIgnoreCase));
                    if (idColIdx < 0)
                    {
                        MessageBox.Show("CSV file must contain an 'ElementId' column to match elements accurately.", "OR SHEETLINK", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var rowMap = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataRow r in _liveDataTable.Rows)
                    {
                        string eid = r["ElementId"]?.ToString();
                        if (!string.IsNullOrEmpty(eid)) rowMap[eid] = r;
                    }

                    int updatedCells = 0;
                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        var vals = ParseCsvLine(lines[i]);
                        if (idColIdx >= vals.Count) continue;

                        string eid = vals[idColIdx];
                        if (!rowMap.TryGetValue(eid, out DataRow targetRow)) continue;

                        for (int c = 0; c < headers.Count && c < vals.Count; c++)
                        {
                            string colName = headers[c];
                            if (colName.Equals("ElementId", StringComparison.OrdinalIgnoreCase)) continue;
                            if (!_liveDataTable.Columns.Contains(colName)) continue;
                            DataColumn dc = _liveDataTable.Columns[colName];
                            if (dc.ReadOnly) continue;

                            string newVal = vals[c];
                            if (!string.Equals(targetRow[colName]?.ToString() ?? string.Empty, newVal, StringComparison.Ordinal))
                            {
                                targetRow[colName] = newVal;
                                updatedCells++;
                            }
                        }
                    }

                    ProgressValue = 100;
                    StatusMessage = $"Imported {updatedCells} modified cells from CSV into preview grid. Click 'Sync Changes to Revit' to commit to model!";
                }
            }
        }

        public List<ElementId> GetHighlightedElementIds()
        {
            var list = new List<ElementId>();
            if (_liveDataView == null) return list;
            foreach (DataRowView drv in _liveDataView)
            {
                string s = drv.Row["ElementId"]?.ToString();
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v))
                    list.Add(new ElementId(v));
            }
            return list;
        }

        private static string EscapeCsv(string val)
        {
            if (val == null) return string.Empty;
            char quoteChar = '"';
            string qStr = quoteChar.ToString();
            string doubleQStr = new string(quoteChar, 2);
            if (val.Contains(",") || val.Contains(qStr) || val.Contains("\n") || val.Contains("\r"))
            {
                return qStr + val.Replace(qStr, doubleQStr) + qStr;
            }
            return val;
        }

        private static string EscapeXml(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            char quoteChar = '"';
            return val
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace(quoteChar.ToString(), "&quot;")
                .Replace("'", "&apos;");
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;
            var cur = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else cur.Append(ch);
                }
                else
                {
                    if (ch == '"') inQuotes = true;
                    else if (ch == ',') { result.Add(cur.ToString()); cur.Clear(); }
                    else cur.Append(ch);
                }
            }
            result.Add(cur.ToString());
            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
