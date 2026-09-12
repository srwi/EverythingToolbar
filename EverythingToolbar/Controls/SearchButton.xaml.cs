using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.ViewModels;

namespace EverythingToolbar.Controls
{
    public partial class SearchButton
    {
        private readonly SearchButtonViewModel _viewModel = Ioc.Default.GetRequiredService<SearchButtonViewModel>();

        public SearchButton()
        {
            InitializeComponent();
            DataContext = _viewModel;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.SetIconMode(IsVisible);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Cleanup();
        }

        private void OnClick(object? sender, RoutedEventArgs e)
        {
            _viewModel.Toggle();
        }

        private void OnIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel.SetIconMode((bool)e.NewValue);
        }
    }
}
