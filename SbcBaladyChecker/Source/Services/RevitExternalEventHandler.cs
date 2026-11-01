using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Services
{
    public enum AuditActionType
    {
        SelectAndZoom,
        IsolateViolations,
        ColorizeInActiveView,
        ResetViewOverrides
    }

    public class SbcFailuresPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failures = failuresAccessor.GetFailureMessages();
            foreach (var f in failures)
            {
                if (f.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(f);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }

    public class RevitExternalEventHandler : IExternalEventHandler
    {
        public AuditActionType CurrentAction { get; set; }
        public ElementId TargetElementId { get; set; }
        public List<ElementId> TargetElementIds { get; set; } = new List<ElementId>();
        public List<AuditResultItem> ResultsToColorize { get; set; } = new List<AuditResultItem>();

        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            try
            {
                switch (CurrentAction)
                {
                    case AuditActionType.SelectAndZoom:
                        if (TargetElementId != null && TargetElementId != ElementId.InvalidElementId)
                        {
                            uidoc.Selection.SetElementIds(new List<ElementId> { TargetElementId });
                            uidoc.ShowElements(TargetElementId);
                        }
                        break;

                    case AuditActionType.IsolateViolations:
                        if (TargetElementIds != null && TargetElementIds.Count > 0 && activeView != null)
                        {
                            using (Transaction t = new Transaction(doc, "عزل مخالفات كود البناء مؤقتاً"))
                            {
                                FailureHandlingOptions fOptions = t.GetFailureHandlingOptions();
                                fOptions.SetFailuresPreprocessor(new SbcFailuresPreprocessor());
                                t.SetFailureHandlingOptions(fOptions);

                                t.Start();
                                try
                                {
                                    activeView.IsolateElementsTemporary(TargetElementIds);
                                    t.Commit();
                                }
                                catch
                                {
                                    t.RollBack();
                                }
                            }
                        }
                        break;

                    case AuditActionType.ColorizeInActiveView:
                        if (ResultsToColorize != null && activeView != null)
                        {
                            using (Transaction t = new Transaction(doc, "تلوين مخالفات كود البناء بالأحمر والأصفر"))
                            {
                                FailureHandlingOptions fOptions = t.GetFailureHandlingOptions();
                                fOptions.SetFailuresPreprocessor(new SbcFailuresPreprocessor());
                                t.SetFailureHandlingOptions(fOptions);

                                t.Start();
                                try
                                {
                                    Color redColor = new Color(211, 47, 47);    // Violation Red
                                    Color orangeColor = new Color(245, 124, 0); // Warning Orange

                                    OverrideGraphicSettings redSettings = new OverrideGraphicSettings();
                                    redSettings.SetProjectionLineColor(redColor);
                                    redSettings.SetSurfaceForegroundPatternColor(redColor);

                                    OverrideGraphicSettings orangeSettings = new OverrideGraphicSettings();
                                    orangeSettings.SetProjectionLineColor(orangeColor);
                                    orangeSettings.SetSurfaceForegroundPatternColor(orangeColor);

                                    foreach (var item in ResultsToColorize)
                                    {
                                        if (item.ElementId != null && item.ElementId != ElementId.InvalidElementId)
                                        {
                                            if (item.Severity == RuleSeverity.Violation)
                                            {
                                                activeView.SetElementOverrides(item.ElementId, redSettings);
                                            }
                                            else if (item.Severity == RuleSeverity.Warning)
                                            {
                                                activeView.SetElementOverrides(item.ElementId, orangeSettings);
                                            }
                                        }
                                    }
                                    t.Commit();
                                }
                                catch
                                {
                                    t.RollBack();
                                }
                            }
                        }
                        break;

                    case AuditActionType.ResetViewOverrides:
                        if (activeView != null)
                        {
                            using (Transaction t = new Transaction(doc, "إعادة ضبط ألوان المنظور"))
                            {
                                FailureHandlingOptions fOptions = t.GetFailureHandlingOptions();
                                fOptions.SetFailuresPreprocessor(new SbcFailuresPreprocessor());
                                t.SetFailureHandlingOptions(fOptions);

                                t.Start();
                                try
                                {
                                    if (activeView.IsInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate))
                                    {
                                        activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                                    }

                                    foreach (var item in ResultsToColorize)
                                    {
                                        if (item.ElementId != null && item.ElementId != ElementId.InvalidElementId)
                                        {
                                            activeView.SetElementOverrides(item.ElementId, new OverrideGraphicSettings());
                                        }
                                    }
                                    t.Commit();
                                }
                                catch
                                {
                                    t.RollBack();
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("SBC Auditor", "حدث خطأ أثناء تنفيذ الإجراء في الريفيت: " + ex.Message);
            }
        }

        public string GetName() => "SbcRevitExternalEventHandler";
    }
}
