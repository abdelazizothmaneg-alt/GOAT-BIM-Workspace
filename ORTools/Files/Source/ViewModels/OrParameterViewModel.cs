using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.UI;
using ORTools.Commands;
using ORTools.Models;

namespace ORTools.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class OrParameterViewModel : INotifyPropertyChanged
    {
        private readonly OrParameterExternalEventHandler _handler;
        private readonly ExternalEvent _externalEvent;
        private List<OrParameterItemModel> _allParameters = new List<OrParameterItemModel>();

        private bool _isWholeModelScope = true;
        private bool _isActiveViewScope = false;
        private int _scopeFilterModeIndex = 0; // 0 = Instance,Type | 1 = Instance | 2 = Type
        private string _searchParameterText = string.Empty;
        private OrParameterItemModel _selectedParameter;

        private string _valueFilterText = string.Empty;
        private bool _isCaseSensitive = false;
        private string _prefixText = string.Empty;
        private string _suffixText = string.Empty;
        private string _overwriteValueText = string.Empty;

        private double _progressValue = 0.0;
        private int _totalItemsCount = 0;
        private string _statusMessage = "Ready — Select a parameter to bulk edit.";

        public ObservableCollection<OrParameterItemModel> FilteredParameters { get; } = new ObservableCollection<OrParameterItemModel>();

        public bool IsWholeModelScope
        {
            get => _isWholeModelScope;
            set
            {
                if (_isWholeModelScope != value)
                {
                    _isWholeModelScope = value;
                    OnPropertyChanged(nameof(IsWholeModelScope));
                    if (value)
                    {
                        _isActiveViewScope = false;
                        OnPropertyChanged(nameof(IsActiveViewScope));
                        TriggerExternalAction(OrParameterActionType.HarvestParameters);
                    }
                }
            }
        }

        public bool IsActiveViewScope
        {
            get => _isActiveViewScope;
            set
            {
                if (_isActiveViewScope != value)
                {
                    _isActiveViewScope = value;
                    OnPropertyChanged(nameof(IsActiveViewScope));
                    if (value)
                    {
                        _isWholeModelScope = false;
                        OnPropertyChanged(nameof(IsWholeModelScope));
                        TriggerExternalAction(OrParameterActionType.HarvestParameters);
                    }
                }
            }
        }

        public int ScopeFilterModeIndex
        {
            get => _scopeFilterModeIndex;
            set
            {
                if (_scopeFilterModeIndex != value)
                {
                    _scopeFilterModeIndex = value;
                    OnPropertyChanged(nameof(ScopeFilterModeIndex));
                    ApplyParameterListFilter();
                }
            }
        }

        public string SearchParameterText
        {
            get => _searchParameterText;
            set
            {
                if (_searchParameterText != value)
                {
                    _searchParameterText = value;
                    OnPropertyChanged(nameof(SearchParameterText));
                    ApplyParameterListFilter();
                }
            }
        }

        public OrParameterItemModel SelectedParameter
        {
            get => _selectedParameter;
            set
            {
                if (_selectedParameter != value)
                {
                    _selectedParameter = value;
                    OnPropertyChanged(nameof(SelectedParameter));
                    OnPropertyChanged(nameof(IsPrefixSuffixEnabled));
                    OnPropertyChanged(nameof(IsParameterSelected));
                    TotalItemsCount = _selectedParameter != null ? _selectedParameter.ElementCount : 0;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsParameterSelected => SelectedParameter != null;
        public bool IsPrefixSuffixEnabled => SelectedParameter != null && SelectedParameter.IsStringParameter;

        public string ValueFilterText
        {
            get => _valueFilterText;
            set { _valueFilterText = value; OnPropertyChanged(nameof(ValueFilterText)); }
        }

        public bool IsCaseSensitive
        {
            get => _isCaseSensitive;
            set { _isCaseSensitive = value; OnPropertyChanged(nameof(IsCaseSensitive)); }
        }

        public string PrefixText
        {
            get => _prefixText;
            set { _prefixText = value; OnPropertyChanged(nameof(PrefixText)); }
        }

        public string SuffixText
        {
            get => _suffixText;
            set { _suffixText = value; OnPropertyChanged(nameof(SuffixText)); }
        }

        public string OverwriteValueText
        {
            get => _overwriteValueText;
            set { _overwriteValueText = value; OnPropertyChanged(nameof(OverwriteValueText)); }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        public string ProgressText => $"Completed {ProgressValue:0}%";

        public int TotalItemsCount
        {
            get => _totalItemsCount;
            set
            {
                _totalItemsCount = value;
                OnPropertyChanged(nameof(TotalItemsCount));
                OnPropertyChanged(nameof(TotalItemsLabel));
            }
        }

        public string TotalItemsLabel => $"Total Items: {TotalItemsCount}";

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        // Commands
        public ICommand ClearInputsCommand { get; }
        public ICommand SortInstanceCommand { get; }
        public ICommand SortTypeCommand { get; }
        public ICommand FindByValueCommand { get; }
        public ICommand SelectInRevitCommand { get; }
        public ICommand AddPrefixSuffixCommand { get; }
        public ICommand OverwriteValueCommand { get; }
        public ICommand ClearValuesCommand { get; }
        public ICommand ColorSplashCommand { get; }
        public ICommand ResetColorSplashCommand { get; }
        public ICommand InjectRoomDataCommand { get; }

        public OrParameterViewModel(OrParameterExternalEventHandler handler, ExternalEvent externalEvent)
        {
            _handler = handler;
            _externalEvent = externalEvent;
            _handler.ViewModel = this;

            ClearInputsCommand = new RelayCommand(_ => ExecuteClearInputs());
            SortInstanceCommand = new RelayCommand(_ => ExecuteSortBy(true));
            SortTypeCommand = new RelayCommand(_ => ExecuteSortBy(false));
            FindByValueCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.FilterByValue), _ => IsParameterSelected);
            SelectInRevitCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.SelectInRevit), _ => IsParameterSelected);
            AddPrefixSuffixCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.ApplyPrefixSuffix), _ => IsPrefixSuffixEnabled);
            OverwriteValueCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.OverwriteValue), _ => IsParameterSelected);
            ClearValuesCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.ClearValues), _ => IsParameterSelected);
            ColorSplashCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.ColorSplashActiveView), _ => IsParameterSelected);
            ResetColorSplashCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.ResetColorSplash));
            InjectRoomDataCommand = new RelayCommand(_ => TriggerExternalAction(OrParameterActionType.InjectRoomAndLevelData), _ => IsPrefixSuffixEnabled);
        }

        public void TriggerExternalAction(OrParameterActionType action)
        {
            _handler.CurrentAction = action;
            _externalEvent.Raise();
        }

        public void UpdateHarvestedParameters(List<OrParameterItemModel> items)
        {
            _allParameters = items ?? new List<OrParameterItemModel>();
            ApplyParameterListFilter();
        }

        private void ApplyParameterListFilter()
        {
            string q = (SearchParameterText ?? string.Empty).Trim().ToLowerInvariant();
            FilteredParameters.Clear();

            foreach (var it in _allParameters)
            {
                if (ScopeFilterModeIndex == 1 && !it.IsInstance) continue;
                if (ScopeFilterModeIndex == 2 && it.IsInstance) continue;

                if (!string.IsNullOrEmpty(q) && !it.Name.ToLowerInvariant().Contains(q))
                    continue;

                FilteredParameters.Add(it);
            }

            if (SelectedParameter == null && FilteredParameters.Count > 0)
            {
                SelectedParameter = FilteredParameters[0];
            }
        }

        private void ExecuteSortBy(bool instanceFirst)
        {
            var sorted = instanceFirst
                ? FilteredParameters.OrderByDescending(x => x.IsInstance).ThenBy(x => x.Name).ToList()
                : FilteredParameters.OrderBy(x => x.IsInstance).ThenBy(x => x.Name).ToList();

            FilteredParameters.Clear();
            foreach (var it in sorted) FilteredParameters.Add(it);
        }

        private void ExecuteClearInputs()
        {
            SearchParameterText = string.Empty;
            ScopeFilterModeIndex = 0;
            ValueFilterText = string.Empty;
            IsCaseSensitive = false;
            PrefixText = string.Empty;
            SuffixText = string.Empty;
            OverwriteValueText = string.Empty;
            ProgressValue = 0;
            StatusMessage = "Cleared all input fields.";
            TriggerExternalAction(OrParameterActionType.HarvestParameters);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
