using System;
using System.Diagnostics;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.UI;

namespace ORTools.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GoatCeilingCommand : IExternalCommand
    {
        private static GoatCeilingWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new GoatCeilingExternalEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                _activeWindow = new GoatCeilingWindow(handler, extEvent);
                SetRevitOwner(_activeWindow);
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

        internal static void SetRevitOwner(System.Windows.Window win)
        {
            IntPtr revitHandle = Process.GetCurrentProcess().MainWindowHandle;
            if (revitHandle != IntPtr.Zero)
            {
                new WindowInteropHelper(win).Owner = revitHandle;
            }
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GoatRoomCommand : IExternalCommand
    {
        private static GoatRoomWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new GoatRoomEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                _activeWindow = new GoatRoomWindow(handler, extEvent);
                GoatCeilingCommand.SetRevitOwner(_activeWindow);
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

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GoatDimCommand : IExternalCommand
    {
        private static GoatDimWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new GoatDimEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                _activeWindow = new GoatDimWindow(handler, extEvent);
                GoatCeilingCommand.SetRevitOwner(_activeWindow);
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

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GoatAnnotateCommand : IExternalCommand
    {
        private static GoatAnnotateWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new GoatAnnotateEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                _activeWindow = new GoatAnnotateWindow(handler, extEvent);
                GoatCeilingCommand.SetRevitOwner(_activeWindow);
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

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GoatRenumberCommand : IExternalCommand
    {
        private static GoatRenumberWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new GoatRenumberEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                _activeWindow = new GoatRenumberWindow(handler, extEvent);
                GoatCeilingCommand.SetRevitOwner(_activeWindow);
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

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GoatBoqCommand : IExternalCommand
    {
        private static GoatBoqWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new GoatBoqEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                _activeWindow = new GoatBoqWindow(handler, extEvent);
                GoatCeilingCommand.SetRevitOwner(_activeWindow);
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

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GoatAlignerCommand : IExternalCommand
    {
        private static GoatAlignerWindow _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    return Result.Succeeded;
                }

                var handler = new GoatAlignerEventHandler();
                ExternalEvent extEvent = ExternalEvent.Create(handler);
                _activeWindow = new GoatAlignerWindow(handler, extEvent);
                GoatCeilingCommand.SetRevitOwner(_activeWindow);
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
