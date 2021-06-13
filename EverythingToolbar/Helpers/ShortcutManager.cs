using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace EverythingToolbar.Helpers
{
    class ShortcutManager
    {
        public class WinKeyEventArgs : EventArgs
        {
            public WinKeyEventArgs(bool isDown, Key key)
            {
                Key = key;
                IsDown = isDown;
            }

            public bool IsDown { get; set; }
            public Key Key { get; set; }
        }

        public static readonly ShortcutManager Instance = new ShortcutManager();

        private static Dictionary<string, EventHandler<HotkeyEventArgs>> shortcuts = new Dictionary<string, EventHandler<HotkeyEventArgs>>();
        private static event EventHandler<WinKeyEventArgs> winKeyEventHandler;
        private static readonly LowLevelKeyboardProc proc = HookCallback;
        private static IntPtr hookId = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private ShortcutManager() { }

        public bool AddOrReplace(string name, Key key, ModifierKeys modifiers, EventHandler<HotkeyEventArgs> handler)
        {
            try
            {
                shortcuts[name] = handler;
                HotkeyManager.Current.AddOrReplace(name, key, modifiers, handler);
                return true;
            }
            catch (Exception e)
            {
                ToolbarLogger.GetLogger("EverythingInstance").Error(e, "Failed to register hotkey.");
                return false;
            }
        }

        public bool AddOrReplace(string name, Key key, ModifierKeys modifiers)
        {
            return AddOrReplace(name, key, modifiers, shortcuts[name]);
        }

        public void SetShortcut(Key key, ModifierKeys mods)
        {
            Properties.Settings.Default.shortcutKey = (int)key;
            Properties.Settings.Default.shortcutModifiers = (int)mods;
            Properties.Settings.Default.Save();
        }

        public bool CaptureKeyboard(EventHandler<WinKeyEventArgs> callback)
        {
            ReleaseKeyboard();
            winKeyEventHandler += callback;
            hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, (IntPtr)0, 0);
            return hookId != IntPtr.Zero;
        }

        public bool ReleaseKeyboard()
        {
            winKeyEventHandler = null;
            return UnhookWindowsHookEx(hookId);
        }

        public static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Keys vkCode = (Keys)Marshal.ReadInt32(lParam);
                bool isDown = (int)wParam == WM_KEYDOWN || (int)wParam == WM_SYSKEYDOWN;
                switch (vkCode)
                {
                    case Keys.Control:
                    case Keys.ControlKey:
                    case Keys.LControlKey:
                    case Keys.RControlKey:
                        winKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftCtrl));
                        break;
                    case Keys.Shift:
                    case Keys.ShiftKey:
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        winKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftShift));
                        break;
                    case Keys.Alt:
                        winKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftAlt));
                        break;
                    case Keys.LWin:
                    case Keys.RWin:
                        winKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LWin));
                        break;
                    default:
                        winKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, KeyInterop.KeyFromVirtualKey((int)vkCode)));
                        break;
                }

                return (IntPtr)1;
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
                                                      LowLevelKeyboardProc lpfn,
                                                      IntPtr hMod,
                                                      uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
                                                   IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
