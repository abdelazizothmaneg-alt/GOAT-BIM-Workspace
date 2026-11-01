using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public class HabitableRoomsRule : ISbcRule
    {
        public string RuleId => "SBC-201-1208";
        public string Name => "الارتفاع الصافي والحد الأدنى لمساحات الفراغات المعمارية";
        public string SbcReference => "SBC 201 - Sections 1208.2 & 1208.3";
        public SbcCategory Category => SbcCategory.Sbc201_Architectural;

        private const double MinCeilingHeightMeters = 2.40; // 2.40 م أدنى ارتفاع صافي
        private const double MinHabitableAreaSqM = 6.50;    // 6.50 م² أدنى مساحة لغرفة سكنية

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var results = new List<AuditResultItem>();
            bool isResidential = settings.Typology == BuildingTypology.ResidentialVilla 
                              || settings.Typology == BuildingTypology.ResidentialApartments;

            using (var collector = new FilteredElementCollector(doc))
            {
                var rooms = collector
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var element in rooms)
                {
                    if (element is not Room room) continue;

                    if (room.Area <= 0.01 || room.Location == null) continue;

                    string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "فراغ غير مسمى";
                    string roomNumber = room.Number ?? "";
                    double areaSqM = SbcUnits.SqFeetToSqMeters(room.Area);

                    // 1. Check Ceiling Height (2.40 m minimum for both residential and commercial)
                    double heightFeet = room.UnboundedHeight;
                    if (heightFeet <= 0.01)
                    {
                        Parameter pUpperOffset = room.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET);
                        if (pUpperOffset != null && pUpperOffset.HasValue)
                        {
                            heightFeet = pUpperOffset.AsDouble();
                        }
                    }

                    if (heightFeet > 0.01)
                    {
                        double heightMeters = SbcUnits.FeetToMeters(heightFeet);
                        if (heightMeters < MinCeilingHeightMeters - 0.05)
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1208.2",
                                Title = "الارتفاع الصافي للفراغ المعماري أقل من الحد المسموح (2.40م)",
                                Description = $"ارتفاع الفراغ ({roomName} #{roomNumber}) هو {Math.Round(heightMeters, 2)} م، وهو أقل من الحد الأدنى النظامي في كود البناء ({MinCeilingHeightMeters} م).",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Violation,
                                ElementId = room.Id,
                                ElementName = $"{roomName} (رقم {roomNumber})",
                                ElementCategory = "غرف وفراغات",
                                CurrentValue = $"{Math.Round(heightMeters, 2)} م",
                                AllowedRange = $"≥ {MinCeilingHeightMeters} م",
                                Recommendation = "قم بزيادة ارتفاع المنسوب (Level) أو مراجعة السقف الساقط لضمان ألا يقل الارتفاع الصافي عن 2.40م."
                            });
                        }
                    }

                    // 2. Residential Room Area Check
                    if (isResidential)
                    {
                        string lowerName = roomName.ToLower();
                        bool isLikelyHabitable = lowerName.Contains("bed") || lowerName.Contains("نوم") 
                                              || lowerName.Contains("living") || lowerName.Contains("معيشة") 
                                              || lowerName.Contains("مجلس") || lowerName.Contains("صالة");

                        if (isLikelyHabitable && areaSqM < MinHabitableAreaSqM)
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1208.3",
                                Title = "مساحة الغرفة السكنية أقل من الحد الأدنى النظامي",
                                Description = $"مساحة الفراغ ({roomName} #{roomNumber}) هي {Math.Round(areaSqM, 1)} م²، وهي أقل من الحد الأدنى المقبول للغرف السكنية ({MinHabitableAreaSqM} م²).",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Warning,
                                ElementId = room.Id,
                                ElementName = $"{roomName} (رقم {roomNumber})",
                                ElementCategory = "غرف وفراغات",
                                CurrentValue = $"{Math.Round(areaSqM, 1)} م²",
                                AllowedRange = $"≥ {MinHabitableAreaSqM} م²",
                                Recommendation = "تأكد من توافق أبعاد الغرفة مع اشتراطات الكود بحيث لا يقل أي بعد أفقي عن 2.10م والمساحة عن 6.5م²."
                            });
                        }
                    }
                }
            }

            return results;
        }
    }
}
