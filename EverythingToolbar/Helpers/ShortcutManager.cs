using EverythingToolbar.Properties;
using NHotkey;
using NHotkey.Wpf;
using NLog;
using System;
using System.Windows;
using System.Windows.Input;

namespace EverythingToolbar.Helpers
{
    public class ShortcutManager
    {
        private const string HotkeyName = "EverythingToolbarHotkey";
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<ShortcutManager>();
        private static EventHandler<HotkeyEventArgs>? _handler;

        public static void Initialize(EventHandler<HotkeyEventArgs> handler)
        {
            _handler = handler;

            var shortcutKey = (Key)ToolbarSettings.User.ShortcutKey;
            var shortcutModifiers = (ModifierKeys)ToolbarSettings.User.ShortcutModifiers;

            if (shortcutKey == Key.None && shortcutModifiers == ModifierKeys.None)
                return;

            TrySetShortcut(shortcutKey, shortcutModifiers);
        }

        public static void TrySetShortcut(Key key, ModifierKeys modifiers)
        {
            try
            {
                HotkeyManager.Current.AddOrReplace(HotkeyName, key, modifiers, _handler);
                UpdateSettings(key, modifiers);
            }
            catch (HotkeyAlreadyRegisteredException e)
            {
                UpdateSettings(Key.None, ModifierKeys.None);

                Logger.Error(e, "Failed to register hotkey {0} with modifiers {1}", key, modifiers);
                MessageBox.Show(Resources.MessageBoxFailedToRegisterHotkey,
                    Resources.MessageBoxErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public static void UpdateSettings(Key key, ModifierKeys mods)
        {
            ToolbarSettings.User.ShortcutKey = (int)key;
            ToolbarSettings.User.ShortcutModifiers = (int)mods;
        }
    }
}