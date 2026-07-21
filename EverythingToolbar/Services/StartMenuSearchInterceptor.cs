using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private readonly Queue<RecordedKey> _recordedInputs = new();
        private readonly LowLevelKeyboardHook _keyboardHook;
        private WINEVENTPROC? _focusedWindowChangedCallback;
        private HWINEVENTHOOK _focusedWindowChangedHookId;

        private IntPtr _searchAppHwnd = IntPtr.Zero;
        private bool _isAttached;
        private bool _isNativeSearchActive;
        private bool _isInterceptingKeys;
        private bool? _animationsToRestore;
        private readonly DispatcherTimer _cleanupTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly ISettings _settings;
        private readonly SearchWindowController _controller;

        private const uint KeyeventFKeyup = 0x0002;

        public StartMenuSearchInterceptor(ISettings settings, SearchWindowController controller)
        {
            _settings = settings;
            _controller = controller;
            _keyboardHook = new LowLevelKeyboardHook(OnKeyEvent);
            _cleanupTimer.Tick += OnCleanupTimerElapsed;
            _settings.PropertyChanged += OnSettingsChanged;
        }

        public void Initialize()
        {
            _isAttached = true;

            if (_settings.IsReplaceStartMenuSearch)
                EnableHook();
        }

        // Called when the host detaches. The settings subscription lives for the lifetime of this
        // singleton, so without the flag a later settings change would re-arm the hooks for a host
        // that no longer exists.
        public void Disable()
        {
            _isAttached = false;
            DisableHook();
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isAttached)
                return;

            if (e.PropertyName == nameof(ISettings.IsReplaceStartMenuSearch))
            {
                if (_settings.IsReplaceStartMenuSearch)
                    EnableHook();
                else
                    DisableHook();
            }
        }

        private void EnableHook()
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

        private void DisableHook()
        {
            PInvoke.UnhookWinEvent(_focusedWindowChangedHookId);
            _focusedWindowChangedHookId = default;
            _focusedWindowChangedCallback = null;
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

        private bool OnKeyEvent(int vk, bool isDown, bool isInjected)
        {
            if (_isNativeSearchActive)
                return false;

            // We never want to block the Windows keys and Escape
            if (vk == 0x5B || vk == 0x5C || vk == 0x1B)
            {
                return false;
            }

            // Check for exception key (LALT)
            if (vk == 0xA4)
            {
                _isNativeSearchActive = true;
                return false;
            }

            // Queue keypress for replay in EverythingToolbar
            _isInterceptingKeys = true;
            _recordedInputs.Enqueue(new RecordedKey((ushort)vk, isDown));

            CloseStartMenu();

            return true;
        }

        private void OnAnySearchBoxGotKeyboardFocus(object? sender, EventArgs e)
        {
            if (!_isInterceptingKeys)
                return;

            _controller.SearchBoxFocused -= OnAnySearchBoxGotKeyboardFocus;

            Logger.Debug("Search box got keyboard focus. Replaying recorded inputs...");

            UnhookStartMenuInput();
            ReplayRecordedInputs();
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

        private void ReplayRecordedInputs()
        {
            while (_recordedInputs.Count > 0)
            {
                var input = _recordedInputs.Dequeue();
                NativeMethods.SendKeybdEvent((byte)input.Vk, 0, input.IsDown ? 0 : KeyeventFKeyup, IntPtr.Zero);
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
            _keyboardHook.Install();
        }

        private void UnhookStartMenuInput()
        {
            _keyboardHook.Uninstall();
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

        private readonly record struct RecordedKey(ushort Vk, bool IsDown);
    }
}
