using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EverythingToolbar.Launcher
{
    public partial class TaskbarPinGuide : Window
    {
        private FileSystemWatcher watcher;
        private string TaskbarShortcutPath = Utils.GetTaskbarShortcutPath();

        public TaskbarPinGuide()
        {
            InitializeComponent();

            AutostartCheckBox.IsChecked = Utils.GetAutostartState();
            HideWindowsSearchCheckBox.IsChecked = !Utils.GetWindowsSearchEnabledState();
            OnTaskbarPinStateChanged(File.Exists(TaskbarShortcutPath));
            CreateFileWatcher();
            Uri iconUri = new Uri("pack://application:,,,/Icons/" + Utils.GetWindowsTheme().ToString() + ".ico", UriKind.RelativeOrAbsolute);
            Icon = BitmapFrame.Create(iconUri);
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
                OnTaskbarPinStateChanged(true);
            });
            watcher.Deleted += new FileSystemEventHandler((source, e) => {
                OnTaskbarPinStateChanged(false);
            });
        }

        private void OnTaskbarPinStateChanged(bool state)
        {
            Dispatcher.Invoke(() =>
            {
                if (state)
                {
                    OptionalSettingsBlock.Opacity = 1.0;
                    OptionalSettingsBlock.IsHitTestVisible = true;
                }
                else
                {
                    OptionalSettingsBlock.Opacity = 0.2;
                    OptionalSettingsBlock.IsHitTestVisible = false;
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

            if (Utils.GetWindowsTheme() == Utils.WindowsTheme.Dark)
                return;

            if (MessageBox.Show("An explorer restart is required to update the taskbar icon. Would you like to restart it now?",
                "Restart explorer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Utils.ChangeTaskbarPinIcon(Utils.GetWindowsTheme());
            }
        }
    }
}
