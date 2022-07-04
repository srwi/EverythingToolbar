using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace EverythingToolbar.Launcher
{
    public partial class TaskbarPinGuide : Window
    {
        private FileSystemWatcher watcher;
        private string TaskbarPinsPath;

        public TaskbarPinGuide()
        {
            InitializeComponent();
            HideWindowsSearchCheckBox.IsChecked = !GetWindowsSearchEnabledState();
            TaskbarPinsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                "\\Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar\\EverythingToolbar.lnk";
            CreateFileWatcher();
            if (File.Exists(TaskbarPinsPath))
                OnTaskbarPinCreated();

        }

        private void CreateFileWatcher()
        {
            if (File.Exists(TaskbarPinsPath))
                return;

            watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(TaskbarPinsPath),
                Filter = Path.GetFileName(TaskbarPinsPath),
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            watcher.Created += new FileSystemEventHandler((source, e) => { OnTaskbarPinCreated(); });
        }

        private void OnTaskbarPinCreated()
        {
            Dispatcher.Invoke(() =>
            {
                PinToTaskbarBlock.Opacity = 0.3;

                HideWindowsSearchBlock.Opacity = 1.0;
                HideWindowsSearchBlock.IsHitTestVisible = true;
            });
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SetWindowsSearchEnabledState(bool enabled)
        {
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", enabled ? 1 : 0);
        }

        private bool GetWindowsSearchEnabledState()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
            {
                object registryValueObject = key?.GetValue("SearchboxTaskbarMode");
                return registryValueObject != null && (int)registryValueObject > 0;
            }
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            SetWindowsSearchEnabledState(!(bool)HideWindowsSearchCheckBox.IsChecked);
        }
    }
}
