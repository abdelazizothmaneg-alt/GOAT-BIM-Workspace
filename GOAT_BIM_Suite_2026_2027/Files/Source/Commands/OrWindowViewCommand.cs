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
    public class OrWindowViewCommand : IExternalCommand
    {
        private static OrOpeningWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new OrOpeningExternalEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                var vm = new OrOpeningViewModel(handler, extEvent);

                _activeWindow = new OrOpeningWindow(vm, isDoor: false);
                IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
                if (revitHandle != IntPtr.Zero)
                {
                    new WindowInteropHelper(_activeWindow).Owner = revitHandle;
                }

                _activeWindow.Closed += (s, e) => { _activeWindow = null; };
                _activeWindow.Show();

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
