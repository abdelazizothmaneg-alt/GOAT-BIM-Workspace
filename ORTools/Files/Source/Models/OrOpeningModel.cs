using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace ORTools.Models
{
    public class ReferenceInstanceItem
    {
        public ElementId InstanceId { get; set; }
        public string DisplayText { get; set; }
        public string LevelName { get; set; }
        public override string ToString() => DisplayText;
    }

    public class ComboOptionItem
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public class DimensionMatrixRow : INotifyPropertyChanged
    {
        private bool _inPlan;
        private bool _inSection;
        private bool _inElevation;

        public string DimensionName { get; set; }
        public bool CanPlan { get; set; } = true;
        public bool CanSection { get; set; } = true;
        public bool CanElevation { get; set; } = true;

        public bool InPlan
        {
            get => _inPlan;
            set { if (_inPlan != value) { _inPlan = value; OnPropertyChanged(nameof(InPlan)); } }
        }

        public bool InSection
        {
            get => _inSection;
            set { if (_inSection != value) { _inSection = value; OnPropertyChanged(nameof(InSection)); } }
        }

        public bool InElevation
        {
            get => _inElevation;
            set { if (_inElevation != value) { _inElevation = value; OnPropertyChanged(nameof(InElevation)); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class OpeningTypeItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _mark = string.Empty;
        private bool _isAutoNumbered = false;
        private string _dimensionsDetectionStatus = "✓ frame + leaf";
        private ReferenceInstanceItem _selectedReferenceInstance;

        public ElementId TypeId { get; set; }
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public string FamilyAndTypeName { get; set; }
        public int InstanceCount { get; set; }
        public string Dimensions { get; set; }

        public ObservableCollection<ReferenceInstanceItem> ReferenceInstances { get; } = new ObservableCollection<ReferenceInstanceItem>();

        public ReferenceInstanceItem SelectedReferenceInstance
        {
            get => _selectedReferenceInstance;
            set
            {
                if (_selectedReferenceInstance != value)
                {
                    _selectedReferenceInstance = value;
                    OnPropertyChanged(nameof(SelectedReferenceInstance));
                    OnPropertyChanged(nameof(ReferenceLevel));
                }
            }
        }

        public string ReferenceLevel => _selectedReferenceInstance != null ? _selectedReferenceInstance.LevelName : "00 - Ground Floor";

        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
        }

        public string Mark
        {
            get => _mark;
            set { if (_mark != value) { _mark = value; OnPropertyChanged(nameof(Mark)); } }
        }

        public bool IsAutoNumbered
        {
            get => _isAutoNumbered;
            set { if (_isAutoNumbered != value) { _isAutoNumbered = value; OnPropertyChanged(nameof(IsAutoNumbered)); } }
        }

        public string DimensionsDetectionStatus
        {
            get => _dimensionsDetectionStatus;
            set { if (_dimensionsDetectionStatus != value) { _dimensionsDetectionStatus = value; OnPropertyChanged(nameof(DimensionsDetectionStatus)); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
