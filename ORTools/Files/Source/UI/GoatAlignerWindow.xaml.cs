using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Models;

namespace ORTools.UI
{
    public enum GoatAlignerMode
    {
        Scan,
        AutoPackSheets,
        MatchPositionAcrossSheets,
        DuplicateSheetsWithViews
    }

    public class AlignSheetItem
    {
        public bool IsSelected { get; set; }
        public ElementId SheetId { get; set; } = ElementId.InvalidElementId;
        public string SheetNumber { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
    }

    public class ReferencePlacedItem
    {
        public ElementId PlacedElementId { get; set; } = ElementId.InvalidElementId;
        public ElementId UnderlyingViewOrSchedId { get; set; } = ElementId.InvalidElementId;
        public string DisplayTitle { get; set; } = string.Empty;
        public XYZ CenterPoint { get; set; } = XYZ.Zero;
        public bool IsScheduleInstance { get; set; }
        public bool IsLegendViewport { get; set; }
        public override string ToString() => DisplayTitle;
    }

    public class GoatAlignerEventHandler : IExternalEventHandler
    {
        public GoatAlignerWindow Window { get; set; }
        public GoatAlignerMode Mode { get; set; } = GoatAlignerMode.Scan;

        public void Execute(UIApplication uiapp)
        {
            if (Window == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc?.Document == null) return;
            Document doc = uidoc.Document;

            try
            {
                switch (Mode)
                {
                    case GoatAlignerMode.Scan:
                        ExecuteScan(doc);
                        break;
                    case GoatAlignerMode.AutoPackSheets:
                        ExecuteAutoPack(doc);
                        break;
                    case GoatAlignerMode.MatchPositionAcrossSheets:
                        ExecuteMatchPosition(doc);
                        break;
                    case GoatAlignerMode.DuplicateSheetsWithViews:
                        ExecuteDuplicateSheets(doc);
                        break;
                }
            }
            catch (Exception ex)
            {
                Window.TxtStatus.Text = $"Error: {ex.Message}";
            }
        }

        public string GetName() => "GOAT ALIGNER External Event Handler";

        private void ExecuteScan(Document doc)
        {
            ElementId activeSheetId = (doc.ActiveView is ViewSheet avs) ? avs.Id : ElementId.InvalidElementId;

            var sheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsPlaceholder)
                .OrderBy(s => s.SheetNumber)
                .Select(s => new AlignSheetItem
                {
                    IsSelected = s.Id == activeSheetId,
                    SheetId = s.Id,
                    SheetNumber = s.SheetNumber,
                    SheetName = s.Name
                })
                .ToList();

            if (sheets.Count > 0 && !sheets.Any(s => s.IsSelected))
                sheets[0].IsSelected = true;

            var refItems = new List<ReferencePlacedItem>();
            foreach (ViewSheet s in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(x => !x.IsPlaceholder).OrderBy(x => x.SheetNumber))
            {
                foreach (ElementId vpId in s.GetAllViewports())
                {
                    if (doc.GetElement(vpId) is Viewport vp && doc.GetElement(vp.ViewId) is View v)
                    {
                        refItems.Add(new ReferencePlacedItem
                        {
                            PlacedElementId = vp.Id,
                            UnderlyingViewOrSchedId = v.Id,
                            DisplayTitle = $"[{s.SheetNumber}] Viewport: {v.Name}",
                            CenterPoint = vp.GetBoxCenter(),
                            IsLegendViewport = v.ViewType == ViewType.Legend
                        });
                    }
                }

                var schedulesOnSheet = new FilteredElementCollector(doc, s.Id)
                    .OfClass(typeof(ScheduleSheetInstance))
                    .Cast<ScheduleSheetInstance>()
                    .Where(si => !si.IsTitleblockRevisionSchedule);

                foreach (ScheduleSheetInstance ssi in schedulesOnSheet)
                {
                    refItems.Add(new ReferencePlacedItem
                    {
                        PlacedElementId = ssi.Id,
                        UnderlyingViewOrSchedId = ssi.ScheduleId,
                        DisplayTitle = $"[{s.SheetNumber}] Schedule: {ssi.Name}",
                        CenterPoint = ssi.Point,
                        IsScheduleInstance = true
                    });
                }
            }

            Window.ApplyScannedData(sheets, refItems);
        }

        private void ExecuteAutoPack(Document doc)
        {
            var selectedSheets = Window.SheetList.Where(s => s.IsSelected).ToList();
            if (selectedSheets.Count == 0 && doc.ActiveView is ViewSheet activeSheet)
            {
                selectedSheets.Add(new AlignSheetItem { SheetId = activeSheet.Id });
            }

            if (selectedSheets.Count == 0)
            {
                Window.TxtStatus.Text = "Please select at least one sheet on the left.";
                return;
            }

            int packedViewports = 0;
            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ALIGNER — Auto-Pack Sheets"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Auto-Pack Sheet Viewports"))
                {
                    trans.Start();
                    foreach (var item in selectedSheets)
                    {
                        if (doc.GetElement(item.SheetId) is ViewSheet vs)
                        {
                            packedViewports += GoatDocEngines.PackViewsOntoSheet(doc, vs, null, packExistingViewportsOnly: true);
                        }
                    }
                    trans.Commit();
                }
                tg.Assimilate();
            }

            Window.TxtStatus.Text = $"✔ Auto-packed {packedViewports} viewports across {selectedSheets.Count} sheet(s). (1x Ctrl+Z to Undo)";
        }

        private void ExecuteMatchPosition(Document doc)
        {
            var refItem = Window.CbReferenceViewports.SelectedItem as ReferencePlacedItem;
            if (refItem == null)
            {
                Window.TxtStatus.Text = "Please select a Reference Viewport or Schedule first.";
                return;
            }

            var targetSheets = Window.SheetList.Where(s => s.IsSelected).ToList();
            int alignedCount = 0;

            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ALIGNER — Match Position Across Sheets"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Match Viewport & Schedule Coordinates"))
                {
                    trans.Start();

                    foreach (var item in targetSheets)
                    {
                        if (!(doc.GetElement(item.SheetId) is ViewSheet sheet)) continue;

                        if (refItem.IsScheduleInstance)
                        {
                            // Place or move ScheduleSheetInstance on target sheet
                            var existing = new FilteredElementCollector(doc, sheet.Id)
                                .OfClass(typeof(ScheduleSheetInstance))
                                .Cast<ScheduleSheetInstance>()
                                .FirstOrDefault(x => x.ScheduleId == refItem.UnderlyingViewOrSchedId);

                            if (existing != null)
                            {
                                existing.Point = refItem.CenterPoint;
                                alignedCount++;
                            }
                            else
                            {
                                try
                                {
                                    ScheduleSheetInstance.Create(doc, sheet.Id, refItem.UnderlyingViewOrSchedId, refItem.CenterPoint);
                                    alignedCount++;
                                }
                                catch { }
                            }
                        }
                        else if (refItem.IsLegendViewport)
                        {
                            // Place or move Legend Viewport on target sheet
                            Viewport existingLegendVp = sheet.GetAllViewports()
                                .Select(id => doc.GetElement(id) as Viewport)
                                .FirstOrDefault(vp => vp != null && vp.ViewId == refItem.UnderlyingViewOrSchedId);

                            if (existingLegendVp != null)
                            {
                                existingLegendVp.SetBoxCenter(refItem.CenterPoint);
                                alignedCount++;
                            }
                            else if (Viewport.CanAddViewToSheet(doc, sheet.Id, refItem.UnderlyingViewOrSchedId))
                            {
                                try
                                {
                                    Viewport.Create(doc, sheet.Id, refItem.UnderlyingViewOrSchedId, refItem.CenterPoint);
                                    alignedCount++;
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            // Align the primary model viewport on target sheet to the exact Reference CenterPoint
                            Viewport primaryVp = sheet.GetAllViewports()
                                .Select(id => doc.GetElement(id) as Viewport)
                                .FirstOrDefault(vp => vp != null && vp.Id != refItem.PlacedElementId);

                            if (primaryVp != null)
                            {
                                primaryVp.SetBoxCenter(refItem.CenterPoint);
                                alignedCount++;
                            }
                        }
                    }

                    trans.Commit();
                }
                tg.Assimilate();
            }

            Window.TxtStatus.Text = $"✔ Matched exact coordinates across {alignedCount} target sheet(s). (1x Ctrl+Z to Undo)";
        }

        private void ExecuteDuplicateSheets(Document doc)
        {
            var selectedSheets = Window.SheetList.Where(s => s.IsSelected).ToList();
            if (selectedSheets.Count == 0)
            {
                Window.TxtStatus.Text = "Please select at least one sheet to duplicate.";
                return;
            }

            ViewDuplicateOption dupOpt = Window.RbDupWithDetailing.IsChecked == true
                ? ViewDuplicateOption.WithDetailing
                : (Window.RbDupAsDependent.IsChecked == true ? ViewDuplicateOption.AsDependent : ViewDuplicateOption.Duplicate);

            string suffix = string.IsNullOrWhiteSpace(Window.TxtDupSuffix.Text) ? "-COPY" : Window.TxtDupSuffix.Text.Trim();
            int clonedSheets = 0;

            using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ALIGNER — Duplicate Sheets with Views"))
            {
                tg.Start();
                using (Transaction trans = new Transaction(doc, "Clone Sheets, Viewports & Schedules"))
                {
                    trans.Start();

                    foreach (var sItem in selectedSheets)
                    {
                        if (!(doc.GetElement(sItem.SheetId) is ViewSheet srcSheet)) continue;

                        // Find TitleBlock type on source sheet
                        ElementId tbTypeId = ElementId.InvalidElementId;
                        var tbInst = new FilteredElementCollector(doc, srcSheet.Id)
                            .OfCategory(BuiltInCategory.OST_TitleBlocks)
                            .WhereElementIsNotElementType()
                            .FirstOrDefault();
                        if (tbInst != null) tbTypeId = tbInst.GetTypeId();

                        ViewSheet newSheet = ViewSheet.Create(doc, tbTypeId);
                        if (newSheet == null) continue;

                        newSheet.Name = srcSheet.Name;
                        for (int i = 1; i < 100; i++)
                        {
                            string candidate = i == 1 ? $"{srcSheet.SheetNumber}{suffix}" : $"{srcSheet.SheetNumber}{suffix}{i}";
                            try { newSheet.SheetNumber = candidate; break; } catch { }
                        }

                        // Copy Viewports
                        foreach (ElementId vpId in srcSheet.GetAllViewports())
                        {
                            if (!(doc.GetElement(vpId) is Viewport srcVp)) continue;
                            if (!(doc.GetElement(srcVp.ViewId) is View srcView)) continue;

                            XYZ center = srcVp.GetBoxCenter();
                            ElementId targetViewId = srcView.Id;

                            if (srcView.ViewType != ViewType.Legend && srcView.CanViewBeDuplicated(dupOpt))
                            {
                                try
                                {
                                    targetViewId = srcView.Duplicate(dupOpt);
                                }
                                catch { continue; }
                            }

                            if (Viewport.CanAddViewToSheet(doc, newSheet.Id, targetViewId))
                            {
                                try
                                {
                                    Viewport newVp = Viewport.Create(doc, newSheet.Id, targetViewId, center);
                                    if (newVp != null && srcVp.GetTypeId() != ElementId.InvalidElementId)
                                        newVp.ChangeTypeId(srcVp.GetTypeId());
                                }
                                catch { }
                            }
                        }

                        // Copy Schedules
                        var schedules = new FilteredElementCollector(doc, srcSheet.Id)
                            .OfClass(typeof(ScheduleSheetInstance))
                            .Cast<ScheduleSheetInstance>()
                            .Where(si => !si.IsTitleblockRevisionSchedule)
                            .ToList();

                        foreach (ScheduleSheetInstance ssi in schedules)
                        {
                            try
                            {
                                ScheduleSheetInstance.Create(doc, newSheet.Id, ssi.ScheduleId, ssi.Point);
                            }
                            catch { }
                        }

                        clonedSheets++;
                    }

                    trans.Commit();
                }
                tg.Assimilate();
            }

            ExecuteScan(doc);
            Window.TxtStatus.Text = $"✔ Successfully duplicated {clonedSheets} sheet(s) with all views, legends & schedules! (1x Ctrl+Z to Undo)";
        }
    }

    public partial class GoatAlignerWindow : Window
    {
        private readonly GoatAlignerEventHandler _handler;
        private readonly ExternalEvent _extEvent;

        public ObservableCollection<AlignSheetItem> SheetList { get; } = new ObservableCollection<AlignSheetItem>();

        public GoatAlignerWindow(GoatAlignerEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            DgSheets.ItemsSource = SheetList;

            Loaded += (s, e) =>
            {
                _handler.Mode = GoatAlignerMode.Scan;
                _extEvent.Raise();
            };
        }

        public void ApplyScannedData(List<AlignSheetItem> sheets, List<ReferencePlacedItem> refs)
        {
            SheetList.Clear();
            foreach (var s in sheets) SheetList.Add(s);

            CbReferenceViewports.ItemsSource = refs;
            if (refs.Count > 0) CbReferenceViewports.SelectedIndex = 0;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _handler.Mode = GoatAlignerMode.Scan;
            _extEvent.Raise();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var s in SheetList) s.IsSelected = true;
            DgSheets.Items.Refresh();
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var s in SheetList) s.IsSelected = false;
            DgSheets.Items.Refresh();
        }

        private void BtnAutoPack_Click(object sender, RoutedEventArgs e)
        {
            _handler.Mode = GoatAlignerMode.AutoPackSheets;
            _extEvent.Raise();
        }

        private void BtnMatchPosition_Click(object sender, RoutedEventArgs e)
        {
            _handler.Mode = GoatAlignerMode.MatchPositionAcrossSheets;
            _extEvent.Raise();
        }

        private void BtnDuplicateSheets_Click(object sender, RoutedEventArgs e)
        {
            _handler.Mode = GoatAlignerMode.DuplicateSheetsWithViews;
            _extEvent.Raise();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
