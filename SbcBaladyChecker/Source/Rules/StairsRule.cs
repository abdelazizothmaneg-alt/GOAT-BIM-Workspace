using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public class StairsRule : ISbcRule
    {
        public string RuleId => "SBC-201-1011";
        public string Name => "مطابقة أبعاد الأدراج والسلالم (القائمة والنائمة والعرض الصافي)";
        public string SbcReference => "SBC 201 - Section 1011 & Table 1011.5.2";
        public SbcCategory Category => SbcCategory.Sbc201_Architectural;

        private const double MaxRiserMm = 180.0; // أقصى ارتفاع للقائمة 180 مم
        private const double MinRiserMm = 100.0; // أدنى ارتفاع للقائمة 100 مم
        private const double MinTreadMm = 280.0; // أدنى عمق للنائمة 280 مم

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var results = new List<AuditResultItem>();

            // Determine minimum stair width based on typology
            bool isCommercialOrMultiFamily = settings.Typology != BuildingTypology.ResidentialVilla;
            double requiredWidthMm = isCommercialOrMultiFamily ? 1100.0 : 900.0;
            string widthRuleDesc = isCommercialOrMultiFamily 
                ? "1100 مم (للمباني التجارية والمختلطة وعمائر الشقق)" 
                : "900 مم (للفلل السكنية الخاصة)";

            using (var collector = new FilteredElementCollector(doc))
            {
                var stairsList = collector
                    .OfCategory(BuiltInCategory.OST_Stairs)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var element in stairsList)
                {
                    string stairName = element.Name ?? "درج غير مسمى";

                    // 1. Riser Height (ارتفاع القائمة)
                    Parameter pRiser = element.get_Parameter(BuiltInParameter.STAIRS_ACTUAL_RISER_HEIGHT);
                    if (pRiser != null && pRiser.HasValue)
                    {
                        double riserMm = SbcUnits.FeetToMm(pRiser.AsDouble());
                        if (riserMm > MaxRiserMm + 1.0) // 1mm tolerance
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1011.5.2",
                                Title = "ارتفاع قائمة الدرج يتجاوز الحد الأقصى للكود",
                                Description = $"ارتفاع القائمة المسجل هو {Math.Round(riserMm, 1)} مم، وهو أكبر من الحد الأقصى المسموح ({MaxRiserMm} مم).",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Violation,
                                ElementId = element.Id,
                                ElementName = stairName,
                                ElementCategory = "أدراج وسلالم",
                                CurrentValue = $"{Math.Round(riserMm, 1)} مم",
                                AllowedRange = $"{MinRiserMm} مم - {MaxRiserMm} مم",
                                Recommendation = "قم بزيادة عدد الدرجات (Desired Number of Risers) لخفض القائمة إلى 180 مم أو أقل لتفادي الرفض الفني."
                            });
                        }
                    }

                    // 2. Tread Depth (عمق النائمة)
                    Parameter pTread = element.get_Parameter(BuiltInParameter.STAIRS_ACTUAL_TREAD_DEPTH);
                    if (pTread != null && pTread.HasValue)
                    {
                        double treadMm = SbcUnits.FeetToMm(pTread.AsDouble());
                        if (treadMm < MinTreadMm - 1.0)
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1011.5.2",
                                Title = "عمق نائمة الدرج أقل من الحد الأدنى الإلزامي",
                                Description = $"عمق النائمة هو {Math.Round(treadMm, 1)} مم، وأدنى حد نظامي في كود البناء هو ({MinTreadMm} مم).",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Violation,
                                ElementId = element.Id,
                                ElementName = stairName,
                                ElementCategory = "أدراج وسلالم",
                                CurrentValue = $"{Math.Round(treadMm, 1)} مم",
                                AllowedRange = $"≥ {MinTreadMm} مم",
                                Recommendation = "قم بزيادة عمق النائمة في خصائص النوع (Type Properties) إلى 280 مم على الأقل."
                            });
                        }
                    }

                    // 3. Clear Stair Width (العرض الصافي لشاحط الدرج)
                    double widthMm = 0;
                    if (element is Stairs stairs)
                    {
                        var runs = stairs.GetStairsRuns();
                        foreach (var runId in runs)
                        {
                            if (doc.GetElement(runId) is StairsRun run)
                            {
                                Parameter pRunWidth = run.get_Parameter(BuiltInParameter.STAIRS_RUN_ACTUAL_RUN_WIDTH);
                                if (pRunWidth != null && pRunWidth.HasValue)
                                {
                                    widthMm = SbcUnits.FeetToMm(pRunWidth.AsDouble());
                                    break;
                                }
                            }
                        }
                    }

                    if (widthMm <= 0)
                    {
                        Parameter pWidth = element.LookupParameter("Actual Run Width") 
                                        ?? element.LookupParameter("Width") 
                                        ?? element.LookupParameter("العرض");
                        if (pWidth != null && pWidth.HasValue)
                        {
                            widthMm = SbcUnits.FeetToMm(pWidth.AsDouble());
                        }
                    }

                    if (widthMm > 0 && widthMm < requiredWidthMm - 5.0)
                    {
                        results.Add(new AuditResultItem
                        {
                            RuleCode = "SBC 201 - 1011.2",
                            Title = $"عرض الدرج أقل من الحد الأدنى المسموح ({requiredWidthMm} مم)",
                            Description = $"عرض شاحط الدرج هو {Math.Round(widthMm, 0)} مم. الكود يشترط ألا يقل العرض الصافي عن {widthRuleDesc}.",
                            Category = SbcCategory.Sbc201_Architectural,
                            Severity = RuleSeverity.Violation,
                            ElementId = element.Id,
                            ElementName = stairName,
                            ElementCategory = "أدراج وسلالم",
                            CurrentValue = $"{Math.Round(widthMm, 0)} مم",
                            AllowedRange = $"≥ {requiredWidthMm} مم",
                            Recommendation = $"قم بزيادة عرض شاحط الدرج (Run Width) ليكون {requiredWidthMm} مم على الأقل ليتوافق مع اشتراطات {settings.TypologyLabel}."
                        });
                    }
                }
            }

            return results;
        }
    }
}
