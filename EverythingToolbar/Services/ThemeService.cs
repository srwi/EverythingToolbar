using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using NLog;
using Windows.UI.ViewManagement;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;
using Color = Windows.UI.Color;

namespace EverythingToolbar.Services
{
    public enum Theme
    {
        Dark,
        Light,
    }

    public enum ThemeFlavor
    {
        App,
        System,
    }

    public enum ThemedSurface
    {
        AppWindow,

        TaskbarSurface,
    }

    public sealed class ThemeChangedEventArgs : EventArgs
    {
        public Theme SystemTheme { get; init; }
        public Theme AppTheme { get; init; }
    }

    public sealed class ThemeService : IDisposable
    {
        private const string PersonalizeSubKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        private static readonly ILogger Logger = ToolbarLogger.GetLogger<ThemeService>();

        private static readonly RegistryEntry SystemThemeRegistryEntry = new(
            "HKEY_CURRENT_USER",
            PersonalizeSubKey,
            "SystemUsesLightTheme"
        );

        private static readonly RegistryEntry AppsThemeRegistryEntry = new(
            "HKEY_CURRENT_USER",
            PersonalizeSubKey,
            "AppsUseLightTheme"
        );

        private sealed class Registration
        {
            public required WeakReference<FrameworkElement> Root { get; init; }
            public ThemedSurface Surface { get; init; }
            public List<ResourceDictionary> AddedDictionaries { get; } = new();
        }

        private readonly ISettings _settings;
        private readonly WindowsPolicy _windowsPolicy;
        private readonly UISettings? _uiSettings;
        private readonly Dispatcher _dispatcher;
        private readonly List<Registration> _registrations = new();
        private int _applyScheduled;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public ThemeService(ISettings settings, WindowsPolicy windowsPolicy)
        {
            _settings = settings;
            _windowsPolicy = windowsPolicy;
            _dispatcher = Dispatcher.CurrentDispatcher;

            try
            {
                _uiSettings = new UISettings();
                _uiSettings.ColorValuesChanged += OnColorValuesChanged;
            }
            catch
            {
                Logger.Info("Could not apply accent color automatically.");
            }

            _settings.PropertyChanged += OnSettingsChanged;
        }

        public Theme GetEffectiveTheme(ThemeFlavor flavor)
        {
            switch (_settings.ThemeOverride.ToLowerInvariant())
            {
                case "light":
                    return Theme.Light;
                case "dark":
                    return Theme.Dark;
            }

            var entry = flavor == ThemeFlavor.System ? SystemThemeRegistryEntry : AppsThemeRegistryEntry;
            return (int)(entry.GetValue(0) ?? 0) == 1 ? Theme.Light : Theme.Dark;
        }

        public bool IsLightTheme() => GetEffectiveTheme(ThemeFlavor.System) == Theme.Light;

        public void Register(FrameworkElement root, ThemedSurface surface)
        {
            RemoveRegistration(root);
            var registration = new Registration { Root = new WeakReference<FrameworkElement>(root), Surface = surface };
            _registrations.Add(registration);

            var systemTheme = GetEffectiveTheme(ThemeFlavor.System);
            var appTheme = GetEffectiveTheme(ThemeFlavor.App);

            if (surface == ThemedSurface.AppWindow)
                ApplyGlobalWpfUiTheme(appTheme);

            ApplyTo(registration, systemTheme);
        }

        public void Unregister(FrameworkElement root)
        {
            RemoveRegistration(root);
        }

        private void RemoveRegistration(FrameworkElement root)
        {
            for (var i = _registrations.Count - 1; i >= 0; i--)
            {
                if (!_registrations[i].Root.TryGetTarget(out var target) || ReferenceEquals(target, root))
                    _registrations.RemoveAt(i);
            }
        }

        private void OnColorValuesChanged(UISettings sender, object args) => ScheduleApply();

        // Coalesces bursts from watcher/UISettings background threads into one apply pass on the UI thread.
        private void ScheduleApply()
        {
            if (Interlocked.Exchange(ref _applyScheduled, 1) == 1)
                return;

            _dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                () =>
                {
                    Interlocked.Exchange(ref _applyScheduled, 0);
                    ApplyAll();
                }
            );
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ISettings.ThemeOverride) or nameof(ISettings.ForceWin10Behavior))
            {
                ScheduleApply();
            }
        }

        private void ApplyAll()
        {
            var systemTheme = GetEffectiveTheme(ThemeFlavor.System);
            var appTheme = GetEffectiveTheme(ThemeFlavor.App);
            Logger.Debug("Applying theme (system: {system}, app: {app})", systemTheme, appTheme);

            _registrations.RemoveAll(r => !r.Root.TryGetTarget(out _));

            if (_registrations.Any(r => r.Surface == ThemedSurface.AppWindow))
                ApplyGlobalWpfUiTheme(appTheme);

            foreach (var registration in _registrations)
            {
                ApplyTo(registration, systemTheme);
            }

            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { SystemTheme = systemTheme, AppTheme = appTheme });
        }

        private static void ApplyGlobalWpfUiTheme(Theme appTheme) =>
            ApplicationThemeManager.Apply(appTheme == Theme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark);

        private void ApplyTo(Registration registration, Theme systemTheme)
        {
            if (!registration.Root.TryGetTarget(out var root))
                return;

            switch (registration.Surface)
            {
                case ThemedSurface.AppWindow:
                    ApplicationThemeManager.Apply(root);
                    break;
                case ThemedSurface.TaskbarSurface:
                    ApplyCustomLayers(registration, root, systemTheme);
                    break;
            }
        }

        private void ApplyCustomLayers(Registration registration, FrameworkElement root, Theme systemTheme)
        {
            foreach (var dict in registration.AddedDictionaries)
                root.Resources.MergedDictionaries.Remove(dict);
            registration.AddedDictionaries.Clear();

            var profile = _windowsPolicy.GetEffectiveWindowsVersion() >= WindowsVersion.Windows11 ? "Win11" : "Win10";

            AddWpfUiBase(registration, root, systemTheme);

            AddResource(registration, root, $"Themes/{profile}/{(systemTheme == Theme.Light ? "Light" : "Dark")}.xaml");

            AddResource(registration, root, $"Themes/{profile}/Controls.xaml");

            AddAccentColor(registration, root, systemTheme);
        }

        private static void AddWpfUiBase(Registration registration, FrameworkElement root, Theme theme)
        {
            var applicationTheme = theme == Theme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark;

            var controlsDictionary = new ControlsDictionary();
            var themesDictionary = new ThemesDictionary { Theme = applicationTheme };

            root.Resources.MergedDictionaries.Add(controlsDictionary);
            root.Resources.MergedDictionaries.Add(themesDictionary);

            registration.AddedDictionaries.Add(controlsDictionary);
            registration.AddedDictionaries.Add(themesDictionary);
        }

        private static void AddResource(Registration registration, FrameworkElement root, string relativePath)
        {
            var resDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/EverythingToolbar;component/" + relativePath),
            };
            root.Resources.MergedDictionaries.Add(resDict);
            registration.AddedDictionaries.Add(resDict);
        }

        private void AddAccentColor(Registration registration, FrameworkElement root, Theme systemTheme)
        {
            SolidColorBrush brush;
            if (_uiSettings != null)
            {
                var color = _uiSettings.GetColorValue(
                    systemTheme == Theme.Light ? UIColorType.AccentDark1 : UIColorType.AccentLight2
                );
                brush = GetBrush(color);
            }
            else
            {
                brush = Brushes.DimGray;
            }

            var resDict = new ResourceDictionary { ["AccentColor"] = brush };
            root.Resources.MergedDictionaries.Add(resDict);
            registration.AddedDictionaries.Add(resDict);
        }

        private static SolidColorBrush GetBrush(Color color)
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            brush.Freeze();
            return brush;
        }

        public void Dispose()
        {
            _settings.PropertyChanged -= OnSettingsChanged;
            if (_uiSettings != null)
                _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
        }
    }
}
