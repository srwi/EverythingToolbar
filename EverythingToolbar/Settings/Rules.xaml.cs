using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;

namespace EverythingToolbar.Settings
{
    public partial class Rules
    {
        private static List<Rule> _rules = new List<Rule>();
        private static string RulesPath => Path.Combine(Utils.GetConfigDirectory(), "rules.xml");

        public Rules()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _rules = LoadRules();
            DataGrid.ItemsSource = _rules;
            AutoApplyRulesCheckbox.IsChecked = ToolbarSettings.User.IsAutoApplyRules;
            UpdateUi();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (SaveRules(_rules, (bool)AutoApplyRulesCheckbox.IsChecked))
            {
                ToolbarSettings.User.IsAutoApplyRules = (bool)AutoApplyRulesCheckbox.IsChecked;
            }
        }

        public static List<Rule> LoadRules()
        {
            if (File.Exists(RulesPath))
            {
                var serializer = new XmlSerializer(_rules.GetType());
                using (var reader = XmlReader.Create(RulesPath))
                {
                    return (List<Rule>)serializer.Deserialize(reader);
                }
            }

            return new List<Rule>();
        }

        public static bool SaveRules(List<Rule> newRules, bool isAutoApplyRules)
        {
            if (newRules.Any(r => string.IsNullOrEmpty(r.Name)))
            {
                MessageBox.Show(Properties.Resources.MessageBoxRuleNameEmpty,
                                Properties.Resources.MessageBoxErrorTitle,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }
            if (isAutoApplyRules && newRules.Any(r => !r.ExpressionValid))
            {
                MessageBox.Show(Properties.Resources.MessageBoxRegExInvalid,
                                Properties.Resources.MessageBoxErrorTitle,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RulesPath));
            var serializer = new XmlSerializer(newRules.GetType());
            using (var writer = XmlWriter.Create(RulesPath))
            {
                serializer.Serialize(writer, newRules);
            }

            return true;
        }

        private void AddItem(object sender, RoutedEventArgs e)
        {
            _rules.Insert(_rules.Count, new Rule { Name = "", Type = FileType.Any, Expression = "", Command = "" });
            RefreshList();
            DataGrid.SelectedIndex = _rules.Count - 1;
        }

        private void DeleteSelected(object sender, RoutedEventArgs e)
        {
            var selectedIndex = DataGrid.SelectedIndex;
            _rules.RemoveAt(selectedIndex);
            RefreshList();
            if (_rules.Count > selectedIndex)
            {
                DataGrid.SelectedIndex = selectedIndex;
            }
            else if (_rules.Count > 0)
            {
                DataGrid.SelectedIndex = _rules.Count - 1;
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
            var selectedIndex = DataGrid.SelectedIndex;
            var item = DataGrid.SelectedItem as Rule;
            _rules.RemoveAt(selectedIndex);
            _rules.Insert(selectedIndex + delta, item);
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
            DataGrid.ItemsSource = _rules;
        }

        private void UpdateUi()
        {
            DeleteButton.IsEnabled = DataGrid.SelectedIndex >= 0;
            MoveDownButton.IsEnabled = DataGrid.SelectedIndex + 1 < _rules.Count && DataGrid.SelectedIndex >= 0;
            MoveUpButton.IsEnabled = DataGrid.SelectedIndex > 0;

            var typeColumn = DataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == Properties.Resources.RulesType);
            if (typeColumn is null)
                return;

            if ((bool)AutoApplyRulesCheckbox.IsChecked)
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

        public static bool HandleRule(SearchResult searchResult, string command = "")
        {
            if (searchResult == null)
                return false;

            if (ToolbarSettings.User.IsAutoApplyRules && string.IsNullOrEmpty(command))
            {
                foreach (var r in LoadRules())
                {
                    var regexCond = !string.IsNullOrEmpty(r.Expression) && Regex.IsMatch(searchResult.FullPathAndFileName, r.Expression);
                    var typeCond = searchResult.IsFile && r.Type != FileType.Folder || !searchResult.IsFile && r.Type != FileType.File;
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
                    MessageBox.Show(Properties.Resources.MessageBoxFailedToRunCommand + " " + command);
                }
            }

            return false;
        }
    }
}