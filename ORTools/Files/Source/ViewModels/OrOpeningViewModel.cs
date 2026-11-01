using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Autodesk.Revit.UI;
using ORTools.Commands;
using ORTools.Models;

namespace ORTools.ViewModels
{
    public class OrOpeningViewModel : INotifyPropertyChanged
    {
        private readonly OrOpeningExternalEventHandler _handler;
        private readonly ExternalEvent _externalEvent;

        // Mode
        private bool _isDoorMode = true;
        private bool _isWindowMode = false;

        // 1. Types scope
        private bool _allInProject = true;
        private bool _fromSelection = false;
        private string _allTypesHeader = "All door types in project (0)";
        private string _selectionTypesHeader = "From selection (0 types)";

        // 2. Mark (tag) settings
        private string _readMarkFromSource = "Type Mark";
        private string _customMarkParameterName = "";
        private bool _autoNumberEmptyMarks = true;
        private string _autoPrefix = "D";
        private int _autoDigits = 2;
        private bool _writeMarksBackToProject = true;

        // 3. Views to create (Plan, Section, Elevation, 3D Isolated)
        private bool _includePlan = true;
        private string _planSuffix = "Plan";
        private ComboOptionItem _selectedPlanViewType;
        private ComboOptionItem _selectedPlanTemplate;
        private int _planScale = 20;

        private bool _includeSection = true;
        private string _sectionSuffix = "Section";
        private ComboOptionItem _selectedSectionViewType;
        private ComboOptionItem _selectedSectionTemplate;
        private int _sectionScale = 20;

        private bool _includeElevation = true;
        private string _elevationSuffix = "Elevation";
        private ComboOptionItem _selectedElevationViewType;
        private ComboOptionItem _selectedElevationTemplate;
        private int _elevationScale = 20;

        private bool _includeIso = true;
        private string _isoSuffix = "ISO";
        private ComboOptionItem _selectedIsoViewType;
        private ComboOptionItem _selectedIsoTemplate;
        private int _isoScale = 20;

        // 4. Naming & options
        private string _namingPattern = "{Mark}{Sep}{Suffix}";
        private string _separator = "_";
        private double _cropOffsetMm = 600.0;
        private int _lookFromIndex = 0; // 0 = Exterior side (facing), 1 = Interior side
        private int _isolate3DModeIndex = 0; // 0 = Opening + host wall, 1 = Opening only, 2 = Full context
        private int _ifNameExistsIndex = 0; // 0 = Skip, 1 = Overwrite / Rename, 2 = Append number
        private int _openingCentreModeIndex = 0; // 0 = Middle of geometry (recommended), 1 = Family origin
        private double _sectionCutOffsetMm = 0.0;

        // 5. Section / Elevation vertical extent
        private int _bottomExtentModeIndex = 0; // 0 = Structural slab below - bottom face, 1 = Reference level, 2 = Opening bottom
        private double _bottomOffsetMm = -100.0;
        private int _topExtentModeIndex = 0; // 0 = Structural slab above - top face, 1 = Level above, 2 = Opening top
        private double _topOffsetMm = 100.0;
        private int _slabFilterModeIndex = 0; // 0 = Floor with Structural = Yes, 1 = Any Floor, 2 = Type name contains
        private string _slabFilterText = "Structural";
        private bool _useSameExtentForSectionAnd3D = true;

        // 6. Sheets (No schedules!)
        private bool _createSheets = true;
        private ComboOptionItem _selectedTitleBlock;
        private string _sheetNumberPattern = "DR-{Mark}";
        private string _sheetNamePattern = "{Mark} - {Type}";
        private int _sheetLayoutModeIndex = 0; // 0 = Auto: Plan + Elevation top · Section + 3D bottom

        // 7. Standard dimensions
        private bool _enableStandardDimensions = true;
        private int _referenceSourceIndex = 0; // 0 = Auto: family references, then frame / panel geometry (recommended)
        private int _ifRefNotFoundIndex = 0;   // 0 = Place a text label with the value, and flag it in the report
        private ComboOptionItem _selectedPlanDimType;
        private ComboOptionItem _selectedSectionDimType;
        private ComboOptionItem _selectedElevationDimType;
        private double _firstStringSpacingMm = 8.0;
        private double _betweenStringsSpacingMm = 7.0;
        private int _doubleDoorsDimModeIndex = 0; // 0 = Leaf width per leaf + overall frame

        // Expandable parameter names for text labels & Subcategory detection
        private string _paramFrameWidth = "Frame Width";
        private string _paramFrameHeight = "Frame Height";
        private string _paramLeafWidth = "Width";
        private string _paramLeafHeight = "Height";
        private string _paramFrameThickness = "Frame Depth";
        private string _paramFrameFaceWidth = "Frame Face";
        private string _paramLeafThickness = "Thickness";
        private string _paramUndercut = "Undercut";

        private string _frameSubcategoryKeywords = "Frame, Mullion, Jamb";
        private string _leafSubcategoryKeywords = "Panel, Leaf, Glass";

        // Progress & Status
        private double _progressValue = 0.0;
        private string _statusMessage = "Ready — All 7 engineering parameter groups configured.";

        // Collections
        public ObservableCollection<OpeningTypeItem> OpeningTypes { get; } = new ObservableCollection<OpeningTypeItem>();
        public ObservableCollection<string> ReadMarkSources { get; } = new ObservableCollection<string> { "Type Mark", "Mark", "Custom Parameter" };
        public ObservableCollection<int> AvailableScales { get; } = new ObservableCollection<int> { 5, 10, 20, 25, 50, 100 };

        public ObservableCollection<ComboOptionItem> PlanViewTypes { get; } = new ObservableCollection<ComboOptionItem>();
        public ObservableCollection<ComboOptionItem> SectionViewTypes { get; } = new ObservableCollection<ComboOptionItem>();
        public ObservableCollection<ComboOptionItem> ElevationViewTypes { get; } = new ObservableCollection<ComboOptionItem>();
        public ObservableCollection<ComboOptionItem> IsoViewTypes { get; } = new ObservableCollection<ComboOptionItem>();
        public ObservableCollection<ComboOptionItem> ViewTemplates { get; } = new ObservableCollection<ComboOptionItem>();
        public ObservableCollection<ComboOptionItem> TitleBlocks { get; } = new ObservableCollection<ComboOptionItem>();
        public ObservableCollection<ComboOptionItem> DimensionTypes { get; } = new ObservableCollection<ComboOptionItem>();

        public ObservableCollection<DimensionMatrixRow> DimensionRows { get; } = new ObservableCollection<DimensionMatrixRow>();

        public bool IsDoorMode
        {
            get => _isDoorMode;
            set
            {
                if (_isDoorMode != value)
                {
                    _isDoorMode = value;
                    OnPropertyChanged(nameof(IsDoorMode));
                    if (value)
                    {
                        _isWindowMode = false;
                        OnPropertyChanged(nameof(IsWindowMode));
                        AutoPrefix = "D";
                        SheetNumberPattern = "DR-{Mark}";
                        PlanScale = 10; SectionScale = 10; ElevationScale = 10; IsoScale = 10;
                    }
                }
            }
        }

        public bool IsWindowMode
        {
            get => _isWindowMode;
            set
            {
                if (_isWindowMode != value)
                {
                    _isWindowMode = value;
                    OnPropertyChanged(nameof(IsWindowMode));
                    if (value)
                    {
                        _isDoorMode = false;
                        OnPropertyChanged(nameof(IsDoorMode));
                        AutoPrefix = "W";
                        SheetNumberPattern = "WN-{Mark}";
                        PlanScale = 20; SectionScale = 20; ElevationScale = 20; IsoScale = 20;
                    }
                }
            }
        }

        public bool AllInProject { get => _allInProject; set { _allInProject = value; OnPropertyChanged(nameof(AllInProject)); } }
        public bool FromSelection { get => _fromSelection; set { _fromSelection = value; OnPropertyChanged(nameof(FromSelection)); } }
        public string AllTypesHeader { get => _allTypesHeader; set { _allTypesHeader = value; OnPropertyChanged(nameof(AllTypesHeader)); } }
        public string SelectionTypesHeader { get => _selectionTypesHeader; set { _selectionTypesHeader = value; OnPropertyChanged(nameof(SelectionTypesHeader)); } }

        // 2. Mark settings properties
        public string ReadMarkFromSource
        {
            get => _readMarkFromSource;
            set { _readMarkFromSource = value; OnPropertyChanged(nameof(ReadMarkFromSource)); OnPropertyChanged(nameof(IsCustomMarkParamEnabled)); }
        }
        public bool IsCustomMarkParamEnabled => _readMarkFromSource == "Custom Parameter";
        public string CustomMarkParameterName { get => _customMarkParameterName; set { _customMarkParameterName = value; OnPropertyChanged(nameof(CustomMarkParameterName)); } }
        public bool AutoNumberEmptyMarks { get => _autoNumberEmptyMarks; set { _autoNumberEmptyMarks = value; OnPropertyChanged(nameof(AutoNumberEmptyMarks)); } }
        public string AutoPrefix { get => _autoPrefix; set { _autoPrefix = value; OnPropertyChanged(nameof(AutoPrefix)); } }
        public int AutoDigits { get => _autoDigits; set { _autoDigits = value; OnPropertyChanged(nameof(AutoDigits)); } }
        public bool WriteMarksBackToProject { get => _writeMarksBackToProject; set { _writeMarksBackToProject = value; OnPropertyChanged(nameof(WriteMarksBackToProject)); } }

        // 3. Views properties
        public bool IncludePlan { get => _includePlan; set { _includePlan = value; OnPropertyChanged(nameof(IncludePlan)); } }
        public string PlanSuffix { get => _planSuffix; set { _planSuffix = value; OnPropertyChanged(nameof(PlanSuffix)); } }
        public ComboOptionItem SelectedPlanViewType { get => _selectedPlanViewType; set { _selectedPlanViewType = value; OnPropertyChanged(nameof(SelectedPlanViewType)); } }
        public ComboOptionItem SelectedPlanTemplate { get => _selectedPlanTemplate; set { _selectedPlanTemplate = value; OnPropertyChanged(nameof(SelectedPlanTemplate)); } }
        public int PlanScale { get => _planScale; set { _planScale = value; OnPropertyChanged(nameof(PlanScale)); } }

        public bool IncludeSection { get => _includeSection; set { _includeSection = value; OnPropertyChanged(nameof(IncludeSection)); } }
        public string SectionSuffix { get => _sectionSuffix; set { _sectionSuffix = value; OnPropertyChanged(nameof(SectionSuffix)); } }
        public ComboOptionItem SelectedSectionViewType { get => _selectedSectionViewType; set { _selectedSectionViewType = value; OnPropertyChanged(nameof(SelectedSectionViewType)); } }
        public ComboOptionItem SelectedSectionTemplate { get => _selectedSectionTemplate; set { _selectedSectionTemplate = value; OnPropertyChanged(nameof(SelectedSectionTemplate)); } }
        public int SectionScale { get => _sectionScale; set { _sectionScale = value; OnPropertyChanged(nameof(SectionScale)); } }

        public bool IncludeElevation { get => _includeElevation; set { _includeElevation = value; OnPropertyChanged(nameof(IncludeElevation)); } }
        public string ElevationSuffix { get => _elevationSuffix; set { _elevationSuffix = value; OnPropertyChanged(nameof(ElevationSuffix)); } }
        public ComboOptionItem SelectedElevationViewType { get => _selectedElevationViewType; set { _selectedElevationViewType = value; OnPropertyChanged(nameof(SelectedElevationViewType)); } }
        public ComboOptionItem SelectedElevationTemplate { get => _selectedElevationTemplate; set { _selectedElevationTemplate = value; OnPropertyChanged(nameof(SelectedElevationTemplate)); } }
        public int ElevationScale { get => _elevationScale; set { _elevationScale = value; OnPropertyChanged(nameof(ElevationScale)); } }

        public bool IncludeIso { get => _includeIso; set { _includeIso = value; OnPropertyChanged(nameof(IncludeIso)); } }
        public string IsoSuffix { get => _isoSuffix; set { _isoSuffix = value; OnPropertyChanged(nameof(IsoSuffix)); } }
        public ComboOptionItem SelectedIsoViewType { get => _selectedIsoViewType; set { _selectedIsoViewType = value; OnPropertyChanged(nameof(SelectedIsoViewType)); } }
        public ComboOptionItem SelectedIsoTemplate { get => _selectedIsoTemplate; set { _selectedIsoTemplate = value; OnPropertyChanged(nameof(SelectedIsoTemplate)); } }
        public int IsoScale { get => _isoScale; set { _isoScale = value; OnPropertyChanged(nameof(IsoScale)); } }

        // 4. Naming & options properties
        public string NamingPattern { get => _namingPattern; set { _namingPattern = value; OnPropertyChanged(nameof(NamingPattern)); } }
        public string Separator { get => _separator; set { _separator = value; OnPropertyChanged(nameof(Separator)); } }
        public double CropOffsetMm { get => _cropOffsetMm; set { _cropOffsetMm = value; OnPropertyChanged(nameof(CropOffsetMm)); } }
        public int LookFromIndex { get => _lookFromIndex; set { _lookFromIndex = value; OnPropertyChanged(nameof(LookFromIndex)); } }
        public int Isolate3DModeIndex { get => _isolate3DModeIndex; set { _isolate3DModeIndex = value; OnPropertyChanged(nameof(Isolate3DModeIndex)); } }
        public int IfNameExistsIndex { get => _ifNameExistsIndex; set { _ifNameExistsIndex = value; OnPropertyChanged(nameof(IfNameExistsIndex)); } }
        public int OpeningCentreModeIndex { get => _openingCentreModeIndex; set { _openingCentreModeIndex = value; OnPropertyChanged(nameof(OpeningCentreModeIndex)); } }
        public double SectionCutOffsetMm { get => _sectionCutOffsetMm; set { _sectionCutOffsetMm = value; OnPropertyChanged(nameof(SectionCutOffsetMm)); } }

        // 5. Vertical extent properties
        public int BottomExtentModeIndex { get => _bottomExtentModeIndex; set { _bottomExtentModeIndex = value; OnPropertyChanged(nameof(BottomExtentModeIndex)); } }
        public double BottomOffsetMm { get => _bottomOffsetMm; set { _bottomOffsetMm = value; OnPropertyChanged(nameof(BottomOffsetMm)); } }
        public int TopExtentModeIndex { get => _topExtentModeIndex; set { _topExtentModeIndex = value; OnPropertyChanged(nameof(TopExtentModeIndex)); } }
        public double TopOffsetMm { get => _topOffsetMm; set { _topOffsetMm = value; OnPropertyChanged(nameof(TopOffsetMm)); } }
        public int SlabFilterModeIndex { get => _slabFilterModeIndex; set { _slabFilterModeIndex = value; OnPropertyChanged(nameof(SlabFilterModeIndex)); } }
        public string SlabFilterText { get => _slabFilterText; set { _slabFilterText = value; OnPropertyChanged(nameof(SlabFilterText)); } }
        public bool UseSameExtentForSectionAnd3D { get => _useSameExtentForSectionAnd3D; set { _useSameExtentForSectionAnd3D = value; OnPropertyChanged(nameof(UseSameExtentForSectionAnd3D)); } }

        // 6. Sheets properties
        public bool CreateSheets { get => _createSheets; set { _createSheets = value; OnPropertyChanged(nameof(CreateSheets)); } }
        public ComboOptionItem SelectedTitleBlock { get => _selectedTitleBlock; set { _selectedTitleBlock = value; OnPropertyChanged(nameof(SelectedTitleBlock)); } }
        public string SheetNumberPattern { get => _sheetNumberPattern; set { _sheetNumberPattern = value; OnPropertyChanged(nameof(SheetNumberPattern)); } }
        public string SheetNamePattern { get => _sheetNamePattern; set { _sheetNamePattern = value; OnPropertyChanged(nameof(SheetNamePattern)); } }
        public int SheetLayoutModeIndex { get => _sheetLayoutModeIndex; set { _sheetLayoutModeIndex = value; OnPropertyChanged(nameof(SheetLayoutModeIndex)); } }

        // 7. Standard dimensions properties
        public bool EnableStandardDimensions { get => _enableStandardDimensions; set { _enableStandardDimensions = value; OnPropertyChanged(nameof(EnableStandardDimensions)); } }
        public int ReferenceSourceIndex { get => _referenceSourceIndex; set { _referenceSourceIndex = value; OnPropertyChanged(nameof(ReferenceSourceIndex)); } }
        public int IfRefNotFoundIndex { get => _ifRefNotFoundIndex; set { _ifRefNotFoundIndex = value; OnPropertyChanged(nameof(IfRefNotFoundIndex)); } }
        public ComboOptionItem SelectedPlanDimType { get => _selectedPlanDimType; set { _selectedPlanDimType = value; OnPropertyChanged(nameof(SelectedPlanDimType)); } }
        public ComboOptionItem SelectedSectionDimType { get => _selectedSectionDimType; set { _selectedSectionDimType = value; OnPropertyChanged(nameof(SelectedSectionDimType)); } }
        public ComboOptionItem SelectedElevationDimType { get => _selectedElevationDimType; set { _selectedElevationDimType = value; OnPropertyChanged(nameof(SelectedElevationDimType)); } }
        public double FirstStringSpacingMm { get => _firstStringSpacingMm; set { _firstStringSpacingMm = value; OnPropertyChanged(nameof(FirstStringSpacingMm)); } }
        public double BetweenStringsSpacingMm { get => _betweenStringsSpacingMm; set { _betweenStringsSpacingMm = value; OnPropertyChanged(nameof(BetweenStringsSpacingMm)); } }
        public int DoubleDoorsDimModeIndex { get => _doubleDoorsDimModeIndex; set { _doubleDoorsDimModeIndex = value; OnPropertyChanged(nameof(DoubleDoorsDimModeIndex)); } }

        public string ParamFrameWidth { get => _paramFrameWidth; set { _paramFrameWidth = value; OnPropertyChanged(nameof(ParamFrameWidth)); } }
        public string ParamFrameHeight { get => _paramFrameHeight; set { _paramFrameHeight = value; OnPropertyChanged(nameof(ParamFrameHeight)); } }
        public string ParamLeafWidth { get => _paramLeafWidth; set { _paramLeafWidth = value; OnPropertyChanged(nameof(ParamLeafWidth)); } }
        public string ParamLeafHeight { get => _paramLeafHeight; set { _paramLeafHeight = value; OnPropertyChanged(nameof(ParamLeafHeight)); } }
        public string ParamFrameThickness { get => _paramFrameThickness; set { _paramFrameThickness = value; OnPropertyChanged(nameof(ParamFrameThickness)); } }
        public string ParamFrameFaceWidth { get => _paramFrameFaceWidth; set { _paramFrameFaceWidth = value; OnPropertyChanged(nameof(ParamFrameFaceWidth)); } }
        public string ParamLeafThickness { get => _paramLeafThickness; set { _paramLeafThickness = value; OnPropertyChanged(nameof(ParamLeafThickness)); } }
        public string ParamUndercut { get => _paramUndercut; set { _paramUndercut = value; OnPropertyChanged(nameof(ParamUndercut)); } }

        public string FrameSubcategoryKeywords { get => _frameSubcategoryKeywords; set { _frameSubcategoryKeywords = value; OnPropertyChanged(nameof(FrameSubcategoryKeywords)); } }
        public string LeafSubcategoryKeywords { get => _leafSubcategoryKeywords; set { _leafSubcategoryKeywords = value; OnPropertyChanged(nameof(LeafSubcategoryKeywords)); } }

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(nameof(ProgressValue)); OnPropertyChanged(nameof(ProgressText)); }
        }

        public string ProgressText => $"Completed {ProgressValue:0}%";
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }

        public ICommand ScanModelCommand { get; }
        public ICommand RunPreCheckCommand { get; }
        public ICommand GenerateDetailsCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand SelectNoneCommand { get; }

        public OrOpeningViewModel(OrOpeningExternalEventHandler handler, ExternalEvent extEvent)
        {
            _handler = handler;
            _externalEvent = extEvent;
            _handler.ViewModel = this;

            InitDefaultDimensionMatrix();

            ScanModelCommand = new RelayCommand(_ => TriggerAction(OpeningActionType.HarvestOpeningTypes));
            RunPreCheckCommand = new RelayCommand(_ => TriggerAction(OpeningActionType.RunPreCheckReport));
            GenerateDetailsCommand = new RelayCommand(_ => TriggerAction(OpeningActionType.GenerateOpeningDetails));
            SelectAllCommand = new RelayCommand(_ => { foreach (var i in OpeningTypes) i.IsSelected = true; });
            SelectNoneCommand = new RelayCommand(_ => { foreach (var i in OpeningTypes) i.IsSelected = false; });
        }

        private void InitDefaultDimensionMatrix()
        {
            DimensionRows.Clear();
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Frame width (overall)", InPlan = true, CanSection = false, InElevation = true });
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Frame height (overall)", CanPlan = false, InSection = true, InElevation = true });
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Leaf / Glazing width", InPlan = true, CanSection = false, InElevation = true });
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Leaf / Glazing height", CanPlan = false, InSection = true, InElevation = true });
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Frame thickness (jamb / head depth)", InPlan = true, InSection = true, CanElevation = false });
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Frame face width", InPlan = false, InSection = false, InElevation = false });
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Leaf / Panel thickness", InPlan = false, InSection = false, CanElevation = false });
            DimensionRows.Add(new DimensionMatrixRow { DimensionName = "Undercut / Sill height", CanPlan = false, InSection = false, InElevation = false });
        }

        public void TriggerAction(OpeningActionType action)
        {
            _handler.CurrentAction = action;
            _externalEvent.Raise();
        }

        public void UpdateOpeningTypes(List<OpeningTypeItem> items, int selCount)
        {
            OpeningTypes.Clear();
            foreach (var it in items) OpeningTypes.Add(it);
            string catWord = IsDoorMode ? "door" : "window";
            AllTypesHeader = $"All {catWord} types in project ({items.Count})";
            SelectionTypesHeader = $"From selection ({selCount} types)";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
