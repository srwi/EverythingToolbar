using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace EverythingToolbar.Helpers
{
    class ShortcutManager
    {
        public class WinKeyEventArgs : EventArgs
        {
            public WinKeyEventArgs(bool isDown)
            {
                IsDown = isDown;
            }

            public bool IsDown { get; set; }
        }

        public static readonly ShortcutManager Instance = new ShortcutManager();

        private static Dictionary<string, EventHandler<HotkeyEventArgs>> shortcuts = new Dictionary<string, EventHandler<HotkeyEventArgs>>();
        private static event EventHandler<WinKeyEventArgs> winKeyEventHandler;
        private static readonly LowLevelKeyboardProc proc = HookCallback;
        private static IntPtr hookId = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int WM_KEYUP = 0x0101;
        private const int WM_KEYDOWN = 0x0100;

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

        public bool LockWindowsKey(EventHandler<WinKeyEventArgs> callback)
        {
            ReleaseWindowsKey();
            winKeyEventHandler += callback;
            hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, (IntPtr)0, 0);
            return hookId != IntPtr.Zero;
        }

        public bool ReleaseWindowsKey()
        {
            winKeyEventHandler = null;
            return UnhookWindowsHookEx(hookId);
        }

        public static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN)
                        winKeyEventHandler?.Invoke(null, new WinKeyEventArgs(true));
                    else if (wParam == (IntPtr)WM_KEYUP)
                        winKeyEventHandler?.Invoke(null, new WinKeyEventArgs(false));
                    return (IntPtr)1;
                }
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
