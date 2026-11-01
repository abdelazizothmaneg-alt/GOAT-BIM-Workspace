using System;
using System.IO;
using System.Linq;
using System.Text;
using SbcBaladyChecker.Core;

namespace SbcBaladyChecker.Services
{
    public class ReportGenerator
    {
        public static string GenerateHtmlReport(AuditSummary summary)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"ar\" dir=\"rtl\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"  <title>تقرير فحص كود البناء السعودي ومنصة بلدي - {summary.ProjectName}</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    :root {");
            sb.AppendLine("      --primary: #006C35; /* Saudi Green */");
            sb.AppendLine("      --primary-light: #E8F5E9;");
            sb.AppendLine("      --gold: #C5A059;");
            sb.AppendLine("      --danger: #D32F2F;");
            sb.AppendLine("      --danger-light: #FFEBEE;");
            sb.AppendLine("      --warning: #F57C00;");
            sb.AppendLine("      --warning-light: #FFF3E0;");
            sb.AppendLine("      --success: #2E7D32;");
            sb.AppendLine("      --success-light: #E8F5E9;");
            sb.AppendLine("      --dark: #1E293B;");
            sb.AppendLine("      --gray: #64748B;");
            sb.AppendLine("      --bg: #F8FAFC;");
            sb.AppendLine("      --card: #FFFFFF;");
            sb.AppendLine("    }");
            sb.AppendLine("    * { box-sizing: border-box; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; }");
            sb.AppendLine("    body { background: var(--bg); color: var(--dark); margin: 0; padding: 24px; direction: rtl; }");
            sb.AppendLine("    .container { max-width: 1100px; margin: 0 auto; }");
            sb.AppendLine("    .header { background: linear-gradient(135deg, #004d25, #006C35); color: white; padding: 32px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); }");
            sb.AppendLine("    .header h1 { margin: 0 0 8px 0; font-size: 26px; }");
            sb.AppendLine("    .header p { margin: 0; opacity: 0.9; font-size: 15px; }");
            sb.AppendLine("    .meta-bar { display: flex; gap: 24px; flex-wrap: wrap; margin-top: 20px; padding-top: 16px; border-top: 1px solid rgba(255,255,255,0.2); font-size: 14px; }");
            sb.AppendLine("    .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; margin-bottom: 24px; }");
            sb.AppendLine("    .stat-card { background: var(--card); padding: 20px; border-radius: 12px; border: 1px solid #E2E8F0; box-shadow: 0 2px 4px rgba(0,0,0,0.02); }");
            sb.AppendLine("    .stat-card.danger { border-top: 4px solid var(--danger); }");
            sb.AppendLine("    .stat-card.warning { border-top: 4px solid var(--warning); }");
            sb.AppendLine("    .stat-card.success { border-top: 4px solid var(--success); }");
            sb.AppendLine("    .stat-card.primary { border-top: 4px solid var(--primary); }");
            sb.AppendLine("    .stat-title { font-size: 13px; color: var(--gray); margin-bottom: 8px; }");
            sb.AppendLine("    .stat-value { font-size: 32px; font-weight: 700; }");
            sb.AppendLine("    .badge { display: inline-block; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }");
            sb.AppendLine("    .badge.violation { background: var(--danger-light); color: var(--danger); }");
            sb.AppendLine("    .badge.warning { background: var(--warning-light); color: var(--warning); }");
            sb.AppendLine("    .badge.compliant { background: var(--success-light); color: var(--success); }");
            sb.AppendLine("    .table-card { background: var(--card); border-radius: 16px; border: 1px solid #E2E8F0; overflow: hidden; margin-bottom: 24px; box-shadow: 0 4px 6px rgba(0,0,0,0.02); }");
            sb.AppendLine("    .table-header { padding: 18px 24px; border-bottom: 1px solid #E2E8F0; background: #FAFBFD; font-weight: 700; font-size: 16px; display: flex; justify-content: space-between; align-items: center; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; text-align: right; }");
            sb.AppendLine("    th { background: #F1F5F9; padding: 14px 16px; font-size: 13px; color: var(--dark); border-bottom: 1px solid #E2E8F0; }");
            sb.AppendLine("    td { padding: 14px 16px; font-size: 14px; border-bottom: 1px solid #F1F5F9; vertical-align: top; }");
            sb.AppendLine("    tr:hover { background: #F8FAFC; }");
            sb.AppendLine("    .recommendation { margin-top: 6px; padding: 8px 12px; background: #F8FAFC; border-right: 3px solid var(--gold); border-radius: 4px; font-size: 12px; color: #334155; }");
            sb.AppendLine("    .footer { text-align: center; color: var(--gray); font-size: 13px; padding: 24px 0; border-top: 1px solid #E2E8F0; }");
            sb.AppendLine("    @media print { body { padding: 0; background: white; } .header { border-radius: 0; box-shadow: none; } }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"container\">");

            // Header
            sb.AppendLine("    <div class=\"header\">");
            sb.AppendLine("      <h1>تقرير التدقيق المسبق لكود البناء السعودي ومنصة بلدي (SBC Audit)</h1>");
            sb.AppendLine($"      <p>فحص آلي شامل للمطابقة المعمارية، وسبل الهروب، ومواقف السيارات، واشتراطات الارتدادات البلدية - <strong>تصنيف المبنى: {summary.Settings?.TypologyLabel}</strong></p>");
            sb.AppendLine("      <div class=\"meta-bar\">");
            sb.AppendLine($"        <div><strong>المشروع:</strong> {summary.ProjectName}</div>");
            sb.AppendLine($"        <div><strong>الملف:</strong> {summary.DocumentTitle}</div>");
            sb.AppendLine($"        <div><strong>نوع الاستخدام:</strong> {summary.Settings?.TypologyLabel}</div>");
            sb.AppendLine($"        <div><strong>تاريخ الفحص:</strong> {summary.AuditDate:yyyy-MM-dd HH:mm}</div>");
            sb.AppendLine("        <div><strong>نظام الكود:</strong> SBC 201 / SBC 801 / بلدي</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");

            // Stats Cards
            sb.AppendLine("    <div class=\"stats-grid\">");
            sb.AppendLine("      <div class=\"stat-card primary\">");
            sb.AppendLine("        <div class=\"stat-title\">نسبة الامتثال للكود</div>");
            sb.AppendLine($"        <div class=\"stat-value\" style=\"color: var(--primary);\">{summary.CompliancePercentage}%</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <div class=\"stat-card danger\">");
            sb.AppendLine("        <div class=\"stat-title\">المخالفات الصريحة (تسبب الرفض)</div>");
            sb.AppendLine($"        <div class=\"stat-value\" style=\"color: var(--danger);\">{summary.ViolationCount}</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <div class=\"stat-card warning\">");
            sb.AppendLine("        <div class=\"stat-title\">الملاحظات والتنبيهات</div>");
            sb.AppendLine($"        <div class=\"stat-value\" style=\"color: var(--warning);\">{summary.WarningCount}</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <div class=\"stat-card success\">");
            sb.AppendLine("        <div class=\"stat-title\">البنود المطابقة</div>");
            sb.AppendLine($"        <div class=\"stat-value\" style=\"color: var(--success);\">{summary.PassedCount}</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");

            // Violations Table
            sb.AppendLine("    <div class=\"table-card\">");
            sb.AppendLine("      <div class=\"table-header\">");
            sb.AppendLine("        <span>سجل الملاحظات والمخالفات الفنية المفصلة</span>");
            sb.AppendLine($"        <span class=\"badge violation\">إجمالي البنود: {summary.TotalChecks}</span>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <table>");
            sb.AppendLine("        <thead>");
            sb.AppendLine("          <tr>");
            sb.AppendLine("            <th>الحالة</th>");
            sb.AppendLine("            <th>مرجع الكود</th>");
            sb.AppendLine("            <th>العنصر والمعرّف</th>");
            sb.AppendLine("            <th>الوصف والمخالفة</th>");
            sb.AppendLine("            <th>القيمة المسجلة</th>");
            sb.AppendLine("            <th>الحد النظامي للكود</th>");
            sb.AppendLine("          </tr>");
            sb.AppendLine("        </thead>");
            sb.AppendLine("        <tbody>");

            if (summary.Results.Count == 0)
            {
                sb.AppendLine("          <tr><td colspan=\"6\" style=\"text-align: center; padding: 32px; color: var(--success);\">تهانينا! لم يتم رصد أي مخالفات صريحة لكود البناء في هذا النموذج.</td></tr>");
            }
            else
            {
                foreach (var item in summary.Results)
                {
                    string badgeClass = item.Severity switch
                    {
                        RuleSeverity.Violation => "violation",
                        RuleSeverity.Warning => "warning",
                        _ => "compliant"
                    };

                    sb.AppendLine("          <tr>");
                    sb.AppendLine($"            <td><span class=\"badge {badgeClass}\">{item.SeverityLabel}</span></td>");
                    sb.AppendLine($"            <td><strong>{item.RuleCode}</strong><br><small style=\"color: var(--gray);\">{item.CategoryLabel}</small></td>");
                    sb.AppendLine($"            <td>{item.ElementName}<br><small style=\"color: var(--gray);\">ID: {item.ElementId}</small></td>");
                    sb.AppendLine("            <td>");
                    sb.AppendLine($"              <strong>{item.Title}</strong>");
                    sb.AppendLine($"              <div style=\"color: var(--gray); font-size: 13px; margin-top: 4px;\">{item.Description}</div>");
                    if (!string.IsNullOrWhiteSpace(item.Recommendation))
                    {
                        sb.AppendLine($"              <div class=\"recommendation\"><strong>💡 التوصية لتجاوز الفحص:</strong> {item.Recommendation}</div>");
                    }
                    sb.AppendLine("            </td>");
                    sb.AppendLine($"            <td style=\"font-weight: 600; color: {(item.Severity == RuleSeverity.Violation ? "var(--danger)" : "var(--dark)")};\">{item.CurrentValue}</td>");
                    sb.AppendLine($"            <td style=\"color: var(--primary); font-weight: 600;\">{item.AllowedRange}</td>");
                    sb.AppendLine("          </tr>");
                }
            }

            sb.AppendLine("        </tbody>");
            sb.AppendLine("      </table>");
            sb.AppendLine("    </div>");

            // Footer
            sb.AppendLine("    <div class=\"footer\">");
            sb.AppendLine("      تم إنشاء هذا التقرير تلقائياً بواسطة <strong>SBC Balady Code Auditor</strong> - حزمة GOAT BIM Suite للذكاء الاصطناعي والأتمتة الهندسية.");
            sb.AppendLine("    </div>");

            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public static string SaveReportToFile(AuditSummary summary, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SBC_Reports");
            }

            Directory.CreateDirectory(folderPath);

            string safeProjName = string.Join("_", (summary.ProjectName ?? "Project").Split(Path.GetInvalidFileNameChars()));
            string fileName = $"SBC_Audit_{safeProjName}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string fullPath = Path.Combine(folderPath, fileName);

            string html = GenerateHtmlReport(summary);
            File.WriteAllText(fullPath, html, Encoding.UTF8);

            return fullPath;
        }
    }
}
