using System;
using System.Runtime.InteropServices;
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

        private Action? _handler;
        private Dispatcher? _dispatcher;

        private int _triggerVk;
        private ModifierKeys _modifiers;
        private bool _hotkeyDown;

        public bool IsEnabled { get; set; } = true;

        private NativeMethods.LowLevelKeyboardProc? _hookCallback;
        private IntPtr _hookId = IntPtr.Zero;

        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmKeyup = 0x0101;
        private const int WmSyskeydown = 0x0104;
        private const int WmSyskeyup = 0x0105;

        private const int VkShift = 0x10;
        private const int VkControl = 0x11;
        private const int VkMenu = 0x12; // Alt
        private const int VkLWin = 0x5B;
        private const int VkRWin = 0x5C;

        private const int LlkhfInjected = 0x10;
        private const uint KeyeventfKeyup = 0x0002;

        public GlobalShortcutListener(ISettings settings)
        {
            _settings = settings;
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
                RemoveHook();
            else
                InstallHook();
        }

        private void UpdateSettings(Key key, ModifierKeys mods)
        {
            _settings.ShortcutKey = (int)key;
            _settings.ShortcutModifiers = (int)mods;
        }

        private void InstallHook()
        {
            if (_hookId != IntPtr.Zero)
                return;

            _hookCallback = HookCallback;
            _hookId = NativeMethods.SetWindowsHookEx(WhKeyboardLl, _hookCallback, IntPtr.Zero, 0);

            if (_hookId == IntPtr.Zero)
                Logger.Error("Failed to install the keyboard hook for the global shortcut.");
        }

        private void RemoveHook()
        {
            if (_hookId == IntPtr.Zero)
                return;

            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _hookCallback = null;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode < 0 || !IsEnabled || _triggerVk == 0)
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

                if (Marshal.ReadInt32(lParam) != _triggerVk)
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

                // Ignore events we injected ourselves (e.g. the disguise keystroke below).
                var flags = Marshal.ReadInt32(lParam, 8);
                if ((flags & LlkhfInjected) != 0)
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

                var message = (int)wParam;

                if (message == WmKeyup || message == WmSyskeyup)
                {
                    if (!_hotkeyDown)
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

                    _hotkeyDown = false;
                    return 1; // Swallow the key up matching a suppressed key down
                }

                if (message == WmKeydown || message == WmSyskeydown)
                {
                    if (_hotkeyDown)
                        return 1; // Swallow auto-repeat while the hotkey is held

                    if (GetCurrentModifiers() == _modifiers)
                    {
                        _hotkeyDown = true;
                        DisguiseModifiersIfNeeded();
                        _dispatcher?.BeginInvoke(() => _handler?.Invoke());
                        return 1; // Swallow the trigger key
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in the keyboard hook callback.");
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
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

            NativeMethods.keybd_event(VkControl, 0, 0, IntPtr.Zero);
            NativeMethods.keybd_event(VkControl, 0, KeyeventfKeyup, IntPtr.Zero);
        }

        private static bool IsKeyDown(int vk) => (PInvoke.GetAsyncKeyState(vk) & 0x8000) != 0;
    }
}
