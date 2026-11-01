using System;
using System.Diagnostics;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.UI;
using ORTools.ViewModels;

namespace ORTools.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class OrSheetLinkCommand : IExternalCommand
    {
        private static OrSheetLinkWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new OrSheetLinkExternalEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                var vm = new OrSheetLinkViewModel(handler, extEvent);

                _activeWindow = new OrSheetLinkWindow(vm);
                IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
                if (revitHandle != IntPtr.Zero)
                {
                    new WindowInteropHelper(_activeWindow).Owner = revitHandle;
                }

                _activeWindow.Closed += (s, e) => { _activeWindow = null; };
                _activeWindow.Show(); // Modeless window so user can interact with Revit and Excel simultaneously

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
