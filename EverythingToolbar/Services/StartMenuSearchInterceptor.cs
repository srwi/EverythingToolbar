using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Accessibility;

namespace EverythingToolbar.Services
{
    public class StartMenuSearchInterceptor
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<StartMenuSearchInterceptor>();

        private readonly Queue<Input> _recordedInputs = new();
        private WINEVENTPROC? _focusedWindowChangedCallback;
        private NativeMethods.LowLevelKeyboardProc? _startMenuKeyboardHookCallback;
        private HWINEVENTHOOK _focusedWindowChangedHookId;
        private IntPtr _startMenuKeyboardHookId = IntPtr.Zero;

        private IntPtr _searchAppHwnd = IntPtr.Zero;
        private bool _isNativeSearchActive;
        private bool _isInterceptingKeys;
        private bool? _animationsToRestore;
        private readonly DispatcherTimer _cleanupTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly ISettings _settings;
        private readonly SearchWindowController _controller;

        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmSyskeyDown = 0x0104;
        private const int InputKeyboard = 1;
        private const uint KeyeventFKeyup = 0x0002;

        public StartMenuSearchInterceptor(ISettings settings, SearchWindowController controller)
        {
            _settings = settings;
            _controller = controller;
            _cleanupTimer.Tick += OnCleanupTimerElapsed;
            _settings.PropertyChanged += OnSettingsChanged;
        }

        public void Initialize()
        {
            if (_settings.IsReplaceStartMenuSearch)
                Enable();
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettings.IsReplaceStartMenuSearch))
            {
                if (_settings.IsReplaceStartMenuSearch)
                    Enable();
                else
                    Disable();
            }
        }

        private void Enable()
        {
            PInvoke.UnhookWinEvent(_focusedWindowChangedHookId);
            _focusedWindowChangedCallback = OnFocusedWindowChanged;
            _focusedWindowChangedHookId = PInvoke.SetWinEventHook(
                3,
                3,
                default(HMODULE),
                _focusedWindowChangedCallback,
                0,
                0,
                0
            );
            CancelCleanupTimer();
        }

        public void Disable()
        {
            PInvoke.UnhookWinEvent(_focusedWindowChangedHookId);
            _focusedWindowChangedHookId = default;
            ResetHandoverState();
        }

        private void OnFocusedWindowChanged(
            HWINEVENTHOOK hWinEventHook,
            uint eventType,
            HWND hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime
        )
        {
            GetForegroundWindowAndProcess(out var foregroundHwnd, out var foregroundProcessName);
            Logger.Debug($"Foreground process: {foregroundProcessName}");

            if (
                foregroundProcessName.EndsWith("SearchApp.exe")
                || foregroundProcessName.EndsWith("SearchUI.exe")
                || foregroundProcessName.EndsWith("SearchHost.exe")
            )
            {
                if (_isInterceptingKeys)
                {
                    Logger.Debug("Native search regained the foreground during handover. Resetting intercepted state.");
                    ResetHandoverState();
                }
                else
                {
                    RestoreAnimations();
                }

                _searchAppHwnd = foregroundHwnd;

                HookStartMenuInput();
                CancelCleanupTimer();
            }
            else
            {
                if (_isInterceptingKeys)
                {
                    TriggerSearchWindow();
                    StartCleanupTimer();
                }
                else
                {
                    UnhookStartMenuInput();
                }
                _isNativeSearchActive = false;
            }
        }

        private IntPtr StartMenuKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_isNativeSearchActive)
            {
                var virtualKeyCode = (uint)Marshal.ReadInt32(lParam);
                var isKeyDown = wParam is WmKeyDown or WmSyskeyDown;

                // We never want to block the Windows keys and Escape
                if (virtualKeyCode == 0x5B || virtualKeyCode == 0x5C || virtualKeyCode == 0x1B)
                {
                    return NativeMethods.CallNextHookEx(_startMenuKeyboardHookId, nCode, wParam, lParam);
                }

                // Check for exception key (LALT)
                if (virtualKeyCode == 0xA4)
                {
                    _isNativeSearchActive = true;
                    return NativeMethods.CallNextHookEx(_startMenuKeyboardHookId, nCode, wParam, lParam);
                }

                // Queue keypress for replay in EverythingToolbar
                _isInterceptingKeys = true;
                _recordedInputs.Enqueue(
                    new Input
                    {
                        type = InputKeyboard,
                        u = new InputUnion
                        {
                            ki = new KeybdInput
                            {
                                wVk = (ushort)virtualKeyCode,
                                dwFlags = isKeyDown ? 0 : KeyeventFKeyup,
                            },
                        },
                    }
                );

                CloseStartMenu();

                return 1;
            }

            return NativeMethods.CallNextHookEx(_startMenuKeyboardHookId, nCode, wParam, lParam);
        }

        private void OnAnySearchBoxGotKeyboardFocus(object? sender, EventArgs e)
        {
            if (!_isInterceptingKeys)
                return;

            _controller.SearchBoxFocused -= OnAnySearchBoxGotKeyboardFocus;

            Logger.Debug("Search box got keyboard focus. Replaying recorded inputs...");

            UnhookStartMenuInput();
            Replay_recordedInputs();
            _isInterceptingKeys = false;
            _searchAppHwnd = IntPtr.Zero;
        }

        private void StartCleanupTimer()
        {
            _cleanupTimer.Stop();
            _cleanupTimer.Start();
        }

        private void CancelCleanupTimer()
        {
            _cleanupTimer.Stop();
        }

        private void OnCleanupTimerElapsed(object? sender, EventArgs e)
        {
            Logger.Debug("Cleanup timer elapsed. Clearing recorded inputs and unhooking keyboard hook.");
            ResetHandoverState();
        }

        private void TriggerSearchWindow()
        {
            _controller.SearchBoxFocused -= OnAnySearchBoxGotKeyboardFocus;
            _controller.SearchBoxFocused += OnAnySearchBoxGotKeyboardFocus;
            _dispatcher.BeginInvoke(
                new Action(() =>
                {
                    _controller.Show();
                    _controller.FocusSearchBox();
                }),
                DispatcherPriority.Input
            );
        }

        private void Replay_recordedInputs()
        {
            while (_recordedInputs.Count > 0)
            {
                var input = _recordedInputs.Dequeue();
                NativeMethods.KeybdEvent(
                    (byte)input.u.ki.wVk,
                    (byte)input.u.ki.wScan,
                    input.u.ki.dwFlags,
                    input.u.ki.dwExtraInfo
                );
            }
        }

        private void CloseStartMenu()
        {
            if (_searchAppHwnd != IntPtr.Zero)
            {
                _animationsToRestore ??= SystemSettings.GetSystemAnimationsEnabled();
                SystemSettings.SetSystemAnimationsEnabled(false);
                PInvoke.PostMessage((HWND)_searchAppHwnd, 0x0010, 0, 0);
                _searchAppHwnd = IntPtr.Zero;
            }
        }

        private void ResetHandoverState()
        {
            CancelCleanupTimer();
            _recordedInputs.Clear();
            UnhookStartMenuInput();
            _controller.SearchBoxFocused -= OnAnySearchBoxGotKeyboardFocus;
            _searchAppHwnd = IntPtr.Zero;
            _isInterceptingKeys = false;
            _isNativeSearchActive = false;
            RestoreAnimations();
        }

        private void RestoreAnimations()
        {
            if (_animationsToRestore is not bool enabled)
                return;

            SystemSettings.SetSystemAnimationsEnabled(enabled);
            _animationsToRestore = null;
        }

        private void HookStartMenuInput()
        {
            UnhookStartMenuInput();
            _startMenuKeyboardHookCallback = StartMenuKeyboardHookCallback;
            _startMenuKeyboardHookId = NativeMethods.SetWindowsHookEx(
                WhKeyboardLl,
                _startMenuKeyboardHookCallback,
                IntPtr.Zero,
                0
            );
        }

        private void UnhookStartMenuInput()
        {
            NativeMethods.UnhookWindowsHookEx(_startMenuKeyboardHookId);
            _startMenuKeyboardHookId = IntPtr.Zero;
        }

        private static void GetForegroundWindowAndProcess(out IntPtr foregroundHwnd, out string foregroundProcessName)
        {
            foregroundHwnd = NativeMethods.GetForegroundWindow();
            NativeMethods.GetWindowThreadProcessId(foregroundHwnd, out var processId);

            using var processHandle = PInvoke.OpenProcess_SafeHandle(
                PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ,
                false,
                processId
            );

            Span<char> nameBuffer = new char[1000];
            uint length = PInvoke.GetModuleFileNameEx(processHandle, default, nameBuffer);
            foregroundProcessName = nameBuffer[..(int)length].ToString();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KeybdInput ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeybdInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
