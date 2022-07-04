using System;
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
            CreateFileWatcher();
            if (File.Exists(TaskbarShortcutPath))
                OnTaskbarPinCreated();
            Uri iconUri = new Uri("pack://application:,,,/Icons/" + Utils.GetIconTypeBasedOnWindowsTheme().ToString() + ".ico", UriKind.RelativeOrAbsolute);
            Icon = BitmapFrame.Create(iconUri);
        }

        private void CreateFileWatcher()
        {
            if (File.Exists(TaskbarShortcutPath))
                return;

            watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(TaskbarShortcutPath),
                Filter = Path.GetFileName(TaskbarShortcutPath),
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += new FileSystemEventHandler((source, e) => {
                OnTaskbarPinCreated();
                watcher.EnableRaisingEvents = false;
                Utils.ChangeTaskbarPinIcon(Utils.GetIconTypeBasedOnWindowsTheme());
                watcher.EnableRaisingEvents = true;
            });
        }

        private void OnTaskbarPinCreated()
        {
            Dispatcher.Invoke(() =>
            {
                OptionalSettingsBlock.Opacity = 1.0;
                OptionalSettingsBlock.IsHitTestVisible = true;
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
    }
}
