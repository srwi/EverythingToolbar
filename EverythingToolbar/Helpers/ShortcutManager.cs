using System;
using System.Windows;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using NLog;

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
            TrySetShortcut(
                (Key)ToolbarSettings.User.ShortcutKey,
                (ModifierKeys)ToolbarSettings.User.ShortcutModifiers,
                handler);
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
                MessageBox.Show(Properties.Resources.MessageBoxFailedToRegisterHotkey,
                    Properties.Resources.MessageBoxErrorTitle,
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
