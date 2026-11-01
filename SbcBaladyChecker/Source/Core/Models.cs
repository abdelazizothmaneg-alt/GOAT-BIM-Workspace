using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SbcBaladyChecker.Core
{
    public enum BuildingTypology
    {
        ResidentialVilla,       // سكني - فيلا خاصة
        ResidentialApartments,  // سكني - عمائر شقق سكنية
        CommercialRetail,       // تجاري - معارض ومحلات وأسواق
        CommercialOffice,       // إداري - مكاتب وشركات
        MixedUse                // سكني تجاري مختلط
    }

    public enum RuleSeverity
    {
        Compliant, // مطابق
        Warning,   // تنبيه / ملاحظة
        Violation  // مخالفة كود صريحة (مرفوض)
    }

    public enum SbcCategory
    {
        Sbc201_Architectural,    // كود البناء العام - المعماري
        Sbc801_FireSafety,       // كود الحريق وسبل الهروب
        Balady_Zoning,           // الاشتراطات البلدية (الارتدادات ونسب البناء)
        Balady_Parking,          // معايير مواقف السيارات
        Sbc601_Energy,           // كود كفاءة الطاقة والعزل
        Structural_Coordination  // التنسيق بين التخصصات والكمرات الساقطة
    }

    public class AuditSettings
    {
        public BuildingTypology Typology { get; set; } = BuildingTypology.ResidentialVilla;
        public double StreetWidthMeters { get; set; } = 15.0; // عرض الشارع
        public bool HasSprinklerSystem { get; set; } = false; // هل المبنى محمي بنظام رش آلي
        public double? PlotAreaSqMeters { get; set; } = null; // مساحة الأرض الصك (م²)

        public string TypologyLabel => Typology switch
        {
            BuildingTypology.ResidentialVilla => "سكني - فيلا خاصة",
            BuildingTypology.ResidentialApartments => "سكني - عمائر شقق سكنية",
            BuildingTypology.CommercialRetail => "تجاري - محلات ومعارض",
            BuildingTypology.CommercialOffice => "إداري - مكاتب وشركات",
            BuildingTypology.MixedUse => "سكني تجاري مختلط",
            _ => "عام"
        };
    }

    public class AuditResultItem
    {
        public string RuleCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public SbcCategory Category { get; set; }
        public RuleSeverity Severity { get; set; }
        public ElementId ElementId { get; set; }
        public string ElementName { get; set; }
        public string ElementCategory { get; set; }
        public string CurrentValue { get; set; }
        public string AllowedRange { get; set; }
        public string Recommendation { get; set; }

        public string SeverityLabel => Severity switch
        {
            RuleSeverity.Compliant => "مطابق للكود",
            RuleSeverity.Warning => "ملاحظة / تنبيه",
            RuleSeverity.Violation => "مخالفة صريحة (مرفوض)",
            _ => "غير محدد"
        };

        public string CategoryLabel => Category switch
        {
            SbcCategory.Sbc201_Architectural => "كود البناء المعماري (SBC 201)",
            SbcCategory.Sbc801_FireSafety => "كود الحريق ومخارج الطوارئ (SBC 801)",
            SbcCategory.Balady_Zoning => "اشتراطات بلدي والارتدادات",
            SbcCategory.Balady_Parking => "معايير مواقف السيارات",
            SbcCategory.Sbc601_Energy => "كود كفاءة الطاقة والعزل (SBC 601)",
            SbcCategory.Structural_Coordination => "تعارض الكمرات والتنسيق الإنشائي",
            _ => "عام"
        };
    }

    public class AuditSummary
    {
        public string ProjectName { get; set; }
        public string DocumentTitle { get; set; }
        public DateTime AuditDate { get; set; } = DateTime.Now;
        public AuditSettings Settings { get; set; } = new AuditSettings();
        public int TotalChecks { get; set; }
        public int PassedCount { get; set; }
        public int WarningCount { get; set; }
        public int ViolationCount { get; set; }
        
        public double CompliancePercentage => TotalChecks > 0 
            ? Math.Round(((double)PassedCount / TotalChecks) * 100.0, 1) 
            : 100.0;

        public List<AuditResultItem> Results { get; set; } = new List<AuditResultItem>();
    }

    public interface ISbcRule
    {
        string RuleId { get; }
        string Name { get; }
        string SbcReference { get; }
        SbcCategory Category { get; }
        IEnumerable<AuditResultItem> Validate(Document doc, AuditSettings settings);
    }
}
