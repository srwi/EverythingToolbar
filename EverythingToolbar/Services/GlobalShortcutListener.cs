using System;
using System.Windows.Input;
using EverythingToolbar.Controls;
using EverythingToolbar.Properties;
using NHotkey;
using NHotkey.Wpf;
using NLog;

namespace EverythingToolbar.Services
{
    public class GlobalShortcutListener
    {
        private const string HotkeyName = "EverythingToolbarHotkey";
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<GlobalShortcutListener>();

        private readonly ISettings _settings;
        private Action? _handler;

        public GlobalShortcutListener(ISettings settings)
        {
            _settings = settings;
        }

        public bool IsEnabled
        {
            get => HotkeyManager.Current.IsEnabled;
            set => HotkeyManager.Current.IsEnabled = value;
        }

        public void Initialize(Action handler)
        {
            _handler = handler;

            var shortcutKey = (Key)_settings.ShortcutKey;
            var shortcutModifiers = (ModifierKeys)_settings.ShortcutModifiers;

            if (shortcutKey == Key.None && shortcutModifiers == ModifierKeys.None)
                return;

            TrySetShortcut(shortcutKey, shortcutModifiers);
        }

        public void TrySetShortcut(Key key, ModifierKeys modifiers)
        {
            if (key == Key.None && modifiers == ModifierKeys.None)
            {
                HotkeyManager.Current.Remove(HotkeyName);
                UpdateSettings(key, modifiers);
                return;
            }

            try
            {
                HotkeyManager.Current.AddOrReplace(HotkeyName, key, modifiers, (_, _) => _handler?.Invoke());
                UpdateSettings(key, modifiers);
            }
            catch (HotkeyAlreadyRegisteredException e)
            {
                UpdateSettings(Key.None, ModifierKeys.None);

                Logger.Error(e, "Failed to register hotkey {0} with modifiers {1}", key, modifiers);
                FluentMessageBox
                    .CreateError(Resources.MessageBoxFailedToRegisterHotkey, Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
            }
        }

        public void UpdateSettings(Key key, ModifierKeys mods)
        {
            _settings.ShortcutKey = (int)key;
            _settings.ShortcutModifiers = (int)mods;
        }

        public void Disable()
        {
            HotkeyManager.Current.Remove(HotkeyName);
        }
    }
}
