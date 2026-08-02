using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

namespace EverythingToolbar.Services
{
    public sealed class WindowsPolicy : INotifyPropertyChanged, IDisposable
    {
        private readonly ISettings _settings;

        public event PropertyChangedEventHandler? PropertyChanged;

        public WindowsPolicy(ISettings settings)
        {
            _settings = settings;
            _settings.PropertyChanged += OnSettingsChanged;
            SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        }

        public Version GetEffectiveWindowsVersion()
        {
            if (_settings.ForceWin10Behavior)
                return WindowsVersion.Windows10Anniversary;

            return Environment.OSVersion.Version;
        }

        public Version GetWindowsVersion() => Environment.OSVersion.Version;

        public bool IsEffectiveAnimationsDisabled =>
            _settings.IsAnimationsDisabled || !SystemSettings.GetSystemAnimationsEnabled();

        public bool CanEnableTaskbarWindow() =>
            GetWindowsVersion() >= WindowsVersion.Windows11 && !NativeMethods.IsClassicTaskbar();

        public bool IsTaskbarWindowActive() => _settings.TaskbarWindowEnabled;

        public bool IsTaskbarCenterAligned()
        {
            if (_settings.IsForceCenterAlignment)
                return true;

            if (GetWindowsVersion() < WindowsVersion.Windows11)
                return false;

            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
            );
            var taskbarAlignment = key?.GetValue("TaskbarAl");
            var leftAligned = taskbarAlignment != null && (int)taskbarAlignment == 0;
            return !leftAligned;
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ISettings.IsAnimationsDisabled))
                NotifyAnimationsChanged();
        }

        // Any system parameter change may carry a new SPI_GETCLIENTAREAANIMATION value.
        private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e) => NotifyAnimationsChanged();

        private void NotifyAnimationsChanged() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEffectiveAnimationsDisabled)));

        public void Dispose()
        {
            _settings.PropertyChanged -= OnSettingsChanged;
            SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        }
    }
}
