using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public class BeamDoorClashRule : ISbcRule
    {
        public string RuleId => "SBC-201-1003";
        public string Name => "الخلوص الرأسي وتعارض الكمرات الإنشائية مع فتحات الأبواب ومسارات الحركة";
        public string SbcReference => "SBC 201 - Section 1003.3 (Headroom Clearance)";
        public SbcCategory Category => SbcCategory.Structural_Coordination;

        private const double MinHeadroomClearanceM = 2.03; // 2.03 م أدنى خلوص رأسي

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var results = new List<AuditResultItem>();

            List<Element> beams;
            using (var collector = new FilteredElementCollector(doc))
            {
                beams = collector
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .ToElements() as List<Element> ?? new List<Element>(collector.ToElements());
            }

            if (beams.Count == 0) return results;

            using (var collector = new FilteredElementCollector(doc))
            {
                var doors = collector
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var door in doors)
                {
                    BoundingBoxXYZ doorBox = door.get_BoundingBox(null);
                    if (doorBox == null) continue;

                    Outline expandedDoorOutline = new Outline(
                        new XYZ(doorBox.Min.X, doorBox.Min.Y, doorBox.Min.Z),
                        new XYZ(doorBox.Max.X, doorBox.Max.Y, doorBox.Max.Z + SbcUnits.MetersToFeet(0.30))
                    );

                    using (var beamFilter = new FilteredElementCollector(doc))
                    {
                        var clashingBeams = beamFilter
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsNotElementType()
                            .WherePasses(new BoundingBoxIntersectsFilter(expandedDoorOutline))
                            .ToElements();

                        foreach (var beam in clashingBeams)
                        {
                            BoundingBoxXYZ beamBox = beam.get_BoundingBox(null);
                            if (beamBox == null) continue;

                            if (beamBox.Min.Z < doorBox.Max.Z)
                            {
                                double overlapMm = SbcUnits.FeetToMm(doorBox.Max.Z - beamBox.Min.Z);
                                string doorName = door.Name ?? "باب";
                                string beamName = beam.Name ?? "كمرة ساقطة";

                                results.Add(new AuditResultItem
                                {
                                    RuleCode = "SBC 201 - 1003.3",
                                    Title = "تعارض كمرة ساقطة (Drop Beam) مع منسوب عتب الباب",
                                    Description = $"الكمرة الإنشائية ({beamName}) تتداخل رأسياً بمقدار {Math.Round(overlapMm, 0)} مم مع فتحة الباب ({doorName})، مما يخفض الخلوص الرأسي دون الحد النظامي.",
                                    Category = SbcCategory.Structural_Coordination,
                                    Severity = RuleSeverity.Violation,
                                    ElementId = door.Id,
                                    ElementName = $"{doorName} [مع {beamName}]",
                                    ElementCategory = "تعارض إنشائي / معماري",
                                    CurrentValue = $"تداخل {Math.Round(overlapMm, 0)} مم",
                                    AllowedRange = $"خلوص رأسي صافي ≥ {MinHeadroomClearanceM} م",
                                    Recommendation = "يجب تحويل الكمرة الساقطة إلى كمرة مدفونة (Hidden Beam) أو نقل مسار الباب لتفادي تشويه المدخل ورفض الرخصة."
                                });
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}
