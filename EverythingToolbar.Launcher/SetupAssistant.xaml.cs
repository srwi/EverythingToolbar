using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using EverythingToolbar.Properties;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace EverythingToolbar.Launcher
{
    public partial class SetupAssistant
    {
        private readonly string _taskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private readonly NotifyIcon _icon;
        private bool _iconHasChanged;
        private FileSystemWatcher _watcher;

        public SetupAssistant(NotifyIcon icon)
        {
            InitializeComponent();

            _icon = icon;

            AutostartCheckBox.IsChecked = Utils.GetAutostartState();
            HideWindowsSearchCheckBox.IsChecked = !Utils.GetWindowsSearchEnabledState();
            UpdateSetupStepVisibility();
            CreateFileWatcher();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _icon.Visible = false;

            foreach (RadioButton radio in IconRadioButtons.Children)
            {
                if ((string)radio.Tag == Settings.Default.iconName)
                    radio.IsChecked = true;
            }

            _iconHasChanged = false;
        }

        private void CreateFileWatcher()
        {
            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_taskbarShortcutPath),
                Filter = Path.GetFileName(_taskbarShortcutPath),
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Created += (source, e) => {
                _iconHasChanged = true;
                UpdateSetupStepVisibility();
            };
            _watcher.Deleted += (source, e) => {
                UpdateSetupStepVisibility();
            };
        }

        private void UpdateSetupStepVisibility()
        {
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(Settings.Default.iconName))
                {
                    SectionTwo.Opacity = 1.0;
                    SectionTwo.IsHitTestVisible = true;
                }
                else
                {
                    SectionTwo.Opacity = 0.2;
                    SectionTwo.IsHitTestVisible = false;
                }

                if (File.Exists(_taskbarShortcutPath))
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
            Utils.SetWindowsSearchEnabledState(HideWindowsSearchCheckBox.IsChecked != null && !(bool)HideWindowsSearchCheckBox.IsChecked);
        }

        private void AutostartChanged(object sender, RoutedEventArgs e)
        {
            Utils.SetAutostartState(AutostartCheckBox.IsChecked != null && (bool)AutostartCheckBox.IsChecked);
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _icon.Visible = true;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            if (!_iconHasChanged)
                return;

            if (MessageBox.Show(Properties.Resources.SetupAssistantRestartExplorerDialogText,
                Properties.Resources.SetupAssistantRestartExplorerDialogTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Utils.ChangeTaskbarPinIcon(Settings.Default.iconName);
            }
        }

        private void OnIconRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.iconName = ((RadioButton)sender).Tag as string;
            Icon = new BitmapImage(new Uri("pack://application:,,,/" + Settings.Default.iconName));
            _iconHasChanged = true;
            UpdateSetupStepVisibility();
        }
    }
}
