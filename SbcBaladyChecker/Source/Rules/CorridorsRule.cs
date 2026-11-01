using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public class CorridorsRule : ISbcRule
    {
        public string RuleId => "SBC-201-1020";
        public string Name => "العرض الصافي للممرات العامة ومسارات الحركة الأفقية";
        public string SbcReference => "SBC 201 - Section 1020.2 & SBC 801";
        public SbcCategory Category => SbcCategory.Sbc201_Architectural;

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var results = new List<AuditResultItem>();

            // Determine minimum required corridor width
            double requiredWidthMeters = settings.Typology switch
            {
                BuildingTypology.CommercialRetail => 1.80,       // 1.80 م للتجاري والمحلات
                BuildingTypology.CommercialOffice => 1.80,       // 1.80 م للمكاتب العامة (أو 1.20م لأقل من 50 شخص)
                BuildingTypology.MixedUse => 1.80,               // 1.80 م للمختلط
                BuildingTypology.ResidentialApartments => 1.40,  // 1.40 م لممرات العمارات السكنية المشتركة
                BuildingTypology.ResidentialVilla => 1.00,       // 1.00 م لممرات الفلل الداخلية
                _ => 1.20
            };

            using (var collector = new FilteredElementCollector(doc))
            {
                var rooms = collector
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var element in rooms)
                {
                    if (element is not Room room || room.Area <= 0.01) continue;

                    string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
                    string lowerName = roomName.ToLower();

                    // Check if room represents a corridor/hallway/lobby
                    bool isCorridor = lowerName.Contains("corridor") || lowerName.Contains("ممر") 
                                   || lowerName.Contains("hallway") || lowerName.Contains("موزع") 
                                   || lowerName.Contains("مسار");

                    if (!isCorridor) continue;

                    // Approximate minimum horizontal dimension from bounding box or perimeter/area
                    BoundingBoxXYZ box = room.get_BoundingBox(null);
                    if (box == null) continue;

                    double dimX = SbcUnits.FeetToMeters(box.Max.X - box.Min.X);
                    double dimY = SbcUnits.FeetToMeters(box.Max.Y - box.Min.Y);
                    double minDimension = Math.Min(dimX, dimY);

                    // If minimum dimension of corridor is less than required width
                    if (minDimension < requiredWidthMeters - 0.05 && minDimension > 0.4) // tolerance
                    {
                        results.Add(new AuditResultItem
                        {
                            RuleCode = "SBC 201 - 1020.2",
                            Title = $"عرض الممر أقل من الحد النظامي لـ {settings.TypologyLabel}",
                            Description = $"عرض الممر المعماري ({roomName} #{room.Number}) هو تقريباً {Math.Round(minDimension, 2)} م، في حين يشترط الكود ألا يقل العرض الصافي عن {requiredWidthMeters} م لضمان سلامة وسلاسة حركة الإخلاء.",
                            Category = SbcCategory.Sbc201_Architectural,
                            Severity = RuleSeverity.Violation,
                            ElementId = room.Id,
                            ElementName = $"{roomName} (رقم {room.Number})",
                            ElementCategory = "ممرات ومسارات حركة",
                            CurrentValue = $"{Math.Round(minDimension, 2)} م",
                            AllowedRange = $"≥ {requiredWidthMeters} م",
                            Recommendation = $"قم بتوسيع عرض الممر ليكون {requiredWidthMeters} م على الأقل ليتوافق مع اشتراطات كود البناء لـ {settings.TypologyLabel}."
                        });
                    }
                }
            }

            return results;
        }
    }
}
