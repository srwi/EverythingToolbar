using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, EventHandler<HotkeyEventArgs>> Shortcuts = new Dictionary<string, EventHandler<HotkeyEventArgs>>();

        public bool AddOrReplace(string name, Key key, ModifierKeys modifiers, EventHandler<HotkeyEventArgs> handler)
        {
            try
            {
                Shortcuts[name] = handler;
                HotkeyManager.Current.AddOrReplace(name, key, modifiers, handler);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to register hotkey.");
                return false;
            }
        }

        public bool AddOrReplace(string name, Key key, ModifierKeys modifiers)
        {
            return AddOrReplace(name, key, modifiers, Shortcuts[name]);
        }

        public void SetShortcut(Key key, ModifierKeys mods)
        {
            ToolbarSettings.User.ShortcutKey = (int)key;
            ToolbarSettings.User.ShortcutModifiers = (int)mods;
        }
    }
}
