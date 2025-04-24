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
        public static readonly ShortcutManager Instance = new ShortcutManager();
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<ShortcutManager>();

        private static EventHandler<HotkeyEventArgs> _shortcut;
        private const string HotkeyName = "EverythingToolbarHotkey";

        public void Initialize(EventHandler<HotkeyEventArgs> handler)
        {
            var shortcutKey = (Key)ToolbarSettings.User.ShortcutKey;
            var shortcutModifiers = (ModifierKeys)ToolbarSettings.User.ShortcutModifiers;

            if (shortcutKey == Key.None && shortcutModifiers == ModifierKeys.None)
                return;

            TrySetShortcut(shortcutKey, shortcutModifiers, handler);
        }

        private void TrySetShortcut(Key key, ModifierKeys modifiers, EventHandler<HotkeyEventArgs> handler)
        {
            try
            {
                HotkeyManager.Current.AddOrReplace(HotkeyName, key, modifiers, handler);

                _shortcut = handler;
                UpdateSettings(key, modifiers);
            }
            catch (HotkeyAlreadyRegisteredException e)
            {
                _shortcut = null;
                UpdateSettings(Key.None, ModifierKeys.None);

                Logger.Error(e, "Failed to register hotkey {0} with modifiers {1}", key, modifiers);
                MessageBox.Show(Resources.MessageBoxFailedToRegisterHotkey,
                    Resources.MessageBoxErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void TryUpdateShortcut(Key key, ModifierKeys modifiers)
        {
            TrySetShortcut(key, modifiers, _shortcut);
        }

        private void UpdateSettings(Key key, ModifierKeys mods)
        {
            ToolbarSettings.User.ShortcutKey = (int)key;
            ToolbarSettings.User.ShortcutModifiers = (int)mods;
        }
    }
}