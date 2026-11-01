using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SbcBaladyDemo
{
    public enum DemoSeverity { Compliant, Warning, Violation }
    public enum DemoCategory { Arch, Fire, Zoning, Parking, Struct }

    public class DemoResultItem
    {
        public string RuleCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DemoCategory Category { get; set; }
        public DemoSeverity Severity { get; set; }
        public string ElementId { get; set; }
        public string ElementName { get; set; }
        public string CurrentValue { get; set; }
        public string AllowedRange { get; set; }
        public string Recommendation { get; set; }

        public string SeverityLabel => Severity switch
        {
            DemoSeverity.Compliant => "مطابق للكود",
            DemoSeverity.Warning => "ملاحظة / تنبيه",
            DemoSeverity.Violation => "مخالفة صريحة (مرفوض)",
            _ => "غير محدد"
        };

        public string FullDescription => $"{Title} - {Description} [💡 التوصية: {Recommendation}]";
    }

    public partial class MainWindow : Window
    {
        private List<DemoResultItem> _currentItems = new List<DemoResultItem>();
        private string _currentProjectName = "مشروع فيلا الياسمين السكنية - الرياض";
        private string _currentTypologyLabel = "سكني - فيلا خاصة";

        public MainWindow()
        {
            InitializeComponent();
            LoadVillaData();
        }

        private void CmbTypology_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTypology == null || CmbTypology.SelectedItem is not ComboBoxItem item) return;

            string tag = item.Tag?.ToString();
            switch (tag)
            {
                case "Apartments":
                    LoadApartmentsData();
                    break;
                case "Retail":
                    LoadRetailData();
                    break;
                case "Office":
                    LoadOfficeData();
                    break;
                case "Mixed":
                    LoadMixedData();
                    break;
                default:
                    LoadVillaData();
                    break;
            }
        }

        private void BtnReAudit_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("⚡ تم إعادة فحص النموذج وتحديث مؤشرات الامتثال لكود البناء بلحظية!");
        }

        private void LoadVillaData()
        {
            _currentProjectName = "مشروع فيلا الياسمين السكنية - الرياض";
            _currentTypologyLabel = "سكني - فيلا خاصة";
            TxtTypologyHint.Text = "معايير الفلل: نسبة البناء 60%، ارتداد 2م، موقف سيارة واحد، عرض الدرج 900مم.";

            _currentItems = new List<DemoResultItem>
            {
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-GCR-01",
                    Title = "تجاوز نسبة البناء المسموحة للأرضي (64.2%)",
                    Description = "مساحة مسطح الأرضي هي 321 م² من إجمالي صك الأرض 500 م²، متجاوزاً الحد الأقصى 60%.",
                    Category = DemoCategory.Zoning,
                    Severity = DemoSeverity.Violation,
                    ElementId = "412891",
                    ElementName = "صك الأرض والحدود العقارية",
                    CurrentValue = "64.2%",
                    AllowedRange = "≤ 60.0%",
                    Recommendation = "تقليص مسطح الدور الأرضي بمقدار 21 م² أو زيادة الارتدادات لتفادي رفض رخصة بلدي."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1011.5.2",
                    Title = "ارتفاع قائمة درج المدخل يتجاوز الحد الأقصى للكود",
                    Description = "ارتفاع القائمة المسجل 195 مم، وهو أكبر من الحد الأقصى المسموح (180 مم).",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Violation,
                    ElementId = "389201",
                    ElementName = "درج المدخل الرئيسي (Main Stair)",
                    CurrentValue = "195 مم",
                    AllowedRange = "100 مم - 180 مم",
                    Recommendation = "زيادة عدد درجات السلم (Desired Risers) لخفض الارتفاع إلى 175 مم لتفادي الرفض في الفحص الفني."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1010.1.1",
                    Title = "عرض باب غرفة النوم الرئيسية أقل من الحد الموصى به",
                    Description = "عرض الباب 780 مم. الكود السكني يشترط ألا يقل العرض الصافي للأبواب السكنية عن 800 مم.",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Warning,
                    ElementId = "310245",
                    ElementName = "Single Flush 750x2100",
                    CurrentValue = "780 مم",
                    AllowedRange = "≥ 800 مم",
                    Recommendation = "ترقية فتحة الباب إلى مقاس 800 مم أو 900 مم لتسهيل الحركة ونقل الأثاث."
                },
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-PARKING-01",
                    Title = "موقف سيارة واحد داخل الملكية محقق ومطابق",
                    Description = "تم نمذجة كراج سيارة داخل حد الملكية مطابق لاشتراطات كود الفلل السكنية.",
                    Category = DemoCategory.Parking,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "501230",
                    ElementName = "Parking Space Standard",
                    CurrentValue = "1 موقف",
                    AllowedRange = "≥ 1 موقف",
                    Recommendation = "مطابق، لا يلزم إجراء."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1208.2",
                    Title = "الارتفاع الصافي لفراغ المجلس وصالة المعيشة مطابقة",
                    Description = "الارتفاع الصافي المسجل 3.10 م، وهو أعلى من الحد الأدنى الإلزامي (2.40 م).",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "290184",
                    ElementName = "مجلس الضيوف (رقم 102)",
                    CurrentValue = "3.10 م",
                    AllowedRange = "≥ 2.40 م",
                    Recommendation = "مطابق، لا يلزم إجراء."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1003.3",
                    Title = "الخلوص الرأسي للكمرات الإنشائية سليم ولا يتعارض مع الأبواب",
                    Description = "لم يتم رصد أي هبوط للكمرات الساقطة فوق مسارات ومداخل الأبواب.",
                    Category = DemoCategory.Struct,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "198273",
                    ElementName = "المسارات الإنشائية والمعمارية",
                    CurrentValue = "خلوص 2.45 م",
                    AllowedRange = "≥ 2.03 م",
                    Recommendation = "مطابق، لا يلزم إجراء."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1011.2",
                    Title = "عمق نائمة الدرج مطابقة للكود (300 مم)",
                    Description = "عمق النائمة المسجل 300 مم (الحد الأدنى 280 مم).",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "389201",
                    ElementName = "درج المدخل الرئيسي",
                    CurrentValue = "300 مم",
                    AllowedRange = "≥ 280 مم",
                    Recommendation = "مطابق، لا يلزم إجراء."
                }
            };

            UpdateUI();
        }

        private void LoadApartmentsData()
        {
            _currentProjectName = "مشروع أبراج السحاب وعمائر الشقق - مكة المكرمة";
            _currentTypologyLabel = "سكني - عمائر شقق سكنية";
            TxtTypologyHint.Text = "معايير عمائر الشقق: نسبة البناء 65%، 1.5 موقف/شقة + 10% زوار، عرض الممر 1.40م، الدرج 1100مم.";

            _currentItems = new List<DemoResultItem>
            {
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-PARKING-01",
                    Title = "عجز حاد في عدد مواقف السيارات الإلزامية لمنصة بلدي",
                    Description = "المتوفر في النموذج 8 مواقف، بينما المطلوب نظاماً 14 موقفاً (بناءً على 8 شقق سكنية + مواقف زوار).",
                    Category = DemoCategory.Parking,
                    Severity = DemoSeverity.Violation,
                    ElementId = "602194",
                    ElementName = "مواقف القبو والموقع العام",
                    CurrentValue = "8 مواقف",
                    AllowedRange = "≥ 14 موقف",
                    Recommendation = "إضافة 6 مواقف في طابق القبو (Basement) أو المواقف السطحية لتفادي رفض المعاملة فورياً."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1020.2",
                    Title = "عرض الممر المشترك للشقق أقل من الحد النظامي (1.40 م)",
                    Description = "عرض الممر المسجل هو 1.20 م فقط. الكود يشترط ألا يقل ممر العمارات المشتركة عن 1.40 م.",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Violation,
                    ElementId = "554102",
                    ElementName = "ممر الأدوار المتكررة (Corridor)",
                    CurrentValue = "1.20 م",
                    AllowedRange = "≥ 1.40 م",
                    Recommendation = "توسيع عرض ممر توزيع الشقق ليكون 1.40 م لضمان مسارات الإخلاء والهروب."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1011.2",
                    Title = "عرض شاحط درج الهروب أقل من 1100 مم للمباني متعددة الوحدات",
                    Description = "عرض شاحط الدرج المسجل 950 مم. الكود يلزم ألا يقل عن 1100 مم لعمائر الشقق السكنية.",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Violation,
                    ElementId = "478120",
                    ElementName = "سلم الطوارئ والهروب المشترك",
                    CurrentValue = "950 مم",
                    AllowedRange = "≥ 1100 مم",
                    Recommendation = "تعديل عرض شاحط السلم إلى 1.10 م على الأقل."
                },
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-GCR-01",
                    Title = "نسبة البناء في الدور الأرضي مطابقة لاشتراطات عمائر الشقق",
                    Description = "نسبة التغطية الأرضية 61.8% (ضمن الحد الأقصى المسموح 65%).",
                    Category = DemoCategory.Zoning,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "412891",
                    ElementName = "صك الأرض والحدود",
                    CurrentValue = "61.8%",
                    AllowedRange = "≤ 65.0%",
                    Recommendation = "مطابق، لا يلزم إجراء."
                }
            };

            UpdateUI();
        }

        private void LoadRetailData()
        {
            _currentProjectName = "مشروع سيتي ووك للمعارض والمحلات - الخبر";
            _currentTypologyLabel = "تجاري - محلات ومعارض";
            TxtTypologyHint.Text = "معايير التجاري: نسبة البناء 70%، موقف لكل 25م²، عرض الممر 1.80م، عروض الأبواب 900مم.";

            _currentItems = new List<DemoResultItem>
            {
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-PARKING-01",
                    Title = "عجز في مواقف السيارات للمركز التجاري (حساب المساحة الإجمالية GFA)",
                    Description = "المتوفر في النموذج 16 موقفاً، بينما المطلوب طبقاً لمعادلة بلدي (موقف لكل 25 م²) هو 36 موقفاً لمساحة 900 م².",
                    Category = DemoCategory.Parking,
                    Severity = DemoSeverity.Violation,
                    ElementId = "781023",
                    ElementName = "الموقع العام ومواقف الزوار",
                    CurrentValue = "16 موقف",
                    AllowedRange = "≥ 36 موقف",
                    Recommendation = "تأمين مواقف إضافية أو استخدام مواقف ذكية/ميكانيكية لتحقيق النصاب النظامي لمنصة بلدي."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1020.2",
                    Title = "عرض ممر حركة الجمهور التجاري أقل من 1.80 م",
                    Description = "عرض الممر بين المعارض 1.50 م، والكود يشترط ألا يقل ممر الجمهور التجاري عن 1.80 م.",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Violation,
                    ElementId = "661204",
                    ElementName = "الممر التجاري الرئيسي (Shopping Mall Concourse)",
                    CurrentValue = "1.50 م",
                    AllowedRange = "≥ 1.80 م",
                    Recommendation = "توسيع الممر إلى 1.80 م لضمان حركة سلسة للمتسوقين وعربات الخدمة."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1010.1.1",
                    Title = "أبواب المكاتب الخلفية أقل من الحد التجاري الإلزامي (900 مم)",
                    Description = "عرض الأبواب 800 مم، وفي المباني التجارية تلزم لائحة الكود بأن تكون جميع الأبواب 900 مم على الأقل.",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Violation,
                    ElementId = "552190",
                    ElementName = "أبواب مكاتب الإدارة والمستودع",
                    CurrentValue = "800 مم",
                    AllowedRange = "≥ 900 مم",
                    Recommendation = "ترقية الأبواب إلى مقاس 900 مم لتتوافق مع معايير الوصول والإخلاء التجاري."
                },
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-GCR-01",
                    Title = "نسبة البناء في الدور الأرضي مطابقة للمباني التجارية",
                    Description = "نسبة التغطية الأرضية 68.5% (ضمن الحد الأقصى 70% المسموح للمعارض التجارية).",
                    Category = DemoCategory.Zoning,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "412891",
                    ElementName = "حدود العقار التجاري",
                    CurrentValue = "68.5%",
                    AllowedRange = "≤ 70.0%",
                    Recommendation = "مطابق، لا يلزم إجراء."
                }
            };

            UpdateUI();
        }

        private void LoadOfficeData()
        {
            _currentProjectName = "مشروع برج الأعمال والمكاتب - جدة";
            _currentTypologyLabel = "إداري - مكاتب وشركات";
            TxtTypologyHint.Text = "معايير المكاتب: نسبة البناء 65%، موقف لكل 40م²، عرض الممر 1.80م، الدرج 1100مم.";

            _currentItems = new List<DemoResultItem>
            {
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1011.2",
                    Title = "عرض سلم الطوارئ الرئيسي أقل من 1100 مم لمبنى إداري",
                    Description = "عرض شاحط السلم 1000 مم، والمطلوب للمباني الإدارية العامة 1100 مم على الأقل.",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Violation,
                    ElementId = "441092",
                    ElementName = "سلم الهروب الإداري A",
                    CurrentValue = "1000 مم",
                    AllowedRange = "≥ 1100 مم",
                    Recommendation = "تعديل عرض شاحط السلم في الريفيت إلى 1.10 م."
                },
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-PARKING-01",
                    Title = "مواقف السيارات محققة للمبنى الإداري (موقف لكل 40 م²)",
                    Description = "المتوفر 26 موقفاً في القبو، والمطلوب طبقاً للمساحة الإجمالية 24 موقفاً.",
                    Category = DemoCategory.Parking,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "781023",
                    ElementName = "مواقف قبو المكاتب",
                    CurrentValue = "26 موقف",
                    AllowedRange = "≥ 24 موقف",
                    Recommendation = "مطابق، لا يلزم إجراء."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1020.2",
                    Title = "عرض ممر المكاتب الرئيسي مطابق (1.80 م)",
                    Description = "عرض الممر 1.85 م وهو أعلى من الحد الأدنى المطلوب للمكاتب العامة.",
                    Category = DemoCategory.Arch,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "552190",
                    ElementName = "ممر المكاتب التنفيذية",
                    CurrentValue = "1.85 م",
                    AllowedRange = "≥ 1.80 م",
                    Recommendation = "مطابق، لا يلزم إجراء."
                }
            };

            UpdateUI();
        }

        private void LoadMixedData()
        {
            _currentProjectName = "مشروع بوليفارد سنتر (سكني تجاري مختلط) - الدمام";
            _currentTypologyLabel = "سكني تجاري مختلط";
            TxtTypologyHint.Text = "معايير المختلط: نسبة البناء 70%، موقف لكل 35م²، عرض الممر 1.80م، الأبواب 900مم.";

            _currentItems = new List<DemoResultItem>
            {
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-PARKING-01",
                    Title = "عجز في مواقف السيارات للمبنى المختلط (المعارض والشقق)",
                    Description = "المتوفر 20 موقفاً، بينما المطلوب الإجمالي 32 موقفاً لخدمة المحلات والشقق السكنية العلوية.",
                    Category = DemoCategory.Parking,
                    Severity = DemoSeverity.Violation,
                    ElementId = "881023",
                    ElementName = "مواقف المشروع العامة",
                    CurrentValue = "20 موقف",
                    AllowedRange = "≥ 32 موقف",
                    Recommendation = "توسيع طابق القبو لتأمين 12 موقفاً إضافياً."
                },
                new DemoResultItem
                {
                    RuleCode = "SBC 201 - 1003.3",
                    Title = "تعارض كمرة ساقطة (Drop Beam) مع فتحة مدخل المعرض رقم 3",
                    Description = "الكمرة الإنشائية تهبط بمقدار 120 مم في فتحة الباب مما يخفض الخلوص إلى 1.95 م.",
                    Category = DemoCategory.Struct,
                    Severity = DemoSeverity.Violation,
                    ElementId = "619024",
                    ElementName = "كمرة B-12 مع باب مدخل معرض 3",
                    CurrentValue = "خلوص 1.95 م",
                    AllowedRange = "≥ 2.03 م",
                    Recommendation = "تحويل الكمرة إلى كمرة مدفونة أو نقل فتحة المدخل لتفادي تشويه الواجهة ورفض الرخصة."
                },
                new DemoResultItem
                {
                    RuleCode = "MOMRAH-GCR-01",
                    Title = "نسبة البناء الأرضي محققة (69.2%)",
                    Description = "نسبة التغطية الأرضية للمعارض 69.2% (ضمن الحد الأقصى 70%).",
                    Category = DemoCategory.Zoning,
                    Severity = DemoSeverity.Compliant,
                    ElementId = "412891",
                    ElementName = "صك العقار",
                    CurrentValue = "69.2%",
                    AllowedRange = "≤ 70.0%",
                    Recommendation = "مطابق، لا يلزم إجراء."
                }
            };

            UpdateUI();
        }

        private void UpdateUI()
        {
            int total = _currentItems.Count;
            int violations = _currentItems.Count(r => r.Severity == DemoSeverity.Violation);
            int warnings = _currentItems.Count(r => r.Severity == DemoSeverity.Warning);
            int passed = _currentItems.Count(r => r.Severity == DemoSeverity.Compliant);

            double score = total > 0 ? Math.Round(((double)passed / total) * 100.0, 1) : 100.0;

            TxtComplianceScore.Text = $"{score}%";
            TxtTotalChecks.Text = total.ToString();
            TxtViolations.Text = violations.ToString();
            TxtWarnings.Text = warnings.ToString();
            TxtPassed.Text = passed.ToString();

            GridResults.ItemsSource = _currentItems;
        }

        private void CmbFilterCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentItems == null || GridResults == null) return;

            string selected = (CmbFilterCategory.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filtered = selected switch
            {
                "المخالفات فقط (مرفوض)" => _currentItems.Where(r => r.Severity == DemoSeverity.Violation).ToList(),
                "التنبيهات فقط" => _currentItems.Where(r => r.Severity == DemoSeverity.Warning).ToList(),
                "المعماري (SBC 201)" => _currentItems.Where(r => r.Category == DemoCategory.Arch).ToList(),
                "اشتراطات بلدي والارتدادات" => _currentItems.Where(r => r.Category == DemoCategory.Zoning).ToList(),
                "معايير مواقف السيارات" => _currentItems.Where(r => r.Category == DemoCategory.Parking).ToList(),
                "التعارض الإنشائي" => _currentItems.Where(r => r.Category == DemoCategory.Struct).ToList(),
                _ => _currentItems
            };

            GridResults.ItemsSource = filtered;
        }

        private void BtnSelectElement_Click(object sender, RoutedEventArgs e)
        {
            if (GridResults.SelectedItem is DemoResultItem selected)
            {
                ShowToast($"🔍 محاكاة ريفيت: تم تحديد العنصر ({selected.ElementName}) ذي المعرّف (ID: {selected.ElementId}) وتكبير المنظور عليه.");
            }
            else
            {
                ShowToast("يرجى اختيار عنصر من الجدول أولاً لمعاينته.");
            }
        }

        private void BtnColorize_Click(object sender, RoutedEventArgs e)
        {
            int vCount = _currentItems.Count(r => r.Severity == DemoSeverity.Violation);
            int wCount = _currentItems.Count(r => r.Severity == DemoSeverity.Warning);
            ShowToast($"🟥 محاكاة ريفيت: تم تلوين {vCount} مخالفات صريحة باللون الأحمر، و {wCount} تنبيهات باللون الأصفر في الـ 3D View!");
        }

        private void BtnIsolate_Click(object sender, RoutedEventArgs e)
        {
            int vCount = _currentItems.Count(r => r.Severity == DemoSeverity.Violation);
            ShowToast($"👁️ محاكاة ريفيت: تم عزل العناصر المخالفة فقط ({vCount} عناصر) وإخفاء بقية المبنى مؤقتاً لدراستها.");
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string demoFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SBC_Reports");
                Directory.CreateDirectory(demoFolder);
                string filePath = Path.Combine(demoFolder, $"SBC_DEMO_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");

                string html = GenerateDemoHtmlReport();
                File.WriteAllText(filePath, html, Encoding.UTF8);

                ShowToast($"📄 تم تصدير تقرير بلدي المعتمد بنجاح وجارٍ فتحه في المتصفح...");

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء فتح التقرير: " + ex.Message);
            }
        }

        private void ShowToast(string message)
        {
            TxtStatusMessage.Text = message;
            StatusToast.Visibility = Visibility.Visible;
        }

        private string GenerateDemoHtmlReport()
        {
            int total = _currentItems.Count;
            int violations = _currentItems.Count(r => r.Severity == DemoSeverity.Violation);
            int warnings = _currentItems.Count(r => r.Severity == DemoSeverity.Warning);
            int passed = _currentItems.Count(r => r.Severity == DemoSeverity.Compliant);
            double score = total > 0 ? Math.Round(((double)passed / total) * 100.0, 1) : 100.0;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang=\"ar\" dir=\"rtl\"><head><meta charset=\"UTF-8\">");
            sb.AppendLine($"<title>تقرير فحص كود البناء السعودي ومنصة بلدي - {_currentProjectName}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(":root { --primary: #006C35; --danger: #D32F2F; --warning: #F57C00; --success: #2E7D32; --bg: #F8FAFC; }");
            sb.AppendLine("* { box-sizing: border-box; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; }");
            sb.AppendLine("body { background: var(--bg); margin: 0; padding: 24px; direction: rtl; }");
            sb.AppendLine(".container { max-width: 1050px; margin: 0 auto; }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #004d25, #006C35); color: white; padding: 28px; border-radius: 12px; margin-bottom: 20px; }");
            sb.AppendLine(".stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 14px; margin-bottom: 20px; }");
            sb.AppendLine(".card { background: white; padding: 18px; border-radius: 8px; border: 1px solid #E2E8F0; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; }");
            sb.AppendLine("th, td { padding: 12px 14px; text-align: right; border-bottom: 1px solid #F1F5F9; font-size: 13px; }");
            sb.AppendLine("th { background: #F1F5F9; }");
            sb.AppendLine(".badge { display: inline-block; padding: 4px 8px; border-radius: 12px; font-size: 11px; font-weight: bold; }");
            sb.AppendLine(".b-v { background: #FFEBEE; color: var(--danger); }");
            sb.AppendLine(".b-w { background: #FFF3E0; color: var(--warning); }");
            sb.AppendLine(".b-c { background: #E8F5E9; color: var(--success); }");
            sb.AppendLine("</style></head><body><div class=\"container\">");

            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine($"<h1>تقرير الفحص المسبق لكود البناء السعودي ومنصة بلدي (SBC Audit)</h1>");
            sb.AppendLine($"<p>المشروع: <strong>{_currentProjectName}</strong> | التصنيف: <strong>{_currentTypologyLabel}</strong> | التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class=\"stats\">");
            sb.AppendLine($"<div class=\"card\" style=\"border-top: 4px solid var(--primary);\"><div>نسبة الامتثال</div><div style=\"font-size: 28px; font-weight: bold; color: var(--primary);\">{score}%</div></div>");
            sb.AppendLine($"<div class=\"card\" style=\"border-top: 4px solid var(--danger);\"><div>مخالفات صريحة (مرفوض)</div><div style=\"font-size: 28px; font-weight: bold; color: var(--danger);\">{violations}</div></div>");
            sb.AppendLine($"<div class=\"card\" style=\"border-top: 4px solid var(--warning);\"><div>ملاحظات وتنبيهات</div><div style=\"font-size: 28px; font-weight: bold; color: var(--warning);\">{warnings}</div></div>");
            sb.AppendLine($"<div class=\"card\" style=\"border-top: 4px solid var(--success);\"><div>بنود مطابقة</div><div style=\"font-size: 28px; font-weight: bold; color: var(--success);\">{passed}</div></div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<table><thead><tr><th>الحالة</th><th>مرجع الكود</th><th>العنصر</th><th>الوصف والتوصية</th><th>المسجل</th><th>الحد النظامي</th><th>ID</th></tr></thead><tbody>");
            foreach (var item in _currentItems)
            {
                string bClass = item.Severity == DemoSeverity.Violation ? "b-v" : item.Severity == DemoSeverity.Warning ? "b-w" : "b-c";
                sb.AppendLine($"<tr><td><span class=\"badge {bClass}\">{item.SeverityLabel}</span></td><td><strong>{item.RuleCode}</strong></td><td>{item.ElementName}</td><td><strong>{item.Title}</strong><br><small style=\"color: #64748B;\">{item.Description}</small><br><small style=\"color: #006C35;\">💡 {item.Recommendation}</small></td><td style=\"font-weight: bold;\">{item.CurrentValue}</td><td style=\"color: var(--primary);\">{item.AllowedRange}</td><td>{item.ElementId}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("<p style=\"text-align: center; color: #94A3B8; font-size: 12px; margin-top: 24px;\">GOAT BIM Suite © 2026 - نظام الأتمتة والذكاء الاصطناعي لكود البناء السعودي ومنصة بلدي</p>");
            sb.AppendLine("</div></body></html>");

            return sb.ToString();
        }
    }
}
