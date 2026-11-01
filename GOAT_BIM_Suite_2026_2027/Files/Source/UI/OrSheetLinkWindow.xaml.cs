using System;
using System.Windows;
using ORTools.Commands;
using ORTools.ViewModels;

namespace ORTools.UI
{
    public partial class OrSheetLinkWindow : Window
    {
        private readonly OrSheetLinkViewModel _viewModel;

        public OrSheetLinkWindow(OrSheetLinkViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += (s, e) =>
            {
                _viewModel.TriggerExternal(OrSheetLinkActionType.ScanCategoriesAndSchedules);
            };
        }
    }
}
