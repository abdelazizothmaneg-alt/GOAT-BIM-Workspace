using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace ORTools.Models
{
    public class ElementTypeOption
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string Name { get; set; } = string.Empty;
        public double ThicknessMm { get; set; } = 15.0;

        public override string ToString() => Name;
    }

    public class RoomBoundarySegment2D
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public bool HasDoorOpening { get; set; }
        public bool IsColumnFace { get; set; }
    }

    public class RoomFinishItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _createsSummary = "W S F C";
        private string _healthStatus = "● Ready";
        private string _healthColor = "#10B981";

        public ElementId RoomId { get; set; } = ElementId.InvalidElementId;
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(RoomNumber) ? RoomName : $"{RoomNumber} - {RoomName}";
        public string LevelName { get; set; } = string.Empty;
        public double AreaSqm { get; set; }
        public double PerimeterMm { get; set; }
        public double UnboundedHeightMm { get; set; } = 3000.0;
        public int WallSegmentsCount { get; set; } = 4;
        public int DoorCount { get; set; } = 1;
        public bool IsEnclosed { get; set; } = true;
        public bool HasExistingGoatFinishes { get; set; }

        // Existing Room Parameter values
        public string WallFinishParam { get; set; } = string.Empty;
        public string BaseFinishParam { get; set; } = string.Empty;
        public string FloorFinishParam { get; set; } = string.Empty;
        public string CeilingFinishParam { get; set; } = string.Empty;

        public List<RoomBoundarySegment2D> Segments2D { get; set; } = new List<RoomBoundarySegment2D>();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CreatesSummary
        {
            get => _createsSummary;
            set
            {
                if (_createsSummary != value)
                {
                    _createsSummary = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HealthStatus
        {
            get => _healthStatus;
            set
            {
                if (_healthStatus != value)
                {
                    _healthStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HealthColor
        {
            get => _healthColor;
            set
            {
                if (_healthColor != value)
                {
                    _healthColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class GoatFinishesSettings
    {
        // Scope
        public bool ActiveViewOnly { get; set; } = true;

        // 1. Walls Tab
        public bool CreateWalls { get; set; } = true;
        public string SelectedWallTypeName { get; set; } = string.Empty;
        public bool WallSameOnAll { get; set; } = true; // false = Read from Room Data
        public bool WallBaseOnSlab { get; set; } = true; // false = Top of Finish Floor
        public bool WallTopAtCeiling { get; set; } = true; // false = Slab Above
        public double CeilingVoidOffsetMm { get; set; } = 150.0;
        public int ColumnHandlingMode { get; set; } = 0; // 0 = Wrap (Same as wall), 1 = Specific Type, 2 = Ignore
        public string SelectedColumnWallTypeName { get; set; } = string.Empty;

        // 2. Skirting Tab
        public bool CreateSkirting { get; set; } = true;
        public int SkirtingMethod { get; set; } = 0; // 0 = Wall Type (Recommended), 1 = Wall Sweep
        public string SelectedSkirtingTypeName { get; set; } = string.Empty;
        public double SkirtingHeightMm { get; set; } = 100.0;
        public bool SkirtingInFrontOfWall { get; set; } = true; // false = Below finish wall (Flush)
        public bool SkirtingJoinWithWall { get; set; } = true;

        // 3. Floors Tab
        public bool CreateFloors { get; set; } = true;
        public string SelectedFloorTypeName { get; set; } = string.Empty;
        public bool FloorSameOnAll { get; set; } = true; // false = Read from Room Data
        public double FloorOffsetMm { get; set; } = 0.0;
        public bool ExtendIntoDoorThresholds { get; set; } = true;
        public double DoorThresholdDepthMm { get; set; } = 100.0;

        // 4. Ceilings Tab
        public bool CreateCeilings { get; set; } = true;
        public string SelectedCeilingTypeName { get; set; } = string.Empty;
        public bool CeilingSameOnAll { get; set; } = true; // false = Read from Room Data
        public double CeilingHeightMm { get; set; } = 2700.0;

        // 5. Data Tab
        public bool WriteRoomFinishParameters { get; set; } = true;
        public bool SyncTypeNameToRoomParams { get; set; } = true;
        public string CustomWallFinishCode { get; set; } = "WF-01 (Paint)";
        public string CustomBaseFinishCode { get; set; } = "SK-01 (Wood 100mm)";
        public string CustomFloorFinishCode { get; set; } = "FF-01 (Porcelain)";
        public string CustomCeilingFinishCode { get; set; } = "CF-01 (Gypsum Board)";

        // 6. Options Tab
        public bool BreakWallsAtDoors { get; set; } = true;
        public bool AutoJoinWithHostWalls { get; set; } = true;
        public bool SmartReplaceExisting { get; set; } = true;
        public bool SkipUnenclosedRooms { get; set; } = true;
    }
}
