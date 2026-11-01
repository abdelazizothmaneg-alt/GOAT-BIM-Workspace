using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public class ParkingQuotaRule : ISbcRule
    {
        public string RuleId => "MOMRAH-PARKING-QUOTA";
        public string Name => "مطابقة الحصة الإلزامية لمواقف السيارات في منصة بلدي";
        public string SbcReference => "دليل المعايير التخطيطية لمواقف السيارات - وزارة البلديات والإسكان (MOMRAH)";
        public SbcCategory Category => SbcCategory.Balady_Parking;

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var results = new List<AuditResultItem>();

            // 1. Count Provided Parking spaces in Revit model
            int providedSpaces = 0;
            using (var collector = new FilteredElementCollector(doc))
            {
                var parkingElements = collector
                    .OfCategory(BuiltInCategory.OST_Parking)
                    .WhereElementIsNotElementType()
                    .ToElements();

                providedSpaces = parkingElements.Count;
            }

            // 2. Calculate Total Gross Floor Area (GFA)
            double totalGfaSqFeet = 0.0;
            using (var collector = new FilteredElementCollector(doc))
            {
                var floors = collector
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var floor in floors)
                {
                    Parameter pArea = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                    if (pArea != null && pArea.HasValue)
                    {
                        totalGfaSqFeet += pArea.AsDouble();
                    }
                }
            }

            double totalGfaSqM = SbcUnits.SqFeetToSqMeters(totalGfaSqFeet);

            // 3. Calculate Required Spaces based on Typology
            int requiredSpaces = 1;
            string calcFormula = "";

            switch (settings.Typology)
            {
                case BuildingTypology.ResidentialVilla:
                    // 1 car parking inside boundary
                    requiredSpaces = 1;
                    calcFormula = "موقف سيارة واحد داخل حدود الملكية لكل فيلا سكنية";
                    break;

                case BuildingTypology.ResidentialApartments:
                    // Estimate number of apartments (e.g. 1 apartment per ~150 m2 GFA)
                    int estApartments = Math.Max(2, (int)Math.Round(totalGfaSqM / 150.0));
                    requiredSpaces = (int)Math.Ceiling(estApartments * 1.5 + (estApartments * 0.1));
                    calcFormula = $"1.5 موقف لكل شقة + 10% مواقف زوار (تقدير {estApartments} وحدة سكنية)";
                    break;

                case BuildingTypology.CommercialRetail:
                    // 1 space per 25 m2 GFA
                    requiredSpaces = Math.Max(2, (int)Math.Ceiling(totalGfaSqM / 25.0));
                    calcFormula = $"موقف واحد لكل 25 م² من المساحة الإجمالية ({Math.Round(totalGfaSqM, 0)} م²)";
                    break;

                case BuildingTypology.CommercialOffice:
                    // 1 space per 40 m2 GFA
                    requiredSpaces = Math.Max(2, (int)Math.Ceiling(totalGfaSqM / 40.0));
                    calcFormula = $"موقف واحد لكل 40 م² من المساحة الإجمالية ({Math.Round(totalGfaSqM, 0)} م²)";
                    break;

                case BuildingTypology.MixedUse:
                    // 1 space per 35 m2 GFA
                    requiredSpaces = Math.Max(3, (int)Math.Ceiling(totalGfaSqM / 35.0));
                    calcFormula = $"موقف واحد لكل 35 م² للمباني السكنية التجارية المختلطة ({Math.Round(totalGfaSqM, 0)} م²)";
                    break;
            }

            // 4. Evaluate Compliance
            if (providedSpaces < requiredSpaces)
            {
                results.Add(new AuditResultItem
                {
                    RuleCode = "MOMRAH-PARKING-01",
                    Title = "عجز في عدد مواقف السيارات الإلزامية لمنصة بلدي",
                    Description = $"العدد المتوفر في النموذج هو ({providedSpaces}) موقف، بينما الحد الأدنى المطلوب نظاماً لـ {settings.TypologyLabel} هو ({requiredSpaces}) موقف، بناءً على: {calcFormula}.",
                    Category = SbcCategory.Balady_Parking,
                    Severity = RuleSeverity.Violation,
                    ElementId = ElementId.InvalidElementId,
                    ElementName = "مواقف السيارات (Parking)",
                    ElementCategory = "الموقع والتنظيم",
                    CurrentValue = $"{providedSpaces} موقف",
                    AllowedRange = $"≥ {requiredSpaces} موقف",
                    Recommendation = $"قم بنمذجة مواقف سيارات إضافية (Site -> Parking Component) لتحقيق النصاب النظامي ({requiredSpaces} موقف) لتفادي الرفض الفوري للرخصة."
                });
            }
            else
            {
                results.Add(new AuditResultItem
                {
                    RuleCode = "MOMRAH-PARKING-01",
                    Title = "عدد مواقف السيارات محقق ومطابق لاشتراطات بلدي",
                    Description = $"العدد المتوفر في النموذج هو ({providedSpaces}) موقف، وهو يغطي أو يتجاوز الحد الأدنى المطلوب ({requiredSpaces} موقف) لـ {settings.TypologyLabel}.",
                    Category = SbcCategory.Balady_Parking,
                    Severity = RuleSeverity.Compliant,
                    ElementId = ElementId.InvalidElementId,
                    ElementName = "مواقف السيارات (Parking)",
                    ElementCategory = "الموقع والتنظيم",
                    CurrentValue = $"{providedSpaces} موقف",
                    AllowedRange = $"≥ {requiredSpaces} موقف",
                    Recommendation = "عدد المواقف كافٍ ومطابق للاشتراطات."
                });
            }

            return results;
        }
    }
}
