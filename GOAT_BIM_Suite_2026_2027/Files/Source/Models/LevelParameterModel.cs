using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace ORTools.Models
{
    public class LevelTypeItem
    {
        public string Name { get; set; }
        public ElementId TypeId { get; set; }
        public override string ToString() => Name;
    }

    public class LevelParameterModel : INotifyPropertyChanged
    {
        private int _levelIndex;
        private string _levelNameParameter;
        private double _elevationParameterMeters;
        private bool _createLevelParameter;
        private bool _addToFloorPlansParameter;

        public int LevelIndex
        {
            get => _levelIndex;
            set { _levelIndex = value; OnPropertyChanged(); }
        }

        public string LevelNameParameter
        {
            get => _levelNameParameter;
            set { _levelNameParameter = value; OnPropertyChanged(); }
        }

        public double ElevationParameterMeters
        {
            get => _elevationParameterMeters;
            set { _elevationParameterMeters = value; OnPropertyChanged(); }
        }

        public bool CreateLevelParameter
        {
            get => _createLevelParameter;
            set { _createLevelParameter = value; OnPropertyChanged(); }
        }

        public bool AddToFloorPlansParameter
        {
            get => _addToFloorPlansParameter;
            set { _addToFloorPlansParameter = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
