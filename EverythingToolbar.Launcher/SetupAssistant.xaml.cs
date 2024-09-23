using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using EverythingToolbar.Helpers;
using NLog;
using FlowDirection = System.Windows.FlowDirection;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace EverythingToolbar.Launcher
{
    public partial class SetupAssistant
    {
        private readonly string _taskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private readonly NotifyIcon _icon;
        private const int TotalPages = 3;
        private int _unlockedPages = 1;
        private bool _iconHasChanged;
        private FileSystemWatcher _watcher;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SetupAssistant>();

        public SetupAssistant(NotifyIcon icon)
        {
            InitializeComponent();

            _icon = icon;

            AutostartCheckBox.IsChecked = Utils.GetAutostartState();
            HideWindowsSearchCheckBox.IsChecked = !Utils.GetWindowsSearchEnabledState();
            TrayIconCheckBox.IsChecked = ToolbarSettings.User.IsTrayIconEnabled;

            CreateFileWatcher(_taskbarShortcutPath);
            
            if (File.Exists(_taskbarShortcutPath))
            {
                _unlockedPages = Math.Max(3, _unlockedPages);
                Dispatcher.Invoke(() => { SelectPage(2); });
            }

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _icon.Visible = false;

            UpdatePaginationToFlowDirection();

            foreach (RadioButton radio in IconRadioButtons.Children)
            {
                if ((string)radio.Tag == ToolbarSettings.User.IconName)
                    radio.IsChecked = true;
            }

            _iconHasChanged = false;
            
            // Bring to front
            Topmost = true;
            Topmost = false;
        }

        private void CreateFileWatcher(string taskbarShortcutPath)
        {
            var pinnedIconsDir = Path.GetDirectoryName(taskbarShortcutPath);
            var pinnedIconName = Path.GetFileName(taskbarShortcutPath);

            try
            {
                // The directory might not exist on some systems (#523)
                Directory.CreateDirectory(pinnedIconsDir);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Error(e, "Failed to create pinned taskbar icons directory.");
                return;
            }

            _watcher = new FileSystemWatcher
            {
                Path = pinnedIconsDir,
                Filter = pinnedIconName,
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Created += (source, e) =>
            {
                _iconHasChanged = true;
                _unlockedPages = Math.Max(3, _unlockedPages);
                Dispatcher.Invoke(() => { SelectPage(2); });
            };
            _watcher.Deleted += (source, e) =>
            {
                _unlockedPages = Math.Min(2, _unlockedPages);
                Dispatcher.Invoke(() => { SelectPage(1); });
            };
        }

        private void HideWindowsSearchChanged(object sender, RoutedEventArgs e)
        {
            Utils.SetWindowsSearchEnabledState(HideWindowsSearchCheckBox.IsChecked != null &&
                                               !(bool)HideWindowsSearchCheckBox.IsChecked);
        }

        private void AutostartChanged(object sender, RoutedEventArgs e)
        {
            Utils.SetAutostartState(AutostartCheckBox.IsChecked != null && (bool)AutostartCheckBox.IsChecked);
        }

        private void TrayIconChanged(object sender, RoutedEventArgs e)
        {
            ToolbarSettings.User.IsTrayIconEnabled = TrayIconCheckBox.IsChecked != null && (bool)TrayIconCheckBox.IsChecked;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_unlockedPages == TotalPages)
                return;

            var disableSetupAssistant = MessageBox.Show(
                Properties.Resources.SetupAssistantDisableWarningText,
                Properties.Resources.SetupAssistantDisableWarningTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation
            ) == MessageBoxResult.Yes;
            if (disableSetupAssistant)
            {
                ToolbarSettings.User.IsSetupAssistantDisabled = disableSetupAssistant;
                // Ensuring the user can access the setup assistant
                ToolbarSettings.User.IsTrayIconEnabled = disableSetupAssistant;
            }
            e.Cancel = !disableSetupAssistant;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _icon.Visible = ToolbarSettings.User.IsTrayIconEnabled;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            if (!_iconHasChanged)
                return;

            if (MessageBox.Show(Properties.Resources.SetupAssistantRestartExplorerDialogText,
                    Properties.Resources.SetupAssistantRestartExplorerDialogTitle, MessageBoxButton.YesNo) ==
                MessageBoxResult.Yes)
            {
                Utils.ChangeTaskbarPinIcon(ToolbarSettings.User.IconName);
            }
        }

        private void OnIconRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ToolbarSettings.User.IconName = ((RadioButton)sender).Tag as string;
            Icon = new BitmapImage(new Uri("pack://application:,,,/" + ToolbarSettings.User.IconName));
            _iconHasChanged = true;

            _unlockedPages = Math.Max(2, _unlockedPages);
            UpdatePagination();
        }

        private void SelectPage(int page)
        {
            PaginationTabControl.SelectedIndex = page;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            PreviousButton.IsEnabled = PaginationTabControl.SelectedIndex > 0;
            NextButton.IsEnabled = PaginationTabControl.SelectedIndex < _unlockedPages - 1;
            PaginationLabel.Text = $"{PaginationTabControl.SelectedIndex + 1} / {TotalPages}";
        }

        private void UpdatePaginationToFlowDirection()
        {
            if (FlowDirection == FlowDirection.RightToLeft)
            {
                (NextButton.Content, PreviousButton.Content) = (PreviousButton.Content, NextButton.Content);
            }
        }

        private void OnNextPageClicked(object sender, RoutedEventArgs e)
        {
            var nextPage = Math.Min(PaginationTabControl.SelectedIndex + 1, _unlockedPages - 1);
            SelectPage(nextPage);
        }

        private void OnPreviousPageClicked(object sender, RoutedEventArgs e)
        {
            var previousPage = Math.Max(PaginationTabControl.SelectedIndex - 1, 0);
            SelectPage(previousPage);
        }
    }
}