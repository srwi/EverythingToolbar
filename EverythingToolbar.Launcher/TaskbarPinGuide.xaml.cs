using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using IWshRuntimeLibrary;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace EverythingToolbar.Launcher
{
    public partial class TaskbarPinGuide : Window
    {
        private FileSystemWatcher watcher;
        private string TaskbarPinPath;

        private enum IconType
        {
            Dark,
            Light
        }

        public TaskbarPinGuide()
        {
            InitializeComponent();
            AutostartCheckBox.IsChecked = GetAutostartState();
            HideWindowsSearchCheckBox.IsChecked = !GetWindowsSearchEnabledState();
            TaskbarPinPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                "\\Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar\\EverythingToolbar.lnk";
            CreateFileWatcher();
            if (System.IO.File.Exists(TaskbarPinPath))
                OnTaskbarPinCreated();
        }

        private void CreateFileWatcher()
        {
            if (System.IO.File.Exists(TaskbarPinPath))
                return;

            watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(TaskbarPinPath),
                Filter = Path.GetFileName(TaskbarPinPath),
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += new FileSystemEventHandler((source, e) => {
                OnTaskbarPinCreated();
                ChangeTaskbarPinIcon(GetIconTypeBasedOnWindowsTheme());
            });
        }

        private IconType GetIconTypeBasedOnWindowsTheme()
        {
            bool systemUsesLightTheme;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object registryValueObject = key?.GetValue("SystemUsesLightTheme");
                systemUsesLightTheme = registryValueObject != null && (int)registryValueObject > 0;
            }

            return systemUsesLightTheme ? IconType.Light : IconType.Dark;
        }

        private string GetIconPath(IconType iconType)
        {
            string processPath = Process.GetCurrentProcess().MainModule.FileName;
            return Directory.GetParent(processPath).FullName + "\\Icons\\" + iconType.ToString() + ".ico";
        }

        private void OnTaskbarPinCreated()
        {
            Dispatcher.Invoke(() =>
            {
                OptionalSettingsBlock.Opacity = 1.0;
                OptionalSettingsBlock.IsHitTestVisible = true;
            });
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool GetWindowsSearchEnabledState()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
            {
                object registryValueObject = key?.GetValue("SearchboxTaskbarMode");
                return registryValueObject != null && (int)registryValueObject > 0;
            }
        }

        private bool GetAutostartState()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                object registryValueObject = key?.GetValue("EverythingToolbar");
                return registryValueObject != null;
            }
        }

        private void HideWindowsSearchChanged(object sender, RoutedEventArgs e)
        {
            bool enabled = !(bool)HideWindowsSearchCheckBox.IsChecked;
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", enabled ? 1 : 0);
        }

        private void AutostartChanged(object sender, RoutedEventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if ((bool)AutostartCheckBox.IsChecked)
                key.SetValue("EverythingToolbar", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
            else
                key.DeleteValue("EverythingToolbar", false);
        }

        //private void PromptExplorerRestart()
        //{
        //    if (MessageBox.Show("To show the correct icon a Explorer restart is necessary. Do you want to restart it now?", "Restart Explorer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        //    {
        //        foreach (var process in Process.GetProcessesByName("explorer"))
        //        {
        //            process.Kill();
        //        }
        //    }
        //}

        private void ChangeTaskbarPinIcon(IconType iconType)
        {
            bool watcherState = watcher.EnableRaisingEvents;
            watcher.EnableRaisingEvents = false;

            const int maxTries = 1000;
            for (int i = 0; i < maxTries; i++)
            {
                try
                {
                    if (System.IO.File.Exists(TaskbarPinPath))
                        System.IO.File.Delete(TaskbarPinPath);

                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(1);
                }
            }

            if (System.IO.File.Exists(TaskbarPinPath))
                throw new IOException("Could not access shortcut.");

            string targetPath = Process.GetCurrentProcess().MainModule.FileName;
            string iconPath = GetIconPath(iconType);

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(TaskbarPinPath);
            shortcut.TargetPath = targetPath;
            shortcut.IconLocation = iconPath;
            shortcut.Save();

            watcher.EnableRaisingEvents = watcherState;
        }
    }
}
