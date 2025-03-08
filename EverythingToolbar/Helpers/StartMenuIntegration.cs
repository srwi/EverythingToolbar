using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using NHotkey;

namespace EverythingToolbar.Helpers
{
    public class StartMenuIntegration
    {
        public static readonly StartMenuIntegration Instance = new StartMenuIntegration();

        private static WinEventDelegate _focusedWindowChangedCallback;
        private static LowLevelKeyboardProc _llKeyboardHookCallback;
        private static Action<object, HotkeyEventArgs> _focusToolbarCallback;

        private static IntPtr _llKeyboardHookId = IntPtr.Zero;
        private static IntPtr _focusedWindowChangedHookId = IntPtr.Zero;

        private static IntPtr _searchAppHwnd = IntPtr.Zero;

        private static bool _isException;
        private static bool _isNativeSearchActive;
        private static string _searchTermQueue = "";

        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmSyskeydown = 0x0104;

        private StartMenuIntegration()
        {
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToolbarSettings.User.IsReplaceStartMenuSearch))
            {
                if (ToolbarSettings.User.IsReplaceStartMenuSearch)
                    Enable();
                else
                    Disable();
            }
        }

        public void Enable()
        {
            _focusedWindowChangedCallback = OnFocusedWindowChanged;
            _focusedWindowChangedHookId = SetWinEventHook(3, 3, IntPtr.Zero, _focusedWindowChangedCallback, 0, 0, 0);
        }

        public void Disable()
        {
            UnhookWinEvent(_focusedWindowChangedHookId);
        }

        public void SetFocusToolbarCallback(Action<object, HotkeyEventArgs> callback)
        {
            _focusToolbarCallback = callback;
        }

        private void OnFocusedWindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            GetForegroundWindowAndProcess(out var foregroundHwnd, out var foregroundProcessName);

            if (foregroundProcessName.EndsWith("SearchApp.exe") ||
                foregroundProcessName.EndsWith("SearchUI.exe") ||
                foregroundProcessName.EndsWith("SearchHost.exe"))
            {
                _searchAppHwnd = foregroundHwnd;
                _searchTermQueue = "";
                HookStartMenuInput();
            }
            else
            {
                if (_searchAppHwnd != IntPtr.Zero && !string.IsNullOrEmpty(_searchTermQueue))
                {
                    _searchAppHwnd = IntPtr.Zero;
                    _focusToolbarCallback?.Invoke(null, null);
                    SearchWindow.Instance.Show();
                    EventDispatcher.Instance.InvokeSearchTermReplaced(this, _searchTermQueue);
                    _searchTermQueue = "";
                }
                _isException = false;
                _isNativeSearchActive = false;

                UnhookStartMenuInput();
            }
        }

        private static void GetForegroundWindowAndProcess(out IntPtr foregroundHwnd, out string foregroundProcessName)
        {
            foregroundHwnd = GetForegroundWindow();
            GetWindowThreadProcessId(foregroundHwnd, out var processId);
            var processHandle = OpenProcess(0x0410, false, processId);
            var processNameBuilder = new StringBuilder(1000);
            GetModuleFileNameEx(processHandle, IntPtr.Zero, processNameBuilder, processNameBuilder.Capacity);
            CloseHandle(processHandle);
            foregroundProcessName = processNameBuilder.ToString();
        }

        private static IntPtr StartMenuKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_isNativeSearchActive)
            {
                var virtualKeyCode = (uint)Marshal.ReadInt32(lParam);
                var isKeyDown = wParam == (IntPtr)WmKeydown || wParam == (IntPtr)WmSyskeydown;

                if(Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                {
                    return CallNextHookEx(_llKeyboardHookId, nCode, wParam, lParam);
                }
                // Check for exception keys (VK_LCONTROL, VK_RCONTROL, VK_LMENU)
                if (virtualKeyCode == 0xA2 || virtualKeyCode == 0xA3 || virtualKeyCode == 0xA4)
                {
                    _isException = isKeyDown;
                    return (IntPtr)1;
                }

                // Determine key string
                var keyboardState = new byte[255];
                var keyString = "";
                if (GetKeyboardState(keyboardState))
                {
                    var scanCode = MapVirtualKey(virtualKeyCode, 0);
                    var inputLocaleIdentifier = GetKeyboardLayout(0);
                    var keyStringbuilder = new StringBuilder();
                    ToUnicodeEx(virtualKeyCode, scanCode, keyboardState, keyStringbuilder, 5, 0, inputLocaleIdentifier);
                    keyString = keyStringbuilder.ToString();
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        keyString = keyString.ToUpper();
                }

                if (keyString.Length > 0 && isKeyDown &&
                    (char.IsLetterOrDigit(keyString, 0) ||
                     char.IsPunctuation(keyString, 0) ||
                     char.IsSymbol(keyString, 0)))
                {
                    // Send input to native search app
                    if (_isException)
                    {
                        _isNativeSearchActive = true;
                        return CallNextHookEx(_llKeyboardHookId, nCode, wParam, lParam);
                    }

                    // Send input to EverythingToolbar
                    _searchTermQueue += keyString;

                    CloseStartMenu();

                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_llKeyboardHookId, nCode, wParam, lParam);
        }

        private static void CloseStartMenu()
        {
            if (_searchAppHwnd != IntPtr.Zero)
            {
                PostMessage(_searchAppHwnd, 0x0010, 0, 0);
            }
        }

        private void HookStartMenuInput()
        {
            UnhookStartMenuInput();
            _llKeyboardHookCallback = StartMenuKeyboardHookCallback;
            _llKeyboardHookId = SetWindowsHookEx(WhKeyboardLl, _llKeyboardHookCallback, IntPtr.Zero, 0);
        }

        private void UnhookStartMenuInput()
        {
            UnhookWindowsHookEx(_llKeyboardHookId);
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