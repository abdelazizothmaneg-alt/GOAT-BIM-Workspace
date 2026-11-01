using System;
using System.Reflection;
using Autodesk.Revit.UI;

namespace SbcBaladyChecker
{
    public class App : IExternalApplication
    {
        public const string TabName = "كود البناء SBC";
        public const string PanelName = "التدقيق المسبق ومنصة بلدي";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create custom Ribbon Tab
                try
                {
                    application.CreateRibbonTab(TabName);
                }
                catch
                {
                    // Tab might already exist if loaded in suite
                }

                RibbonPanel panel = null;
                var panels = application.GetRibbonPanels(TabName);
                foreach (var p in panels)
                {
                    if (p.Name == PanelName)
                    {
                        panel = p;
                        break;
                    }
                }

                if (panel == null)
                {
                    panel = application.CreateRibbonPanel(TabName, PanelName);
                }

                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // 1. Audit Button
                PushButtonData auditBtnData = new PushButtonData(
                    "cmdSbcAudit",
                    "فحص المشروع\n(SBC Audit)",
                    assemblyPath,
                    "SbcBaladyChecker.Commands.AuditCommand")
                {
                    ToolTip = "فحص آلي شامل للمخططات والنموذج لمطابقة كود البناء السعودي (SBC 201 / 801) واشتراطات منصة بلدي قبل رفع المعاملة."
                };

                panel.AddItem(auditBtnData);

                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
