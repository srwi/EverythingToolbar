using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EverythingToolbar.Helpers
{
    public class StartMenuIntegration
    {
        public static readonly StartMenuIntegration Instance = new StartMenuIntegration();
        private static readonly Queue<INPUT> RecordedInputs = new Queue<INPUT>();
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<StartMenuIntegration>();

        private static WinEventDelegate _focusedWindowChangedCallback;
        private static LowLevelKeyboardProc _startMenuKeyboardHookCallback;
        private static IntPtr _focusedWindowChangedHookId = IntPtr.Zero;
        private static IntPtr _startMenuKeyboardHookId = IntPtr.Zero;

        private static IntPtr _searchAppHwnd = IntPtr.Zero;
        private static bool _isNativeSearchActive;
        private static bool _isInterceptingKeys;

        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmSyskeyDown = 0x0104;
        private const int InputKeyboard = 1;
        private const uint KeyeventFKeyup = 0x0002;

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

        private void OnFocusedWindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            GetForegroundWindowAndProcess(out var foregroundHwnd, out var foregroundProcessName);
            Logger.Debug($"Foreground process: {foregroundProcessName}");

            if (foregroundProcessName.EndsWith("SearchApp.exe") ||
                foregroundProcessName.EndsWith("SearchUI.exe") ||
                foregroundProcessName.EndsWith("SearchHost.exe"))
            {
                _searchAppHwnd = foregroundHwnd;
                HookStartMenuInput();
            }
            else
            {
                if (_isInterceptingKeys)
                {
                    _isInterceptingKeys = false;
                    TriggerSearchWindow();

                    Task.Run(async () =>
                    {
                        // In case something goes wrong we make sure the hook is removed
                        await Task.Delay(2000);
                        RecordedInputs.Clear();
                        UnhookStartMenuInput();
                    });
                }
                else
                {
                    UnhookStartMenuInput();
                }
                _isNativeSearchActive = false;
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

        private IntPtr StartMenuKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_isNativeSearchActive)
            {
                var virtualKeyCode = (uint)Marshal.ReadInt32(lParam);
                var isKeyDown = wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSyskeyDown;

                // We never want to block the Windows key
                if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                {
                    return CallNextHookEx(_startMenuKeyboardHookId, nCode, wParam, lParam);
                }

                // Check for exception key (LALT)
                if (virtualKeyCode == 0xA4)
                {
                    _isNativeSearchActive = true;
                    return CallNextHookEx(_startMenuKeyboardHookId, nCode, wParam, lParam);
                }

                // Queue keypress for replay in EverythingToolbar
                _isInterceptingKeys = true;
                RecordedInputs.Enqueue(new INPUT
                {
                    type = InputKeyboard,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)virtualKeyCode,
                            dwFlags = isKeyDown ? 0 : KeyeventFKeyup
                        }
                    }
                });

                CloseStartMenu();

                return (IntPtr)1;
            }

            return CallNextHookEx(_startMenuKeyboardHookId, nCode, wParam, lParam);
        }

        private void OnSearchBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            SearchWindow.Instance.SearchBox.GotKeyboardFocus -= OnSearchBoxGotKeyboardFocus;

            if (SearchWindow.Instance.SearchBox.TextBox.IsLoaded)
            {
                SearchWindow.Instance.SearchBox.Focus();
                ReplayRecordedInputs();
                UnhookStartMenuInput();
            }
            else
            {
                SearchWindow.Instance.SearchBox.TextBox.Loaded += (s, args) =>
                {
                    SearchWindow.Instance.SearchBox.Focus();
                    ReplayRecordedInputs();
                    UnhookStartMenuInput();
                };
            }
        }

        private void TriggerSearchWindow()
        {
            SearchWindow.Instance.SearchBox.GotKeyboardFocus += OnSearchBoxGotKeyboardFocus;
            SearchWindow.Instance.Show();
        }

        private void ReplayRecordedInputs()
        {
            UnhookStartMenuInput();

            while (RecordedInputs.Count > 0)
            {
                var input = RecordedInputs.Dequeue();
                keybd_event((byte)input.u.ki.wVk, (byte)input.u.ki.wScan, input.u.ki.dwFlags, input.u.ki.dwExtraInfo);
            }
        }

        private static void CloseStartMenu()
        {
            if (_searchAppHwnd != IntPtr.Zero)
            {
                PostMessage(_searchAppHwnd, 0x0010, 0, 0);
                NativeMethods.FocusTaskbarWindow();
                _searchAppHwnd = IntPtr.Zero;
            }
        }

        private void HookStartMenuInput()
        {
            UnhookStartMenuInput();
            _startMenuKeyboardHookCallback = StartMenuKeyboardHookCallback;
            _startMenuKeyboardHookId = SetWindowsHookEx(WhKeyboardLl, _startMenuKeyboardHookCallback, IntPtr.Zero, 0);
        }

        private void UnhookStartMenuInput()
        {
            UnhookWindowsHookEx(_startMenuKeyboardHookId);
            _startMenuKeyboardHookId = IntPtr.Zero;
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

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}