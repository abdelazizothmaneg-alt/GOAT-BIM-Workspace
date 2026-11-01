using System;
using System.Windows;
using ORTools.Commands;
using ORTools.ViewModels;

namespace ORTools.UI
{
    public partial class OrParameterWindow : Window
    {
        private readonly OrParameterViewModel _viewModel;

        public OrParameterWindow(OrParameterViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += (s, e) =>
            {
                _viewModel.TriggerExternalAction(OrParameterActionType.HarvestParameters);
            };
        }
    }
}
