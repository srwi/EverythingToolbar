using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Controls;

namespace EverythingToolbar.Settings
{
    public partial class CustomActions
    {
        private readonly CustomActionService _service = Ioc.Default.GetRequiredService<CustomActionService>();
        private ObservableCollection<Rule> _actions = new();

        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();

        public CustomActions()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _actions = new ObservableCollection<Rule>(_service.Load());
            DataGrid.ItemsSource = _actions;
            AutoApplyCustomActionsCheckbox.IsChecked = Settings.IsAutoApplyCustomActions;
            UpdateUi();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var autoApply = AutoApplyCustomActionsCheckbox.IsChecked == true;
            if (TrySave(autoApply))
            {
                Settings.IsAutoApplyCustomActions = autoApply;
            }
        }

        private bool TrySave(bool isAutoApplyCustomActions)
        {
            if (_actions.Any(r => string.IsNullOrEmpty(r.Name)))
            {
                FluentMessageBox
                    .CreateError(
                        Properties.Resources.MessageBoxCustomActionsNameEmpty,
                        Properties.Resources.MessageBoxErrorTitle
                    )
                    .ShowDialogAsync();
                return false;
            }
            if (isAutoApplyCustomActions && _actions.Any(r => !r.ExpressionValid))
            {
                FluentMessageBox
                    .CreateError(Properties.Resources.MessageBoxRegExInvalid, Properties.Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
                return false;
            }

            _service.Save(_actions.ToList());
            return true;
        }

        private void AddItem(object sender, RoutedEventArgs e)
        {
            _actions.Add(
                new Rule
                {
                    Name = "",
                    Type = FileType.Any,
                    Expression = "",
                    Command = "",
                }
            );
            DataGrid.SelectedIndex = _actions.Count - 1;
        }

        private void DeleteSelected(object sender, RoutedEventArgs e)
        {
            var selectedIndex = DataGrid.SelectedIndex;
            _actions.RemoveAt(selectedIndex);

            DataGrid.SelectedIndex = Math.Min(selectedIndex, _actions.Count - 1);
        }

        private void MoveDownSelected(object sender, RoutedEventArgs e)
        {
            MoveItem(1);
        }

        private void MoveUpSelected(object sender, RoutedEventArgs e)
        {
            MoveItem(-1);
        }

        private void MoveItem(int delta)
        {
            if (DataGrid.SelectedItem is not Rule)
                return;

            var selectedIndex = DataGrid.SelectedIndex;
            _actions.Move(selectedIndex, selectedIndex + delta);

            DataGrid.SelectedIndex = selectedIndex + delta;
            UpdateUi();
        }

        private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUi();
        }

        private void UpdateUi()
        {
            DeleteButton.IsEnabled = DataGrid.SelectedIndex >= 0;
            MoveDownButton.IsEnabled = DataGrid.SelectedIndex + 1 < _actions.Count && DataGrid.SelectedIndex >= 0;
            MoveUpButton.IsEnabled = DataGrid.SelectedIndex > 0;

            var typeColumn = DataGrid.Columns.FirstOrDefault(c =>
                c.Header.ToString() == Properties.Resources.CustomActionsType
            );
            if (typeColumn is null)
                return;

            if (AutoApplyCustomActionsCheckbox.IsChecked == true)
            {
                typeColumn.Visibility = Visibility.Visible;
                ExpressionColumn.Visibility = Visibility.Visible;
            }
            else
            {
                typeColumn.Visibility = Visibility.Collapsed;
                ExpressionColumn.Visibility = Visibility.Collapsed;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateUi();
        }
    }
}
