using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using SbcBaladyChecker.Core;
using SbcBaladyChecker.Rules;

namespace SbcBaladyChecker.Services
{
    public class AuditEngine
    {
        private readonly List<ISbcRule> _rules = new List<ISbcRule>();

        public AuditEngine()
        {
            // Register built-in SBC & Balady validation rules
            _rules.Add(new StairsRule());
            _rules.Add(new DoorsAndEgressRule());
            _rules.Add(new CorridorsRule());
            _rules.Add(new HabitableRoomsRule());
            _rules.Add(new SetbackAndZoningRule());
            _rules.Add(new ParkingQuotaRule());
            _rules.Add(new BeamDoorClashRule());
            _rules.Add(new BaladyFARAndCoverageRule());
        }

        public AuditSummary RunFullAudit(Document doc, AuditSettings settings = null)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            settings ??= new AuditSettings();

            var summary = new AuditSummary
            {
                ProjectName = doc.ProjectInformation?.Name ?? "مشروع بدون اسم",
                DocumentTitle = doc.Title,
                AuditDate = DateTime.Now,
                Settings = settings
            };

            var allResults = new List<AuditResultItem>();

            foreach (var rule in _rules)
            {
                try
                {
                    var items = rule.Validate(doc, settings);
                    if (items != null)
                    {
                        allResults.AddRange(items);
                    }
                }
                catch (Exception ex)
                {
                    allResults.Add(new AuditResultItem
                    {
                        RuleCode = rule.RuleId,
                        Title = $"خطأ أثناء فحص القاعدة: {rule.Name}",
                        Description = ex.Message,
                        Category = rule.Category,
                        Severity = RuleSeverity.Warning,
                        ElementId = ElementId.InvalidElementId,
                        ElementName = "النظام الداخلي",
                        CurrentValue = "خطأ برمجي",
                        AllowedRange = "-",
                        Recommendation = "يرجى التحقق من اكتمال عناصر النموذج وبياناته."
                    });
                }
            }

            summary.Results = allResults;
            summary.TotalChecks = allResults.Count;
            summary.PassedCount = allResults.Count(r => r.Severity == RuleSeverity.Compliant);
            summary.WarningCount = allResults.Count(r => r.Severity == RuleSeverity.Warning);
            summary.ViolationCount = allResults.Count(r => r.Severity == RuleSeverity.Violation);

            return summary;
        }
    }
}
