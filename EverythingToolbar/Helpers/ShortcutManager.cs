using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
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

        private WinEventDelegate winEventDelegate = null;
        private static Dictionary<string, EventHandler<HotkeyEventArgs>> shortcuts = new Dictionary<string, EventHandler<HotkeyEventArgs>>();
        private static Action<object, HotkeyEventArgs> focusToolbarCallback;
        private static LowLevelKeyboardProc llKeyboardHookProc;
        private static IntPtr llKeyboardHookId = IntPtr.Zero;
        private static IntPtr winEventHookId = IntPtr.Zero;
        private static IntPtr searchAppHwnd = IntPtr.Zero;
        private static event EventHandler<WinKeyEventArgs> winKeyEventHandler;
        private static bool isShiftDown = false;
        private static bool isException = false;
        private static bool isNativeSearchActive = false;
        private static string searchTermQueue = "";
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private ShortcutManager()
        {
            Properties.Settings.Default.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isReplaceStartMenuSearch")
            {
                if (Properties.Settings.Default.isReplaceStartMenuSearch)
                    HookStartMenu();
                else
                    UnhookStartMenu();
            }
        }

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

        public void CaptureKeyboard(EventHandler<WinKeyEventArgs> callback)
        {
            ReleaseKeyboard();
            winKeyEventHandler += callback;
            llKeyboardHookProc = WinKeyHookCallback;
            llKeyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, llKeyboardHookProc, (IntPtr)0, 0);
        }

        public bool ReleaseKeyboard()
        {
            winKeyEventHandler = null;
            return UnhookWindowsHookEx(llKeyboardHookId);
        }

        public static IntPtr WinKeyHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
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

            return CallNextHookEx(llKeyboardHookId, nCode, wParam, lParam);
        }

        public void SetFocusCallback(Action<object, HotkeyEventArgs> callback)
        {
            focusToolbarCallback = callback;
        }

        public void HookStartMenu()
        {
            winEventDelegate = new WinEventDelegate(WinEventProc);
            winEventHookId = SetWinEventHook(3, 3, IntPtr.Zero, winEventDelegate, 0, 0, 0);
        }

        public void UnhookStartMenu()
        {
            UnhookWinEvent(winEventHookId);
        }

        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            IntPtr hWnd = GetForegroundWindow();
            uint lpdwProcessId;
            GetWindowThreadProcessId(hWnd, out lpdwProcessId);
            IntPtr hProcess = OpenProcess(0x0410, false, lpdwProcessId);
            StringBuilder text = new StringBuilder(1000);
            GetModuleFileNameEx(hProcess, IntPtr.Zero, text, text.Capacity);
            CloseHandle(hProcess);

            if (text.ToString().EndsWith("SearchApp.exe") || text.ToString().EndsWith("SearchUI.exe"))
            {
                searchAppHwnd = hWnd;
                searchTermQueue = "";
                HookStartMenuInput();
            }
            else
            {
                if (searchAppHwnd != IntPtr.Zero && !string.IsNullOrEmpty(searchTermQueue))
                {
                    searchAppHwnd = IntPtr.Zero;
                    focusToolbarCallback?.Invoke(null, null);
                    EverythingSearch.Instance.SearchTerm = searchTermQueue;
                    searchTermQueue = "";
                }
                isException = false;
                isNativeSearchActive = false;
                UnhookStartMenuInput();
            }
        }

        public static IntPtr StartMenuKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !isNativeSearchActive)
            {
                uint virtualKeyCode = (uint)Marshal.ReadInt32(lParam);
                bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;

                // Check for VK_LSHIFT and VK_RSHIFT
                if (virtualKeyCode == 0xA0 || virtualKeyCode == 0xA1)
                {
                    isShiftDown = isKeyDown;
                    return CallNextHookEx(llKeyboardHookId, nCode, wParam, lParam);
                }
                // Check for exception keys (VK_LCONTROL, VK_RCONTROL, VK_LMENU)
                else if (virtualKeyCode == 0xA2 || virtualKeyCode == 0xA3 || virtualKeyCode == 0xA4)
                {
                    isException = isKeyDown;
                    return (IntPtr)1;
                }

                // Determine key string
                byte[] keyboardState = new byte[255];
                string keyString = "";
                if (GetKeyboardState(keyboardState))
                {
                    uint scanCode = MapVirtualKey(virtualKeyCode, 0);
                    IntPtr inputLocaleIdentifier = GetKeyboardLayout(0);
                    StringBuilder keyStringbuilder = new StringBuilder();
                    ToUnicodeEx(virtualKeyCode, scanCode, keyboardState, keyStringbuilder, 5, 0, inputLocaleIdentifier);
                    keyString = keyStringbuilder.ToString();
                    if (isShiftDown)
                        keyString = keyString.ToUpper();
                }

                if (keyString.Length > 0 && isKeyDown &&
                    (char.IsLetterOrDigit(keyString, 0) ||
                     char.IsPunctuation(keyString, 0) ||
                     char.IsSymbol(keyString, 0)))
                {
                    // Send input to native search app
                    if (isException)
                    {
                        isNativeSearchActive = true;
                        return CallNextHookEx(llKeyboardHookId, nCode, wParam, lParam);
                    }
                    // Send input to EverythingToolbar
                    else
                    {
                        searchTermQueue += keyString.ToString();
                        CloseStartMenu();
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(llKeyboardHookId, nCode, wParam, lParam);
        }
        
        public static void CloseStartMenu()
        {
            if (searchAppHwnd != null)
            {
                PostMessage(searchAppHwnd, 0x0010, 0, 0);
            }
        }

        public void HookStartMenuInput()
        {
            UnhookStartMenuInput();
            llKeyboardHookProc = StartMenuKeyboardHookCallback;
            llKeyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, llKeyboardHookProc, (IntPtr)0, 0);
        }

        public void UnhookStartMenuInput()
        {
            isShiftDown = false;
            UnhookWindowsHookEx(llKeyboardHookId);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.Dll")]
        static extern int PostMessage(IntPtr hWnd, UInt32 msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);
    }
}
