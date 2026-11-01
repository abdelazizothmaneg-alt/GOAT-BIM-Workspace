using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Rules
{
    public class DoorsAndEgressRule : ISbcRule
    {
        public string RuleId => "SBC-201-1010";
        public string Name => "مطابقة عروض الأبواب ومخارج الهروب والفتحات المعمارية";
        public string SbcReference => "SBC 201 - Section 1010.1.1 & SBC 801 Chapter 10";
        public SbcCategory Category => SbcCategory.Sbc201_Architectural;

        private const double MinExitDoorWidthMm = 900.0;     // 900 مم للمخارج والمباني التجارية
        private const double MinHabitableDoorWidthMm = 800.0; // 800 مم للأبواب السكنية الداخلية
        private const double MaxSingleLeafWidthMm = 1200.0;   // 1200 مم أقصى عرض للضلفة الواحدة

        public IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings)
        {
            var results = new List<AuditResultItem>();
            bool isCommercial = settings.Typology == BuildingTypology.CommercialRetail 
                             || settings.Typology == BuildingTypology.CommercialOffice 
                             || settings.Typology == BuildingTypology.MixedUse;

            using (var collector = new FilteredElementCollector(doc))
            {
                var doors = collector
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var door in doors)
                {
                    string doorName = door.Name ?? "باب غير مسمى";
                    double widthFeet = GetDoorWidth(doc, door);

                    if (widthFeet <= 0.01) continue;

                    double widthMm = SbcUnits.FeetToMm(widthFeet);

                    // 1. In Commercial: ALL doors serving public / offices must be >= 900 mm
                    if (isCommercial)
                    {
                        if (widthMm < MinExitDoorWidthMm - 5.0)
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1010.1.1",
                                Title = "عرض الباب في المبنى التجاري أقل من الحد النظامي (900 مم)",
                                Description = $"عرض الباب هو {Math.Round(widthMm, 0)} مم. في {settings.TypologyLabel} يشترط الكود ألا يقل العرض الصافي للأبواب عن 900 مم لضمان سهولة الإخلاء والوصول الشامل.",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Violation,
                                ElementId = door.Id,
                                ElementName = doorName,
                                ElementCategory = "أبواب تجارية وإدارية",
                                CurrentValue = $"{Math.Round(widthMm, 0)} مم",
                                AllowedRange = $"≥ 900 مم",
                                Recommendation = "قم بتعديل نوع الباب إلى مقاس 900 مم على الأقل ليتوافق مع كود المباني التجارية والإدارية."
                            });
                        }
                    }
                    else
                    {
                        // Residential rules
                        if (widthMm < 750.0)
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1010.1.1",
                                Title = "عرض الباب ضيق جداً ويخالف أدنى حدود الكود السكني",
                                Description = $"عرض الباب المسجل هو {Math.Round(widthMm, 0)} مم، وهو أقل من الحد الأدنى المقبول للأبواب السكنية (800 مم).",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Violation,
                                ElementId = door.Id,
                                ElementName = doorName,
                                ElementCategory = "أبواب سكنية",
                                CurrentValue = $"{Math.Round(widthMm, 0)} مم",
                                AllowedRange = $"≥ 800 مم",
                                Recommendation = "يجب زيادة عرض فتحة الباب إلى 800 مم للغرف السكنية أو 900 مم للمداخل الرئيسية."
                            });
                        }
                        else if (widthMm < MinHabitableDoorWidthMm)
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1010.1.1",
                                Title = "عرض الباب أقل من اشتراط الغرف السكنية (800 مم)",
                                Description = $"عرض الباب هو {Math.Round(widthMm, 0)} مم. تأكد أنه مخصص لدورة مياه ثانوية فقط.",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Warning,
                                ElementId = door.Id,
                                ElementName = doorName,
                                ElementCategory = "أبواب سكنية",
                                CurrentValue = $"{Math.Round(widthMm, 0)} مم",
                                AllowedRange = $"≥ 800 مم",
                                Recommendation = "ترقية الباب إلى 800 مم إن كان يخدم غرفة معيشية أو نوم."
                            });
                        }
                    }

                    // 2. Check oversized single leaf (> 1200 mm)
                    if (widthMm > MaxSingleLeafWidthMm + 50.0)
                    {
                        // Check if it is a single leaf door
                        string lowerName = doorName.ToLower();
                        if (!lowerName.Contains("double") && !lowerName.Contains("مزدوج") && !lowerName.Contains("ضلفين"))
                        {
                            results.Add(new AuditResultItem
                            {
                                RuleCode = "SBC 201 - 1010.1.1",
                                Title = "عرض ضلفة الباب الفردية يتجاوز الحد الأقصى (1200 مم)",
                                Description = $"عرض الباب المسجل هو {Math.Round(widthMm, 0)} مم. الكود ينص على ألا يتجاوز عرض الضلفة الواحدة 1200 مم لتسهيل الفتح والتشغيل.",
                                Category = SbcCategory.Sbc201_Architectural,
                                Severity = RuleSeverity.Warning,
                                ElementId = door.Id,
                                ElementName = doorName,
                                ElementCategory = "أبواب",
                                CurrentValue = $"{Math.Round(widthMm, 0)} مم",
                                AllowedRange = $"≤ 1200 مم للضلفة الفردية",
                                Recommendation = "يُفضل استبدال هذا الباب بنموذج ضلفتين (Double-leaf door) لتوزيع الوزن وسهولة الحركة."
                            });
                        }
                    }
                }
            }

            return results;
        }

        private double GetDoorWidth(Document doc, Element door)
        {
            Parameter p = door.get_Parameter(BuiltInParameter.DOOR_WIDTH);
            if (p != null && p.HasValue && p.AsDouble() > 0) return p.AsDouble();

            p = door.LookupParameter("Width") ?? door.LookupParameter("العرض");
            if (p != null && p.HasValue && p.AsDouble() > 0) return p.AsDouble();

            ElementId typeId = door.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                Element type = doc.GetElement(typeId);
                if (type != null)
                {
                    p = type.get_Parameter(BuiltInParameter.DOOR_WIDTH)
                     ?? type.get_Parameter(BuiltInParameter.GENERIC_WIDTH)
                     ?? type.LookupParameter("Width")
                     ?? type.LookupParameter("العرض");

                    if (p != null && p.HasValue && p.AsDouble() > 0)
                        return p.AsDouble();
                }
            }

            return 0.0;
        }
    }
}
