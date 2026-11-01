using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public class SetbackAndZoningRule : ISbcRule
    {
        public string RuleId => "MOMRAH-BALADY-ZONING";
        public string Name => "اشتراطات منصة بلدي للارتدادات ونسبة التغطية (GCR / FAR)";
        public string SbcReference => "دليل اشتراطات البناء السكني والتجاري - منصة بلدي وأمانات المناطق";
        public SbcCategory Category => SbcCategory.Balady_Zoning;

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var results = new List<AuditResultItem>();

            // Maximum Ground Coverage Ratio according to typology:
            double maxAllowedGcr = settings.Typology switch
            {
                BuildingTypology.ResidentialVilla => 0.60,       // 60% فلل سكنية
                BuildingTypology.ResidentialApartments => 0.65,  // 65% عمائر شقق
                BuildingTypology.CommercialRetail => 0.70,       // 70% تجاري محلات
                BuildingTypology.CommercialOffice => 0.65,       // 65% إداري مكاتب
                BuildingTypology.MixedUse => 0.70,               // 70% سكني تجاري
                _ => 0.60
            };

            double maxPercent = Math.Round(maxAllowedGcr * 100.0, 0);

            // 1. Property Lines
            List<Element> propLines;
            using (var collector = new FilteredElementCollector(doc))
            {
                propLines = collector
                    .OfCategory(BuiltInCategory.OST_SiteProperty)
                    .WhereElementIsNotElementType()
                    .ToList();
            }

            double plotAreaSqFeet = 0.0;
            Element mainPropertyLine = null;

            if (propLines.Count > 0)
            {
                mainPropertyLine = propLines[0];
                Parameter pArea = mainPropertyLine.get_Parameter(BuiltInParameter.PROPERTY_AREA);
                if (pArea != null && pArea.HasValue && pArea.AsDouble() > 0)
                {
                    plotAreaSqFeet = pArea.AsDouble();
                }
            }

            // 2. Ground Floor Footprint Area
            double groundFootprintSqFeet = 0.0;
            using (var collector = new FilteredElementCollector(doc))
            {
                var floors = collector
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var floor in floors)
                {
                    string levelName = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM)?.AsValueString()?.ToLower() ?? "";
                    if (levelName.Contains("ground") || levelName.Contains("أرضي") || levelName.Contains("00") || levelName.Contains("level 1"))
                    {
                        Parameter pFloorArea = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                        if (pFloorArea != null && pFloorArea.HasValue)
                        {
                            groundFootprintSqFeet += pFloorArea.AsDouble();
                        }
                    }
                }
            }

            // Fallback from rooms if floor area not detected
            if (groundFootprintSqFeet <= 1.0)
            {
                using (var collector = new FilteredElementCollector(doc))
                {
                    var groundRooms = collector
                        .OfCategory(BuiltInCategory.OST_Rooms)
                        .WhereElementIsNotElementType()
                        .Cast<Autodesk.Revit.DB.Architecture.Room>()
                        .Where(r => r.Area > 0 && (r.Level?.Name?.ToLower().Contains("ground") == true 
                                                || r.Level?.Name?.ToLower().Contains("أرضي") == true 
                                                || r.Level?.Name?.ToLower().Contains("level 1") == true))
                        .ToList();

                    groundFootprintSqFeet = groundRooms.Sum(r => r.Area);
                }
            }

            // 3. Evaluate GCR
            if (plotAreaSqFeet > 10.0 && groundFootprintSqFeet > 10.0)
            {
                double plotAreaSqM = SbcUnits.SqFeetToSqMeters(plotAreaSqFeet);
                double footprintSqM = SbcUnits.SqFeetToSqMeters(groundFootprintSqFeet);
                double gcrRatio = groundFootprintSqFeet / plotAreaSqFeet;
                double gcrPercent = Math.Round(gcrRatio * 100.0, 1);

                if (gcrRatio > maxAllowedGcr + 0.005)
                {
                    results.Add(new AuditResultItem
                    {
                        RuleCode = "MOMRAH-GCR-01",
                        Title = $"تجاوز نسبة البناء المسموحة للأرضي ({maxPercent}%)",
                        Description = $"نسبة التغطية الأرضية للمشروع هي {gcrPercent}% (مساحة الدور الأرضي: {Math.Round(footprintSqM, 1)} م² من إجمالي مساحة الأرض: {Math.Round(plotAreaSqM, 1)} م²)، وهو ما يتجاوز الحد النظامي المسموح لـ {settings.TypologyLabel} ({maxPercent}%).",
                        Category = SbcCategory.Balady_Zoning,
                        Severity = RuleSeverity.Violation,
                        ElementId = mainPropertyLine?.Id ?? ElementId.InvalidElementId,
                        ElementName = "صك الأرض والحدود العقارية",
                        ElementCategory = "الموقع والتنظيم",
                        CurrentValue = $"{gcrPercent}%",
                        AllowedRange = $"≤ {maxPercent}.0%",
                        Recommendation = $"قم بتقليص مسطح بناء الدور الأرضي ليتوافق مع النسبة المحددة لـ {settings.TypologyLabel} في منصة بلدي."
                    });
                }
                else
                {
                    results.Add(new AuditResultItem
                    {
                        RuleCode = "MOMRAH-GCR-01",
                        Title = "نسبة البناء في الدور الأرضي مطابقة للاشتراطات",
                        Description = $"نسبة التغطية الأرضية هي {gcrPercent}% (ضمن الحد النظامي المسموح {maxPercent}% لـ {settings.TypologyLabel}).",
                        Category = SbcCategory.Balady_Zoning,
                        Severity = RuleSeverity.Compliant,
                        ElementId = mainPropertyLine?.Id ?? ElementId.InvalidElementId,
                        ElementName = "صك الأرض والحدود العقارية",
                        ElementCategory = "الموقع والتنظيم",
                        CurrentValue = $"{gcrPercent}%",
                        AllowedRange = $"≤ {maxPercent}.0%",
                        Recommendation = "النسبة مطابقة، لا يلزم إجراء."
                    });
                }
            }
            else if (plotAreaSqFeet <= 0.1)
            {
                results.Add(new AuditResultItem
                {
                    RuleCode = "MOMRAH-PROP-00",
                    Title = "لم يتم العثور على خطوط ملكية الأرض (Property Line)",
                    Description = "لم يتم نمذجة حدود الأرض عبر أداة Property Line في الريفيت، مما يمنع الحساب الآلي الدقيق لنسب البناء والارتدادات.",
                    Category = SbcCategory.Balady_Zoning,
                    Severity = RuleSeverity.Warning,
                    ElementId = ElementId.InvalidElementId,
                    ElementName = "حدود الموقع العام",
                    ElementCategory = "الموقع والتنظيم",
                    CurrentValue = "غير معرف",
                    AllowedRange = "صك إلكتروني محدد الأبعاد",
                    Recommendation = "قم برسم حدود الأرض باستخدام Massing & Site -> Property Line طبقاً للقرار المساحي."
                });
            }

            return results;
        }
    }
}
