using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;

namespace ORTools.UI
{
    public class SheetExportItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _customFileName;
        private string _paperSize = "A0";
        private string _orientation = "Landscape";
        private string _status = "Queued";

        public ElementId SheetId { get; set; }
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string Revision { get; set; }
        public string ProjectNumber { get; set; }
        public Action OnChangedCallback { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public string PaperSize
        {
            get => _paperSize;
            set 
            { 
                if (_paperSize != value)
                {
                    _paperSize = value; 
                    OnPropertyChanged(nameof(PaperSize)); 
                    OnChangedCallback?.Invoke();
                }
            }
        }

        public string Orientation
        {
            get => _orientation;
            set 
            { 
                if (_orientation != value)
                {
                    _orientation = value; 
                    OnPropertyChanged(nameof(Orientation)); 
                    OnChangedCallback?.Invoke();
                }
            }
        }

        public string CustomFileName
        {
            get => _customFileName;
            set { _customFileName = value; OnPropertyChanged(nameof(CustomFileName)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class OrSheetsWindow : Window
    {
        private readonly Document _doc;
        private readonly List<SheetExportItem> _allSheets = new List<SheetExportItem>();
        public ObservableCollection<SheetExportItem> FilteredSheets { get; } = new ObservableCollection<SheetExportItem>();
        private string _projectNumber = "PRJ";

        public OrSheetsWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OR_Sheets_Exports");
            TxtOutputFolder.Text = defaultDir;
            TxtFilenameFormula.Text = "{ProjectNumber}-{SheetNumber}-{Revision}-{SheetName}";

            if (_doc.ProjectInformation != null && !string.IsNullOrWhiteSpace(_doc.ProjectInformation.Number))
                _projectNumber = _doc.ProjectInformation.Number.Trim();

            LoadProjectSheets();
            DgSelection.ItemsSource = FilteredSheets;
            DgQueue.ItemsSource = FilteredSheets;
            UpdateNamesAndSummary();
        }

        private void LoadProjectSheets()
        {
            _allSheets.Clear();
            var sheets = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsPlaceholder && !s.IsTemplate)
                .OrderBy(s => s.SheetNumber)
                .ToList();

            foreach (var sh in sheets)
            {
                string rev = "R00";
                try
                {
                    ElementId revId = sh.GetCurrentRevision();
                    if (revId != ElementId.InvalidElementId)
                    {
                        Revision r = _doc.GetElement(revId) as Revision;
                        if (r != null) rev = r.RevisionNumber ?? r.SequenceNumber.ToString();
                    }
                }
                catch { }

                DetectSizeAndOrientation(sh, out string size, out string orient);

                var item = new SheetExportItem
                {
                    SheetId = sh.Id,
                    SheetNumber = sh.SheetNumber ?? "S00",
                    SheetName = sh.Name ?? "Unnamed",
                    Revision = rev,
                    ProjectNumber = _projectNumber,
                    PaperSize = size,
                    Orientation = orient,
                    IsSelected = true,
                    OnChangedCallback = () => UpdateNamesAndSummary()
                };
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SheetExportItem.IsSelected))
                        UpdateSummaryCount();
                };
                _allSheets.Add(item);
            }

            ApplySearchFilter();
        }

        private void DetectSizeAndOrientation(ViewSheet sheet, out string size, out string orient)
        {
            size = "A0";
            orient = "Landscape";
            try
            {
                var tb = new FilteredElementCollector(_doc, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .FirstElement();
                if (tb != null)
                {
                    Parameter pW = tb.get_Parameter(BuiltInParameter.SHEET_WIDTH);
                    Parameter pH = tb.get_Parameter(BuiltInParameter.SHEET_HEIGHT);
                    if (pW != null && pH != null)
                    {
                        double wMm = pW.AsDouble() * 304.8;
                        double hMm = pH.AsDouble() * 304.8;
                        orient = (wMm >= hMm) ? "Landscape" : "Portrait";
                        double maxDim = Math.Max(wMm, hMm);
                        if (maxDim >= 1100) size = "A0";
                        else if (maxDim >= 780) size = "A1";
                        else if (maxDim >= 550) size = "A2";
                        else if (maxDim >= 380) size = "A3";
                        else size = "A4";
                    }
                }
            }
            catch { }
        }

        private void ApplySearchFilter()
        {
            if (FilteredSheets == null) return;
            string q = (TxtSearch?.Text ?? "").Trim().ToLowerInvariant();
            FilteredSheets.Clear();
            foreach (var it in _allSheets)
            {
                if (string.IsNullOrEmpty(q) ||
                    it.SheetNumber.ToLowerInvariant().Contains(q) ||
                    it.SheetName.ToLowerInvariant().Contains(q))
                {
                    FilteredSheets.Add(it);
                }
            }
            UpdateSummaryCount();
        }

        private void UpdateNamesAndSummary()
        {
            if (TxtFilenameFormula == null) return;
            string formula = TxtFilenameFormula.Text;
            if (string.IsNullOrWhiteSpace(formula)) formula = "{SheetNumber}-{SheetName}";

            foreach (var it in _allSheets)
            {
                string raw = formula
                    .Replace("{ProjectNumber}", it.ProjectNumber)
                    .Replace("{SheetNumber}", it.SheetNumber)
                    .Replace("{SheetName}", it.SheetName)
                    .Replace("{Revision}", it.Revision)
                    .Replace("{Size}", it.PaperSize)
                    .Replace("{Orientation}", it.Orientation);

                foreach (char c in Path.GetInvalidFileNameChars())
                    raw = raw.Replace(c, '_');

                it.CustomFileName = raw.Trim();
            }
            UpdateSummaryCount();
        }

        private void UpdateSummaryCount()
        {
            if (LblSummaryCount == null) return;
            int sel = _allSheets.Count(x => x.IsSelected);
            LblSummaryCount.Text = $"{sel} of {_allSheets.Count} sheets selected · Unlimited Export Active";
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var it in FilteredSheets) it.IsSelected = true;
            UpdateSummaryCount();
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var it in FilteredSheets) it.IsSelected = false;
            UpdateSummaryCount();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplySearchFilter();
        private void TxtFilenameFormula_TextChanged(object sender, TextChangedEventArgs e) => UpdateNamesAndSummary();

        private void BtnAddToken_Click(object sender, RoutedEventArgs e)
        {
            TxtFilenameFormula.Text += "-{Size}";
        }

        private void CbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChkPdf == null) return;
            int idx = CbProfiles.SelectedIndex;
            if (idx == 0)
            {
                ChkPdf.IsChecked = true; ChkDwg.IsChecked = true;
                ChkNwc.IsChecked = false; ChkIfc.IsChecked = false;
                RbSeparateFiles.IsChecked = true;
            }
            else if (idx == 1)
            {
                ChkPdf.IsChecked = true; ChkDwg.IsChecked = false;
                RbCombinePdf.IsChecked = true;
            }
            else if (idx == 2)
            {
                ChkPdf.IsChecked = true; ChkDwg.IsChecked = true;
                ChkDwf.IsChecked = true; ChkDocRegister.IsChecked = true;
                RbSeparateFiles.IsChecked = true;
            }
        }

        private void CbOverrideSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbOverrideSize.SelectedIndex <= 0) return;
            string sz = ((CbOverrideSize.SelectedItem as ComboBoxItem)?.Content)?.ToString();
            foreach (var it in _allSheets.Where(x => x.IsSelected)) it.PaperSize = sz;
            UpdateNamesAndSummary();
        }

        private void CbOverrideOrientation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbOverrideOrientation.SelectedIndex <= 0) return;
            string ori = ((CbOverrideOrientation.SelectedItem as ComboBoxItem)?.Content)?.ToString();
            foreach (var it in _allSheets.Where(x => x.IsSelected)) it.Orientation = ori;
            UpdateNamesAndSummary();
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = "Select Root Export Folder for OR SHEETS Pro";
                dlg.SelectedPath = TxtOutputFolder.Text;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TxtOutputFolder.Text = dlg.SelectedPath;
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnExportNow_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allSheets.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one sheet to export.", "OR SHEETS Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rootFolder = TxtOutputFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(rootFolder))
            {
                MessageBox.Show("Please specify a valid destination folder.", "OR SHEETS Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Directory.CreateDirectory(rootFolder);
            bool splitByFormat = RbSplitFolders.IsChecked == true;

            string GetFormatDir(string fmt)
            {
                string dir = splitByFormat ? Path.Combine(rootFolder, fmt) : rootFolder;
                Directory.CreateDirectory(dir);
                return dir;
            }

            MainTabs.SelectedIndex = 2;
            PbExport.Value = 0;

            int done = 0;
            int total = selected.Count;

            if (ChkPdf.IsChecked == true && RbCombinePdf.IsChecked == true)
            {
                try
                {
                    string pdfDir = GetFormatDir("PDF");
                    PDFExportOptions pdfOpts = BuildPdfOptions();
                    pdfOpts.Combine = true;
                    pdfOpts.FileName = $"{_projectNumber}_Combined_Sheets_{DateTime.Now:yyyyMMdd_HHmm}";
                    IList<ElementId> ids = selected.Select(s => s.SheetId).ToList();
                    _doc.Export(pdfDir, ids, pdfOpts);
                }
                catch { }
            }

            foreach (var item in selected)
            {
                IList<ElementId> singleId = new List<ElementId> { item.SheetId };

                if (ChkPdf.IsChecked == true && RbSeparateFiles.IsChecked == true)
                {
                    try
                    {
                        string pdfDir = GetFormatDir("PDF");
                        PDFExportOptions pdfOpts = BuildPdfOptions();
                        pdfOpts.Combine = false;
                        pdfOpts.FileName = item.CustomFileName;
                        _doc.Export(pdfDir, singleId, pdfOpts);
                    }
                    catch { }
                }

                if (ChkDwg.IsChecked == true)
                {
                    try
                    {
                        string dwgDir = GetFormatDir("DWG");
                        DWGExportOptions dwgOpts = new DWGExportOptions { MergedViews = true };
                        _doc.Export(dwgDir, item.CustomFileName, singleId, dwgOpts);
                    }
                    catch { }
                }

                if (ChkDgn.IsChecked == true)
                {
                    try
                    {
                        string dgnDir = GetFormatDir("DGN");
                        DGNExportOptions dgnOpts = new DGNExportOptions();
                        _doc.Export(dgnDir, item.CustomFileName, singleId, dgnOpts);
                    }
                    catch { }
                }

                if (ChkDwf.IsChecked == true)
                {
                    try
                    {
                        string dwfDir = GetFormatDir("DWF");
                        DWFXExportOptions dwfOpts = new DWFXExportOptions();
                        ViewSet vs = new ViewSet();
                        View vw = _doc.GetElement(item.SheetId) as View;
                        if (vw != null) vs.Insert(vw);
                        _doc.Export(dwfDir, item.CustomFileName, vs, dwfOpts);
                    }
                    catch { }
                }

                if (ChkImg.IsChecked == true)
                {
                    try
                    {
                        string imgDir = GetFormatDir("IMG");
                        ImageExportOptions imgOpts = new ImageExportOptions
                        {
                            ExportRange = ExportRange.SetOfViews,
                            FilePath = Path.Combine(imgDir, item.CustomFileName),
                            HLRandWFViewsFileType = ImageFileType.PNG,
                            ShadowViewsFileType = ImageFileType.PNG,
                            ZoomType = ZoomFitType.FitToPage,
                            PixelSize = 2400
                        };
                        imgOpts.SetViewsAndSheets(singleId);
                        _doc.ExportImage(imgOpts);
                    }
                    catch { }
                }

                done++;
                item.Status = "Exported (100%)";
                PbExport.Value = (double)done / total * 100.0;
            }

            if (ChkDocRegister.IsChecked == true)
            {
                try
                {
                    string regDir = GetFormatDir("DocRegister");
                    string csvPath = Path.Combine(regDir, $"DocRegister_{_projectNumber}_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                    var lines = new List<string>
                    {
                        "Project Number,Sheet Number,Sheet Name,Revision,Paper Size,Orientation,ISO 19650 File Name,Export Timestamp"
                    };
                    string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    foreach (var it in selected)
                    {
                        lines.Add($"\"{it.ProjectNumber}\",\"{it.SheetNumber}\",\"{it.SheetName}\",\"{it.Revision}\",\"{it.PaperSize}\",\"{it.Orientation}\",\"{it.CustomFileName}\",\"{ts}\"");
                    }
                    File.WriteAllLines(csvPath, lines, System.Text.Encoding.UTF8);
                }
                catch { }
            }

            LblProgress.Text = $"Completed 100% — Exported {selected.Count} sheets to {rootFolder}";
            MessageBox.Show($"Successfully exported {selected.Count} sheets!\n\nOutput Folder:\n{rootFolder}", "OR SHEETS Pro — Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private PDFExportOptions BuildPdfOptions()
        {
            PDFExportOptions opts = new PDFExportOptions
            {
                PaperPlacement = (RbCenter.IsChecked == true) ? PaperPlacementType.Center : PaperPlacementType.LowerLeft,
                ZoomType = (RbZoom100.IsChecked == true) ? ZoomType.Zoom : ZoomType.FitToPage,
                ZoomPercentage = 100,
                HideCropBoundaries = ChkHideCrop.IsChecked == true,
                HideReferencePlane = ChkHideRef.IsChecked == true,
                HideScopeBoxes = ChkHideScope.IsChecked == true,
                HideUnreferencedViewTags = ChkHideUnref.IsChecked == true,
                MaskCoincidentLines = ChkMaskCoincident.IsChecked == true,
                ReplaceHalftoneWithThinLines = ChkHalftoneThin.IsChecked == true,
                ViewLinksInBlue = ChkLinksBlue.IsChecked == true
            };

            if (CbColors.SelectedIndex == 1) opts.ColorDepth = ColorDepthType.GrayScale;
            else if (CbColors.SelectedIndex == 2) opts.ColorDepth = ColorDepthType.BlackLine;
            else opts.ColorDepth = ColorDepthType.Color;

            if (CbRasterQuality.SelectedIndex == 0) opts.RasterQuality = RasterQualityType.Low;
            else if (CbRasterQuality.SelectedIndex == 1) opts.RasterQuality = RasterQualityType.Medium;
            else if (CbRasterQuality.SelectedIndex == 3) opts.RasterQuality = RasterQualityType.Presentation;
            else opts.RasterQuality = RasterQualityType.High;

            return opts;
        }
    }
}
