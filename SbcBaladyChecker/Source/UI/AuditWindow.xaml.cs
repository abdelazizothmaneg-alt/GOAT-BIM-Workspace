using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SbcBaladyChecker.Core;
using SbcBaladyChecker.Services;

namespace SbcBaladyChecker.UI
{
    public partial class AuditWindow : Window
    {
        private readonly Document _doc;
        private readonly AuditEngine _auditEngine;
        private AuditSummary _summary;
        private readonly ExternalEvent _externalEvent;
        private readonly RevitExternalEventHandler _eventHandler;
        private List<AuditResultItem> _currentViewItems;
        private AuditSettings _settings = new AuditSettings();

        public AuditWindow(Document doc, AuditSummary summary, ExternalEvent externalEvent, RevitExternalEventHandler eventHandler)
        {
            InitializeComponent();
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _summary = summary ?? throw new ArgumentNullException(nameof(summary));
            _externalEvent = externalEvent;
            _eventHandler = eventHandler;
            _auditEngine = new AuditEngine();

            LoadData();
        }

        private void LoadData()
        {
            TxtComplianceScore.Text = $"{_summary.CompliancePercentage}%";
            TxtTotalChecks.Text = _summary.TotalChecks.ToString();
            TxtViolations.Text = _summary.ViolationCount.ToString();
            TxtWarnings.Text = _summary.WarningCount.ToString();
            TxtPassed.Text = _summary.PassedCount.ToString();

            ApplyCategoryFilter();
        }

        private void CmbTypology_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_doc == null || CmbTypology.SelectedItem is not ComboBoxItem item) return;

            string tag = item.Tag?.ToString();
            _settings.Typology = tag switch
            {
                "ResidentialApartments" => BuildingTypology.ResidentialApartments,
                "CommercialRetail" => BuildingTypology.CommercialRetail,
                "CommercialOffice" => BuildingTypology.CommercialOffice,
                "MixedUse" => BuildingTypology.MixedUse,
                _ => BuildingTypology.ResidentialVilla
            };

            TxtTypologyHint.Text = _settings.Typology switch
            {
                BuildingTypology.ResidentialVilla => "فلل سكنية: نسبة البناء 60%، ارتداد 2م، موقف سيارة واحد، عرض الدرج 900مم.",
                BuildingTypology.ResidentialApartments => "عمائر شقق: نسبة البناء 65%، 1.5 موقف/شقة + 10% زوار، عرض الممر 1.40م، الدرج 1100مم.",
                BuildingTypology.CommercialRetail => "تجاري محلات: نسبة البناء 70%، موقف لكل 25م²، عرض الممر 1.80م، الأبواب 900مم.",
                BuildingTypology.CommercialOffice => "إداري مكاتب: نسبة البناء 65%، موقف لكل 40م²، عرض الممر 1.80م، الدرج 1100مم.",
                BuildingTypology.MixedUse => "سكني تجاري: نسبة البناء 70%، موقف لكل 35م²، عرض الممر 1.80م، الأبواب 900مم.",
                _ => ""
            };

            ReRunAudit();
        }

        private void BtnReAudit_Click(object sender, RoutedEventArgs e)
        {
            ReRunAudit();
        }

        private void ReRunAudit()
        {
            if (_doc == null) return;
            _summary = _auditEngine.RunFullAudit(_doc, _settings);
            LoadData();
        }

        private void CmbFilterCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyCategoryFilter();
        }

        private void ApplyCategoryFilter()
        {
            if (_summary == null || GridResults == null) return;

            string selected = (CmbFilterCategory.SelectedItem as ComboBoxItem)?.Content?.ToString();

            _currentViewItems = selected switch
            {
                "المخالفات فقط (مرفوض)" => _summary.Results.Where(r => r.Severity == RuleSeverity.Violation).ToList(),
                "التنبيهات فقط" => _summary.Results.Where(r => r.Severity == RuleSeverity.Warning).ToList(),
                "المعماري (SBC 201)" => _summary.Results.Where(r => r.Category == SbcCategory.Sbc201_Architectural).ToList(),
                "اشتراطات بلدي والارتدادات" => _summary.Results.Where(r => r.Category == SbcCategory.Balady_Zoning).ToList(),
                "معايير مواقف السيارات" => _summary.Results.Where(r => r.Category == SbcCategory.Balady_Parking).ToList(),
                "التعارض الإنشائي" => _summary.Results.Where(r => r.Category == SbcCategory.Structural_Coordination).ToList(),
                _ => _summary.Results
            };

            GridResults.ItemsSource = _currentViewItems;
        }

        private void GridResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TriggerSelectAndZoom();
        }

        private void BtnSelectElement_Click(object sender, RoutedEventArgs e)
        {
            TriggerSelectAndZoom();
        }

        private void TriggerSelectAndZoom()
        {
            if (GridResults.SelectedItem is AuditResultItem selected && selected.ElementId != null && selected.ElementId != ElementId.InvalidElementId)
            {
                _eventHandler.CurrentAction = AuditActionType.SelectAndZoom;
                _eventHandler.TargetElementId = selected.ElementId;
                _externalEvent.Raise();
            }
        }

        private void BtnColorize_Click(object sender, RoutedEventArgs e)
        {
            _eventHandler.CurrentAction = AuditActionType.ColorizeInActiveView;
            _eventHandler.ResultsToColorize = _summary.Results;
            _externalEvent.Raise();
        }

        private void BtnIsolate_Click(object sender, RoutedEventArgs e)
        {
            var violatingIds = _summary.Results
                .Where(r => r.Severity == RuleSeverity.Violation && r.ElementId != null && r.ElementId != ElementId.InvalidElementId)
                .Select(r => r.ElementId)
                .Distinct()
                .ToList();

            if (violatingIds.Count == 0)
            {
                System.Windows.MessageBox.Show("لا توجد عناصر مخالفة لعزلها في هذا المنظور.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _eventHandler.CurrentAction = AuditActionType.IsolateViolations;
            _eventHandler.TargetElementIds = violatingIds;
            _externalEvent.Raise();
        }

        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            _eventHandler.CurrentAction = AuditActionType.ResetViewOverrides;
            _eventHandler.ResultsToColorize = _summary.Results;
            _externalEvent.Raise();
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string reportPath = ReportGenerator.SaveReportToFile(_summary, null);

                Process.Start(new ProcessStartInfo
                {
                    FileName = reportPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("حدث خطأ أثناء تصدير التقرير: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
