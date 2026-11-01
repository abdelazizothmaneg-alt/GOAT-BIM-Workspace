using System;
using System.Collections.Generic;
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace ORTools.Models
{
    public class OrParameterItemModel : INotifyPropertyChanged
    {
        private int _elementCount;

        public string Key => $"{Name}|{(IsInstance ? "Instance" : "Type")}";
        public string Name { get; set; }
        public bool IsInstance { get; set; }
        public string ParameterScopeLabel => IsInstance ? "Instance" : "Type";
        public StorageType DataStorageType { get; set; }
        public string StorageTypeLabel => DataStorageType.ToString();
        public bool IsStringParameter => DataStorageType == StorageType.String;
        public bool IsReadOnly { get; set; }

        // DiRoots OneParameter exact color coding: Light Green for Instance, Warm Amber/Orange for Type
        public string RowBackgroundHex => IsInstance ? "#E8F5E9" : "#FFF3E0";
        public string BadgeForegroundHex => IsInstance ? "#15803D" : "#B45309";

        // ElementIds of elements (or ElementTypes) that own this parameter in the current scope
        public List<ElementId> OwnerElementIds { get; set; } = new List<ElementId>();

        // For Type parameters, keep track of instance elements in scope so "Select in Revit" can highlight instances in the view
        public Dictionary<ElementId, List<ElementId>> TypeToInstanceIdsMap { get; set; } = new Dictionary<ElementId, List<ElementId>>();

        public int ElementCount
        {
            get => _elementCount;
            set
            {
                if (_elementCount != value)
                {
                    _elementCount = value;
                    OnPropertyChanged(nameof(ElementCount));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
