using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ORTools.Models;

namespace ORTools.UI
{
    public class GoatDimEventHandler : IExternalEventHandler
    {
        public GoatDimWindow Window { get; set; }
        public bool IsScanOnly { get; set; }

        public void Execute(UIApplication uiapp)
        {
            if (Window == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc?.Document == null) return;
            Document doc = uidoc.Document;

            try
            {
                if (IsScanOnly)
                {
                    var dimTypes = new FilteredElementCollector(doc)
                        .OfClass(typeof(DimensionType))
                        .Cast<DimensionType>()
                        .Where(d => d.StyleType == DimensionStyleType.Linear)
                        .OrderBy(d => d.Name)
                        .Select(d => new ElementTypeOption { Id = d.Id, Name = d.Name })
                        .ToList();
                    Window.LoadDimTypes(dimTypes);
                    return;
                }

                View view = doc.ActiveView;
                if (view == null) return;

                DimensionType dimType = null;
                ElementId selId = Window.SelectedDimTypeId;
                if (selId != ElementId.InvalidElementId)
                    dimType = doc.GetElement(selId) as DimensionType;

                double.TryParse(Window.TxtWallOffset.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double wallMm);
                double.TryParse(Window.TxtStackOffset.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double stackMm);
                double.TryParse(Window.TxtExteriorOffset.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double extMm);
                bool showMm = Window.ChkShowMmSuffix.IsChecked == true;

                int created = 0;
                using (TransactionGroup tg = new TransactionGroup(doc, "GOAT DIM — Auto Dimensioning"))
                {
                    tg.Start();
                    using (Transaction trans = new Transaction(doc, "Create Dimensions"))
                    {
                        trans.Start();

                        if (Window.RbModeInteriorRooms.IsChecked == true)
                        {
                            var rooms = new FilteredElementCollector(doc, view.Id)
                                .OfCategory(BuiltInCategory.OST_Rooms)
                                .WhereElementIsNotElementType()
                                .OfType<Room>()
                                .Where(r => r.Area > 0.001)
                                .ToList();

                            foreach (Room r in rooms)
                            {
                                created += GoatDocEngines.DimensionRoomInPlan(
                                    doc,
                                    view,
                                    r,
                                    dimType,
                                    Window.ChkRoomClearCross.IsChecked == true,
                                    Window.ChkRoomOpenings.IsChecked == true,
                                    stackMm > 50 ? stackMm : 450,
                                    wallMm > 50 ? wallMm : 500,
                                    showMm);
                            }
                        }
                        else if (Window.RbModeExteriorBuilding.IsChecked == true)
                        {
                            created += GoatDocEngines.DimensionExteriorInPlan(
                                doc,
                                view,
                                dimType,
                                Window.ChkExtGrids.IsChecked == true,
                                true,
                                extMm > 100 ? extMm : 1200,
                                stackMm > 50 ? stackMm : 450,
                                showMm);
                        }
                        else
                        {
                            created += GoatDocEngines.DimensionElevationOrSection(doc, view, dimType, showMm);
                        }

                        trans.Commit();
                    }
                    tg.Assimilate();
                }

                Window.TxtStatus.Text = $"✔ Created {created} dimension chains in '{view.Name}'. (1x Ctrl+Z to Undo)";
            }
            catch (Exception ex)
            {
                Window.TxtStatus.Text = $"Error: {ex.Message}";
            }
        }

        public string GetName() => "GOAT DIM External Event Handler";
    }

    public partial class GoatDimWindow : Window
    {
        private readonly GoatDimEventHandler _handler;
        private readonly ExternalEvent _extEvent;

        public ElementId SelectedDimTypeId => (CbDimTypes.SelectedItem as ElementTypeOption)?.Id ?? ElementId.InvalidElementId;

        public GoatDimWindow(GoatDimEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;

            Loaded += (s, e) =>
            {
                _handler.IsScanOnly = true;
                _extEvent.Raise();
            };
        }

        public void LoadDimTypes(List<ElementTypeOption> types)
        {
            CbDimTypes.ItemsSource = types;
            if (types.Count > 0) CbDimTypes.SelectedIndex = 0;
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            _handler.IsScanOnly = false;
            _extEvent.Raise();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
