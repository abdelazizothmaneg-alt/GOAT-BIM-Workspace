using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using ORTools.Commands;
using ORTools.Models;

namespace ORTools.UI
{
    public partial class OrLevelWindow : Window
    {
        private readonly Document _doc;
        public ObservableCollection<LevelParameterModel> LevelParametersList { get; set; }

        public OrLevelWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            LevelParametersList = new ObservableCollection<LevelParameterModel>();
            DgLevelParameters.ItemsSource = LevelParametersList;

            LoadHeaderLogo();
            LoadLevelTypes();
            PopulateLevelParametersTable();
        }

        private void LoadHeaderLogo()
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string jpgPath = Path.Combine(assemblyDir, "Gemini_Generated_Image_36tlik36tlik36tl.jpg");
                if (File.Exists(jpgPath))
                {
                    BitmapImage bmp = new BitmapImage(new Uri(jpgPath, UriKind.Absolute));
                    ImgHeaderLogo.Source = bmp;
                }
            }
            catch
            {
            }
        }

        private void LoadLevelTypes()
        {
            try
            {
                var levelTypes = new FilteredElementCollector(_doc)
                    .OfClass(typeof(LevelType))
                    .Cast<LevelType>()
                    .ToList();

                var items = levelTypes.Select(lt => new LevelTypeItem
                {
                    Name = lt.Name,
                    TypeId = lt.Id
                }).ToList();

                CbLevelTypes.ItemsSource = items;
                if (items.Count > 0)
                {
                    CbLevelTypes.SelectedIndex = 0;
                }
            }
            catch
            {
            }
        }

        private void PopulateLevelParametersTable()
        {
            if (!int.TryParse(TxtCountParam.Text.Trim(), out int floorCount) || floorCount < 1 || floorCount > 250)
            {
                MessageBox.Show("Please enter a valid Floors Count between 1 and 250.", "OR LEVEL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(TxtStartElevParam.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double startElevMeters))
            {
                MessageBox.Show("Please enter a valid numeric Start Elevation (in meters).", "OR LEVEL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(TxtStepElevParam.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double floorHeightMeters))
            {
                MessageBox.Show("Please enter a valid numeric Floor Height (in meters).", "OR LEVEL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string prefix = TxtPrefixParam.Text;
            bool addToFloorPlans = ChkGlobalFloorPlansParam.IsChecked == true;

            string[] defaultArchFloorNames = { "Ground Floor", "First Floor", "Second Floor", "Third Floor", "Fourth Floor", "Roof Floor" };

            LevelParametersList.Clear();
            for (int i = 0; i < floorCount; i++)
            {
                double calculatedElevation = Math.Round(startElevMeters + (i * floorHeightMeters), 2);
                string defaultName;

                if (prefix.Trim().Equals("Level", StringComparison.OrdinalIgnoreCase) && i < defaultArchFloorNames.Length)
                {
                    defaultName = $"{i:00} - {defaultArchFloorNames[i]}";
                }
                else
                {
                    defaultName = $"{prefix}{i + 1:00}";
                }

                LevelParametersList.Add(new LevelParameterModel
                {
                    LevelIndex = i + 1,
                    CreateLevelParameter = true,
                    LevelNameParameter = defaultName,
                    ElevationParameterMeters = calculatedElevation,
                    AddToFloorPlansParameter = addToFloorPlans
                });
            }

            if (TxtStatus != null)
            {
                TxtStatus.Text = $"Prepared {floorCount} levels. Level Type properties will be applied.";
            }
        }

        private void BtnApplyParameters_Click(object sender, RoutedEventArgs e)
        {
            PopulateLevelParametersTable();
        }

        private void BtnAddFloorRow_Click(object sender, RoutedEventArgs e)
        {
            int nextIndex = LevelParametersList.Count + 1;
            double step = 3.60;
            double.TryParse(TxtStepElevParam.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out step);

            double lastElev = LevelParametersList.Count > 0 ? LevelParametersList.Last().ElevationParameterMeters : 0.0;
            double nextElev = LevelParametersList.Count > 0 ? Math.Round(lastElev + step, 2) : 0.0;

            LevelParametersList.Add(new LevelParameterModel
            {
                LevelIndex = nextIndex,
                CreateLevelParameter = true,
                LevelNameParameter = $"Level {nextIndex:00}",
                ElevationParameterMeters = nextElev,
                AddToFloorPlansParameter = ChkGlobalFloorPlansParam.IsChecked == true
            });
            TxtCountParam.Text = LevelParametersList.Count.ToString();
        }

        private void BtnRemoveFloorRow_Click(object sender, RoutedEventArgs e)
        {
            if (DgLevelParameters.SelectedItem is LevelParameterModel selected)
            {
                LevelParametersList.Remove(selected);
            }
            else if (LevelParametersList.Count > 0)
            {
                LevelParametersList.RemoveAt(LevelParametersList.Count - 1);
            }

            for (int i = 0; i < LevelParametersList.Count; i++)
            {
                LevelParametersList[i].LevelIndex = i + 1;
            }
            TxtCountParam.Text = LevelParametersList.Count.ToString();
        }

        private void ChkGlobalFloorPlansParam_Changed(object sender, RoutedEventArgs e)
        {
            if (LevelParametersList == null) return;
            bool addPlans = ChkGlobalFloorPlansParam.IsChecked == true;
            foreach (var item in LevelParametersList)
            {
                item.AddToFloorPlansParameter = addPlans;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCreateLevels_Click(object sender, RoutedEventArgs e)
        {
            var activeParams = LevelParametersList.Where(p => p.CreateLevelParameter).ToList();
            if (activeParams.Count == 0)
            {
                MessageBox.Show("Please select at least one level to process.", "OR LEVEL", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                bool globalFloorPlanOption = ChkGlobalFloorPlansParam.IsChecked == true;
                ElementId selectedTypeId = null;
                if (CbLevelTypes.SelectedItem is LevelTypeItem selItem)
                {
                    selectedTypeId = selItem.TypeId;
                }

                var result = OrLevelCommand.ExecuteCreateOrUpdateLevelsFromParameters(
                    _doc,
                    activeParams,
                    selectedTypeId,
                    globalFloorPlanOption
                );

                MessageBox.Show(
                    $"OR LEVEL Execution Completed Successfully:\n\n• Created New Levels: {result.CreatedLevels}\n• Updated Existing Levels: {result.UpdatedLevels}\n• Created Floor Plan Views: {result.CreatedFloorPlans}",
                    "OR LEVEL — Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to process levels:\n{ex.Message}",
                    "OR LEVEL — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
