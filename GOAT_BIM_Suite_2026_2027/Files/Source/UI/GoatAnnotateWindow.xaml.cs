using System;
using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ORTools.Models;

namespace ORTools.UI
{
    public class GoatAnnotateEventHandler : IExternalEventHandler
    {
        public GoatAnnotateWindow Window { get; set; }

        public void Execute(UIApplication uiapp)
        {
            if (Window == null) return;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc?.Document == null || uidoc.Document.ActiveView == null) return;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            try
            {
                var cats = Window.GetSelectedCategories();
                bool tagRooms = Window.ChkCatRooms.IsChecked == true;
                bool preventCollisions = Window.ChkPreventCollisions.IsChecked == true;
                bool addLeaders = Window.ChkAddLeaders.IsChecked == true;

                int count = 0;
                using (TransactionGroup tg = new TransactionGroup(doc, "GOAT ANNOTATE — Smart Batch Tagging"))
                {
                    tg.Start();
                    using (Transaction trans = new Transaction(doc, "Place Smart Tags"))
                    {
                        trans.Start();
                        count = GoatDocEngines.TagElementsInView(doc, view, cats, addLeaders, preventCollisions, tagRooms);
                        trans.Commit();
                    }
                    tg.Assimilate();
                }

                Window.TxtStatus.Text = $"✔ Placed {count} smart tags in '{view.Name}' with collision avoidance. (1x Ctrl+Z to Undo)";
            }
            catch (Exception ex)
            {
                Window.TxtStatus.Text = $"Error: {ex.Message}";
            }
        }

        public string GetName() => "GOAT ANNOTATE External Event Handler";
    }

    public partial class GoatAnnotateWindow : Window
    {
        private readonly GoatAnnotateEventHandler _handler;
        private readonly ExternalEvent _extEvent;

        public GoatAnnotateWindow(GoatAnnotateEventHandler handler, ExternalEvent extEvent)
        {
            InitializeComponent();
            _handler = handler;
            _extEvent = extEvent;
            _handler.Window = this;
        }

        public List<BuiltInCategory> GetSelectedCategories()
        {
            var list = new List<BuiltInCategory>();
            if (ChkCatDoors.IsChecked == true) list.Add(BuiltInCategory.OST_Doors);
            if (ChkCatWindows.IsChecked == true) list.Add(BuiltInCategory.OST_Windows);
            if (ChkCatWalls.IsChecked == true) list.Add(BuiltInCategory.OST_Walls);
            if (ChkCatFloors.IsChecked == true) list.Add(BuiltInCategory.OST_Floors);
            if (ChkCatCeilings.IsChecked == true) list.Add(BuiltInCategory.OST_Ceilings);
            if (ChkCatFurniture.IsChecked == true) list.Add(BuiltInCategory.OST_Furniture);
            if (ChkCatPlumbing.IsChecked == true) list.Add(BuiltInCategory.OST_PlumbingFixtures);
            if (ChkCatCasework.IsChecked == true) list.Add(BuiltInCategory.OST_Casework);
            if (ChkCatLighting.IsChecked == true) list.Add(BuiltInCategory.OST_LightingFixtures);
            return list;
        }

        private void BtnTag_Click(object sender, RoutedEventArgs e)
        {
            _extEvent.Raise();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
