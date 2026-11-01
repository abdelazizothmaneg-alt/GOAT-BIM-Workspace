using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SbcBaladyChecker.Core;
using SbcBaladyChecker.Services;
using SbcBaladyChecker.UI;

namespace SbcBaladyChecker.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AuditCommand : IExternalCommand
    {
        private static AuditWindow _currentWindow = null;
        private static RevitExternalEventHandler _eventHandler = null;
        private static ExternalEvent _externalEvent = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("SBC Auditor", "يرجى فتح مشروع ريفيت أولاً قبل تشغيل الفحص.");
                    return Result.Cancelled;
                }

                Document doc = uidoc.Document;

                // 1. Initial audit with default settings
                var auditEngine = new AuditEngine();
                AuditSettings defaultSettings = new AuditSettings { Typology = BuildingTypology.ResidentialVilla };
                AuditSummary summary = auditEngine.RunFullAudit(doc, defaultSettings);

                // 2. Initialize ExternalEvent for thread-safe modeless interactions
                if (_eventHandler == null)
                {
                    _eventHandler = new RevitExternalEventHandler();
                    _externalEvent = ExternalEvent.Create(_eventHandler);
                }

                // 3. Show Window (Modeless)
                if (_currentWindow != null && _currentWindow.IsLoaded)
                {
                    _currentWindow.Close();
                }

                _currentWindow = new AuditWindow(doc, summary, _externalEvent, _eventHandler);
                _currentWindow.Closed += (s, e) => _currentWindow = null;
                _currentWindow.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
