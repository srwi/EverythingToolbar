using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EverythingToolbar.Launcher
{
    public partial class TaskbarPinGuide : Window
    {
        private FileSystemWatcher watcher;
        private string TaskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private bool IconHasChanged;

        public TaskbarPinGuide()
        {
            InitializeComponent();

            AutostartCheckBox.IsChecked = Utils.GetAutostartState();
            HideWindowsSearchCheckBox.IsChecked = !Utils.GetWindowsSearchEnabledState();
            UpdateSetupStepVisibility();
            CreateFileWatcher();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (RadioButton radio in IconRadioButtons.Children)
            {
                if ((string)radio.Tag == EverythingToolbar.Properties.Settings.Default.iconName)
                    radio.IsChecked = true;
            }

            IconHasChanged = false;
        }

        private void CreateFileWatcher()
        {
            watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(TaskbarShortcutPath),
                Filter = Path.GetFileName(TaskbarShortcutPath),
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += new FileSystemEventHandler((source, e) => {
                IconHasChanged = true;
                UpdateSetupStepVisibility();
            });
            watcher.Deleted += new FileSystemEventHandler((source, e) => {
                UpdateSetupStepVisibility();
            });
        }

        private void UpdateSetupStepVisibility()
        {
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(EverythingToolbar.Properties.Settings.Default.iconName))
                {
                    SectionTwo.Opacity = 1.0;
                    SectionTwo.IsHitTestVisible = true;
                }
                else
                {
                    SectionTwo.Opacity = 0.2;
                    SectionTwo.IsHitTestVisible = false;
                }

                if (File.Exists(TaskbarShortcutPath))
                {
                    SectionThree.Opacity = 1.0;
                    SectionThree.IsHitTestVisible = true;
                }
                else
                {
                    SectionThree.Opacity = 0.2;
                    SectionThree.IsHitTestVisible = false;
                }
            });
        }

        private void HideWindowsSearchChanged(object sender, RoutedEventArgs e)
        {
            Utils.SetWindowsSearchEnabledState(!(bool)HideWindowsSearchCheckBox.IsChecked);
        }

        private void AutostartChanged(object sender, RoutedEventArgs e)
        {
            Utils.SetAutostartState((bool)AutostartCheckBox.IsChecked);
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            if (!IconHasChanged)
                return;

            if (MessageBox.Show(Properties.Resources.SetupAssistantRestartExplorerDialogText,
                Properties.Resources.SetupAssistantRestartExplorerDialogTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Utils.ChangeTaskbarPinIcon(EverythingToolbar.Properties.Settings.Default.iconName);
            }
        }

        private void OnIconRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            EverythingToolbar.Properties.Settings.Default.iconName = (sender as RadioButton).Tag as string;
            Icon = new BitmapImage(new Uri("pack://application:,,,/" + EverythingToolbar.Properties.Settings.Default.iconName));
            IconHasChanged = true;
            UpdateSetupStepVisibility();
        }
    }
}
