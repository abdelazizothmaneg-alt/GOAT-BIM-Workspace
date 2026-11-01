using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.UI;

namespace ORTools.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class OrSheetsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            OrSheetsWindow window = new OrSheetsWindow(doc);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
