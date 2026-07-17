using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.ViewModels;

namespace EverythingToolbar.Controls
{
    public partial class SearchResultPreviewPane
    {
        private readonly SearchResultPreviewPaneViewModel _viewModel =
            Ioc.Default.GetRequiredService<SearchResultPreviewPaneViewModel>();

        public sealed class PreviewActionItem
        {
            public string Label { get; init; } = "";
            public string Glyph { get; init; } = "";
            public Action<SearchResult> Action { get; init; } = _ => { };
        }

        public static readonly DependencyProperty SelectedResultProperty = DependencyProperty.Register(
            nameof(SelectedResult),
            typeof(SearchResult),
            typeof(SearchResultPreviewPane),
            new PropertyMetadata(null, OnSelectedResultChanged)
        );

        public static readonly DependencyProperty HasSelectionProperty = DependencyProperty.Register(
            nameof(HasSelection),
            typeof(bool),
            typeof(SearchResultPreviewPane),
            new PropertyMetadata(false)
        );

        public static readonly DependencyProperty ShowPathInfoProperty = DependencyProperty.Register(
            nameof(ShowPathInfo),
            typeof(bool),
            typeof(SearchResultPreviewPane),
            new PropertyMetadata(false)
        );

        public static readonly DependencyProperty ShowSizeInfoProperty = DependencyProperty.Register(
            nameof(ShowSizeInfo),
            typeof(bool),
            typeof(SearchResultPreviewPane),
            new PropertyMetadata(false)
        );

        public static readonly DependencyProperty HasVisibleFileInfoProperty = DependencyProperty.Register(
            nameof(HasVisibleFileInfo),
            typeof(bool),
            typeof(SearchResultPreviewPane),
            new PropertyMetadata(false)
        );

        public SearchResult? SelectedResult
        {
            get => (SearchResult?)GetValue(SelectedResultProperty);
            set => SetValue(SelectedResultProperty, value);
        }

        public bool HasSelection
        {
            get => (bool)GetValue(HasSelectionProperty);
            private set => SetValue(HasSelectionProperty, value);
        }

        public bool ShowPathInfo
        {
            get => (bool)GetValue(ShowPathInfoProperty);
            private set => SetValue(ShowPathInfoProperty, value);
        }

        public bool ShowSizeInfo
        {
            get => (bool)GetValue(ShowSizeInfoProperty);
            private set => SetValue(ShowSizeInfoProperty, value);
        }

        public bool HasVisibleFileInfo
        {
            get => (bool)GetValue(HasVisibleFileInfoProperty);
            private set => SetValue(HasVisibleFileInfoProperty, value);
        }

        public ObservableCollection<PreviewActionItem> PreviewActions { get; } = [];

        public SearchResultPreviewPane()
        {
            InitializeComponent();
            _viewModel.Settings.PropertyChanged += OnToolbarSettingsPropertyChanged;
            Unloaded += OnUnloaded;
        }

        private static void OnSelectedResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchResultPreviewPane pane)
                pane.RefreshActions();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Settings.PropertyChanged -= OnToolbarSettingsPropertyChanged;
            Unloaded -= OnUnloaded;
        }

        private void OnToolbarSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettings.ItemTemplate))
                RefreshActions();
        }

        private void RefreshActions()
        {
            HasSelection = SelectedResult != null;
            PreviewActions.Clear();

            if (SelectedResult == null)
            {
                ShowPathInfo = false;
                ShowSizeInfo = false;
                HasVisibleFileInfo = false;
                return;
            }

            UpdateFileInfoVisibility();

            AddAction(Properties.Resources.ContextMenuOpen, "\uE8A5", _viewModel.Open);
            AddAction(Properties.Resources.ContextMenuOpenPath, "\uE838", _viewModel.OpenPath);
            AddAction(Properties.Resources.ContextMenuOpenWith, "\uE7AC", _viewModel.OpenWith);
            AddAction(Properties.Resources.ContextMenuShowInEverything, "\uF78B", _viewModel.ShowInEverything);
            AddAction(Properties.Resources.ContextMenuProperties, "\uE946", _viewModel.ShowProperties);
        }

        private void UpdateFileInfoVisibility()
        {
            var template = _viewModel.Settings.ItemTemplate ?? "";
            bool isDetailed =
                template.Equals("NormalDetailed", StringComparison.OrdinalIgnoreCase)
                || template.Equals("CompactDetailed", StringComparison.OrdinalIgnoreCase);

            ShowPathInfo = !isDetailed;
            ShowSizeInfo = !isDetailed && SelectedResult?.IsFile == true;
            HasVisibleFileInfo = ShowPathInfo || ShowSizeInfo;
        }

        private void AddAction(string label, string glyph, Action<SearchResult> action)
        {
            PreviewActions.Add(
                new PreviewActionItem
                {
                    Label = label,
                    Glyph = glyph,
                    Action = action,
                }
            );
        }

        private void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (SelectedResult == null || sender is not Button { DataContext: PreviewActionItem item })
                return;

            // The action (a view-model method) hides the search window itself.
            item.Action(SelectedResult);
        }
    }
}
