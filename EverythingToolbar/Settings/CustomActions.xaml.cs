using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Controls;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Settings
{
    public partial class CustomActions
    {
        private static List<Rule> _actions = new();
        private static string CustomActionsPath => Path.Combine(ConfigPaths.GetConfigDirectory(), "rules.xml");

        public CustomActionsOptions CustomActionsOptions { get; } = Ioc.Default.GetRequiredService<CustomActionsOptions>();

        public CustomActions()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _actions = LoadCustomActions();
            DataGrid.ItemsSource = _actions;
            UpdateUi();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var autoApply = AutoApplyCustomActionsCheckbox.IsChecked == true;
            if (SaveCustomActions(_actions, autoApply))
            {
                CustomActionsOptions.IsAutoApplyCustomActions = autoApply;
            }
        }

        public static List<Rule> LoadCustomActions()
        {
            if (!File.Exists(CustomActionsPath))
                return [];

            XmlSerializer serializer = new(_actions.GetType());
            using XmlReader reader = XmlReader.Create(CustomActionsPath);
            try
            {
                return serializer.Deserialize(reader) as List<Rule> ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static bool SaveCustomActions(List<Rule> newActions, bool isAutoApplyCustomActions)
        {
            if (newActions.Any(r => string.IsNullOrEmpty(r.Name)))
            {
                FluentMessageBox
                    .CreateError(
                        Properties.Resources.MessageBoxCustomActionsNameEmpty,
                        Properties.Resources.MessageBoxErrorTitle
                    )
                    .ShowDialogAsync();
                return false;
            }
            if (isAutoApplyCustomActions && newActions.Any(r => !r.ExpressionValid))
            {
                FluentMessageBox
                    .CreateError(Properties.Resources.MessageBoxRegExInvalid, Properties.Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
                return false;
            }

            if (Path.GetDirectoryName(CustomActionsPath) is { } parent)
                Directory.CreateDirectory(parent);

            var serializer = new XmlSerializer(newActions.GetType());
            using var writer = XmlWriter.Create(CustomActionsPath);
            serializer.Serialize(writer, newActions);

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

        public static bool HandleAction(SearchResult? searchResult, string command = "")
        {
            if (searchResult == null)
                return false;

            if (Ioc.Default.GetRequiredService<CustomActionsOptions>().IsAutoApplyCustomActions && string.IsNullOrEmpty(command))
            {
                foreach (var r in LoadCustomActions())
                {
                    var regexCond =
                        !string.IsNullOrEmpty(r.Expression)
                        && Regex.IsMatch(searchResult.FullPathAndFileName, r.Expression);
                    var typeCond =
                        searchResult.IsFile && r.Type != FileType.Folder
                        || !searchResult.IsFile && r.Type != FileType.File;
                    if (regexCond && typeCond)
                    {
                        command = r.Command;
                    }
                }
            }

            if (!string.IsNullOrEmpty(command))
            {
                command = command.Replace("%file%", "\"" + searchResult.FullPathAndFileName + "\"");
                command = command.Replace("%filename%", "\"" + searchResult.FileName + "\"");
                command = command.Replace("%path%", "\"" + searchResult.Path + "\"");
                try
                {
                    ShellUtils.CreateProcessFromCommandLine(command, searchResult.Path);
                    return true;
                }
                catch (Win32Exception)
                {
                    FluentMessageBox
                        .CreateError(
                            Properties.Resources.MessageBoxFailedToRunCommand + " " + command,
                            Properties.Resources.MessageBoxErrorTitle
                        )
                        .ShowDialogAsync();
                }
            }

            return false;
        }
    }
}
