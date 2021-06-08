using EverythingToolbar.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace EverythingToolbar
{
    public partial class Rules : Window
    {
        static List<Rule> rules = new List<Rule>();
        static string rulesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EverythingToolbar", "rules.xml");

        public Rules()
        {
            InitializeComponent();

            rules = LoadRules();
            dataGrid.ItemsSource = rules;
            autoApplyRulesCheckbox.IsChecked = Properties.Settings.Default.isAutoApplyRules;
            UpdateUI();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if(SaveRules(rules, (bool)autoApplyRulesCheckbox.IsChecked))
            {
                Properties.Settings.Default.isAutoApplyRules = (bool)autoApplyRulesCheckbox.IsChecked;
                Properties.Settings.Default.Save();
                Close();
            }
        }

        public static List<Rule> LoadRules()
        {
            if (File.Exists(rulesPath))
            {
                var serializer = new XmlSerializer(rules.GetType());
                using (var reader = XmlReader.Create(rulesPath))
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

            Directory.CreateDirectory(Path.GetDirectoryName(rulesPath));
            var serializer = new XmlSerializer(newRules.GetType());
            using (var writer = XmlWriter.Create(rulesPath))
            {
                serializer.Serialize(writer, newRules);
            }

            return true;
        }

        private void AddItem(object sender, RoutedEventArgs e)
        {
            rules.Insert(rules.Count, new Rule() { Name = "", Type = FileType.Any, Expression = "", Command = "" });
            RefreshList();
            dataGrid.SelectedIndex = rules.Count - 1;
        }

        private void DeleteSelected(object sender, RoutedEventArgs e)
        {
            int selectedIndex = dataGrid.SelectedIndex;
            rules.RemoveAt(selectedIndex);
            RefreshList();
            if (rules.Count > selectedIndex)
            {
                dataGrid.SelectedIndex = selectedIndex;
            }
            else if (rules.Count > 0)
            {
                dataGrid.SelectedIndex = rules.Count - 1;
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
            int selectedIndex = dataGrid.SelectedIndex;
            Rule item = dataGrid.SelectedItem as Rule;
            rules.RemoveAt(selectedIndex);
            rules.Insert(selectedIndex + delta, item);
            RefreshList();
            dataGrid.SelectedIndex = selectedIndex + delta;
        }

        private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void RefreshList()
        {
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = rules;
        }

        private void UpdateUI()
        {
            DeleteButton.IsEnabled = dataGrid.SelectedIndex >= 0;
            MoveDownButton.IsEnabled = dataGrid.SelectedIndex + 1 < rules.Count && dataGrid.SelectedIndex >= 0;
            MoveUpButton.IsEnabled = dataGrid.SelectedIndex > 0;

            if ((bool)autoApplyRulesCheckbox.IsChecked)
            {
                TypeColumn.Visibility = Visibility.Visible;
                ExpressionColumn.Visibility = Visibility.Visible;
            }
            else
            {
                TypeColumn.Visibility = Visibility.Collapsed;
                ExpressionColumn.Visibility = Visibility.Collapsed;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }

        public static bool HandleRule(SearchResult searchResult, string command="")
        {
            if (searchResult == null)
                return false;

            if (Properties.Settings.Default.isAutoApplyRules && string.IsNullOrEmpty(command))
            {
                foreach (Rule r in LoadRules())
                {
                    bool regexCond = !string.IsNullOrEmpty(r.Expression) && Regex.IsMatch(searchResult.FullPathAndFileName, r.Expression);
                    bool typeCond = searchResult.IsFile && r.Type != FileType.Folder || !searchResult.IsFile && r.Type != FileType.File;
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
                catch(Win32Exception)
                {
                    MessageBox.Show(Properties.Resources.MessageBoxFailedToRunCommand + " " + command);
                }
            }

            return false;
        }
    }
}
