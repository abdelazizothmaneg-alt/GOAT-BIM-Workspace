using System;
using System.Windows;
using ORTools.Commands;
using ORTools.ViewModels;

namespace ORTools.UI
{
    public partial class OrOpeningWindow : Window
    {
        private readonly OrOpeningViewModel _viewModel;

        public OrOpeningWindow(OrOpeningViewModel viewModel, bool isDoor)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Populate curly-brace token strings in code-behind to avoid XAML MC3074 markup extension errors
            LblNamingTokensHelp.Text = "Tokens: {Mark} {Sep} {Suffix} {Level} {Family} {Type} – also used in the sheet number / name.";
            LblSeparatorHeader.Text = "Separator {Sep}";

            if (!isDoor)
            {
                Title = "GOAT BIM | W VIEW — Parametric Window 4-View Shop Drawing & Sheet Engine";
                HeaderTitle.Text = "GOAT W VIEW — Parametric Window 4-View Shop Drawing & Sheet Engine";
                GrpTypesHeader.Header = "1 · Window types";
                try
                {
                    HeaderLogo.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("pack://application:,,,/ORTools;component/Resources/WINDOW_VIEW.jpg", UriKind.Absolute));
                }
                catch { }
                _viewModel.IsWindowMode = true;
            }
            else
            {
                Title = "GOAT BIM | D VIEW — Parametric Door 4-View Shop Drawing & Sheet Engine";
                HeaderTitle.Text = "GOAT D VIEW — Parametric Door 4-View Shop Drawing & Sheet Engine";
                GrpTypesHeader.Header = "1 · Door types";
                try
                {
                    HeaderLogo.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("pack://application:,,,/ORTools;component/Resources/DOOR_VIEW.jpg", UriKind.Absolute));
                }
                catch { }
                _viewModel.IsDoorMode = true;
            }

            Loaded += (s, e) =>
            {
                _viewModel.TriggerAction(OpeningActionType.HarvestOpeningTypes);
            };
        }
    }
}
