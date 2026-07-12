using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Controls;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Settings
{
    public partial class CustomActions
    {
        private readonly CustomActionService _service = Ioc.Default.GetRequiredService<CustomActionService>();
        private List<Rule> _actions = new();

        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();

        public CustomActions()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _actions = _service.Load();
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

            _service.Save(_actions);
            return true;
        }

        private void AddItem(object sender, RoutedEventArgs e)
        {
            _actions.Insert(
                _actions.Count,
                new Rule
                {
                    Name = "",
                    Type = FileType.Any,
                    Expression = "",
                    Command = "",
                }
            );
            RefreshList();
            DataGrid.SelectedIndex = _actions.Count - 1;
        }

        private void DeleteSelected(object sender, RoutedEventArgs e)
        {
            var selectedIndex = DataGrid.SelectedIndex;
            _actions.RemoveAt(selectedIndex);
            RefreshList();
            if (_actions.Count > selectedIndex)
            {
                DataGrid.SelectedIndex = selectedIndex;
            }
            else if (_actions.Count > 0)
            {
                DataGrid.SelectedIndex = _actions.Count - 1;
            }
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
            if (DataGrid.SelectedItem is not Rule item)
                return;

            var selectedIndex = DataGrid.SelectedIndex;
            _actions.RemoveAt(selectedIndex);
            _actions.Insert(selectedIndex + delta, item);
            RefreshList();
            DataGrid.SelectedIndex = selectedIndex + delta;
        }

        private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUi();
        }

        private void RefreshList()
        {
            DataGrid.ItemsSource = null;
            DataGrid.ItemsSource = _actions;
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
