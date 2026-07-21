using System;
using System.Windows.Input;
using System.Windows.Threading;
using NLog;
using Windows.Win32;

namespace EverythingToolbar.Services
{
    public class GlobalShortcutListener
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<GlobalShortcutListener>();

        private readonly ISettings _settings;
        private readonly LowLevelKeyboardHook _keyboardHook;

        private Action? _handler;
        private Dispatcher? _dispatcher;

        private int _triggerVk;
        private ModifierKeys _modifiers;
        private bool _hotkeyDown;

        public bool IsEnabled { get; set; } = true;

        private const int VkShift = 0x10;
        private const int VkControl = 0x11;
        private const int VkMenu = 0x12; // Alt
        private const int VkLWin = 0x5B;
        private const int VkRWin = 0x5C;

        private const uint KeyeventfKeyup = 0x0002;

        public GlobalShortcutListener(ISettings settings)
        {
            _settings = settings;
            _keyboardHook = new LowLevelKeyboardHook(OnKeyEvent);
        }

        public void Initialize(Action handler)
        {
            _handler = handler;
            _dispatcher = Dispatcher.CurrentDispatcher;

            SetShortcut((Key)_settings.ShortcutKey, (ModifierKeys)_settings.ShortcutModifiers);
        }

        public void SetShortcut(Key key, ModifierKeys modifiers)
        {
            _triggerVk = key == Key.None ? 0 : KeyInterop.VirtualKeyFromKey(key);
            _modifiers = modifiers;
            _hotkeyDown = false;

            UpdateSettings(key, modifiers);

            if (key == Key.None && modifiers == ModifierKeys.None)
                _keyboardHook.Uninstall();
            else
                _keyboardHook.Install();
        }

        public void Disable()
        {
            _keyboardHook.Uninstall();
            _hotkeyDown = false;
        }

        private void UpdateSettings(Key key, ModifierKeys mods)
        {
            _settings.ShortcutKey = (int)key;
            _settings.ShortcutModifiers = (int)mods;
        }

        private bool OnKeyEvent(int vk, bool isDown, bool isInjected)
        {
            try
            {
                if (!IsEnabled || _triggerVk == 0)
                    return false;

                if (isInjected)
                    return false;

                if (vk != _triggerVk)
                    return false;

                if (!isDown)
                {
                    if (!_hotkeyDown)
                        return false;

                    _hotkeyDown = false;
                    return true; // Swallow the key up matching a suppressed key down
                }

                if (_hotkeyDown)
                    return true; // Swallow auto-repeat while the hotkey is held

                if (GetCurrentModifiers() == _modifiers)
                {
                    _hotkeyDown = true;
                    DisguiseModifiersIfNeeded();
                    _dispatcher?.BeginInvoke(() => _handler?.Invoke());
                    return true; // Swallow the trigger key
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in the keyboard hook callback.");
                return false;
            }
        }

        private static ModifierKeys GetCurrentModifiers()
        {
            var modifiers = ModifierKeys.None;
            if (IsKeyDown(VkControl))
                modifiers |= ModifierKeys.Control;
            if (IsKeyDown(VkShift))
                modifiers |= ModifierKeys.Shift;
            if (IsKeyDown(VkMenu))
                modifiers |= ModifierKeys.Alt;
            if (IsKeyDown(VkLWin) || IsKeyDown(VkRWin))
                modifiers |= ModifierKeys.Windows;
            return modifiers;
        }

        private void DisguiseModifiersIfNeeded()
        {
            // Tapping the Windows key opens the Start menu and tapping Alt activates the window
            // menu, both on key up. Because we swallow the actual trigger key, Windows would treat
            // the modifier as if it had been pressed on its own. Injecting a neutral Ctrl tap while
            // the modifier is still held disguises it and prevents that behavior.
            if ((_modifiers & (ModifierKeys.Windows | ModifierKeys.Alt)) == 0)
                return;

            NativeMethods.SendKeybdEvent(VkControl, 0, 0, IntPtr.Zero);
            NativeMethods.SendKeybdEvent(VkControl, 0, KeyeventfKeyup, IntPtr.Zero);
        }

        private static bool IsKeyDown(int vk) => (PInvoke.GetAsyncKeyState(vk) & 0x8000) != 0;
    }
}
