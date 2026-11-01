using System;
using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace ORTools.Models
{
    public class SheetLinkCategoryItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; }
        public BuiltInCategory BuiltInCat { get; set; }
        public ElementId CategoryId { get; set; }
        public bool IsSchedule { get; set; }
        public ElementId ScheduleId { get; set; }
        public int ElementCount { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string DisplayLabel => IsSchedule ? $"📊 {Name}" : $"{Name} ({ElementCount})";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class SheetLinkParameterColumn : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; }
        public bool IsInstance { get; set; }
        public bool IsReadOnly { get; set; }
        public StorageType DataStorageType { get; set; }

        public string ScopeBadge => IsReadOnly ? "Read-Only" : (IsInstance ? "Instance" : "Type");
        public string RowBackgroundHex => IsReadOnly ? "#F1F5F9" : (IsInstance ? "#E8F5E9" : "#FFF3E0");
        public string BadgeForegroundHex => IsReadOnly ? "#64748B" : (IsInstance ? "#15803D" : "#B45309");

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
