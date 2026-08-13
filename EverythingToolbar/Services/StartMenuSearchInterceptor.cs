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

        private readonly object _stateLock = new();

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
        private Action? _showSearchUi;

        private const uint KeyeventFKeyup = 0x0002;
        private const uint WmClose = 0x0010;

        public StartMenuSearchInterceptor(ISettings settings, SearchWindowController controller)
        {
            _settings = settings;
            _controller = controller;
            _keyboardHook = new LowLevelKeyboardHook(OnKeyEvent);
            _cleanupTimer.Tick += OnCleanupTimerElapsed;
            _settings.PropertyChanged += OnSettingsChanged;
        }

        public void Initialize(Action showSearchUi)
        {
            _showSearchUi = showSearchUi;
            Enable();
        }

        public void Enable()
        {
            _isAttached = true;

            if (_settings.IsReplaceStartMenuSearch)
                EnableHook();
        }

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
                bool wasIntercepting;
                lock (_stateLock)
                    wasIntercepting = _isInterceptingKeys;

                if (wasIntercepting)
                {
                    Logger.Debug("Native search regained the foreground during handover. Resetting intercepted state.");
                    ResetHandoverState();
                }
                else
                {
                    RestoreAnimations();
                }

                lock (_stateLock)
                    _searchAppHwnd = foregroundHwnd;

                HookStartMenuInput();
                CancelCleanupTimer();
            }
            else
            {
                bool wasIntercepting;
                lock (_stateLock)
                    wasIntercepting = _isInterceptingKeys;

                if (wasIntercepting)
                {
                    TriggerSearchWindow();
                    StartCleanupTimer();
                }
                else
                {
                    UnhookStartMenuInput();
                }

                lock (_stateLock)
                    _isNativeSearchActive = false;
            }
        }

        private bool OnKeyEvent(int vk, bool isDown, bool isInjected)
        {
            // Called on the keyboard hook thread
            lock (_stateLock)
            {
                if (_isNativeSearchActive)
                    return false;

                // Check for exception key (LALT)
                if (vk == 0xA4)
                {
                    _isNativeSearchActive = true;
                    return false;
                }

                if (!IsPrintableKey(vk))
                    return false;

                // Queue keypress for replay in EverythingToolbar
                _isInterceptingKeys = true;
                _recordedInputs.Enqueue(new RecordedKey((ushort)vk, isDown));

                CloseStartMenu();

                return true;
            }
        }

        private static bool IsPrintableKey(int vk)
        {
            // Backspace
            if (vk == 0x08)
                return true;

            // Space
            if (vk == 0x20)
                return true;

            // Digits 0-9
            if (vk is >= 0x30 and <= 0x39)
                return true;

            // Letters A-Z
            if (vk is >= 0x41 and <= 0x5A)
                return true;

            // OEM punctuation keys (VK_OEM_1..8, VK_OEM_102, plus the ABNT/reserved
            // codes some layouts use for characters). Stops before VK_PROCESSKEY (IME).
            if (vk is >= 0xBA and <= 0xE2)
                return true;

            return false;
        }

        private void OnAnySearchBoxGotKeyboardFocus(object? sender, EventArgs e)
        {
            lock (_stateLock)
            {
                if (!_isInterceptingKeys)
                    return;
            }

            _controller.SearchBoxFocused -= OnAnySearchBoxGotKeyboardFocus;

            Logger.Debug("Search box got keyboard focus. Replaying recorded inputs...");

            UnhookStartMenuInput(); // Stops the hook thread; no more keys can be recorded

            lock (_stateLock)
            {
                ReplayRecordedInputs();
                _isInterceptingKeys = false;
                _searchAppHwnd = IntPtr.Zero;
            }
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
            if (_showSearchUi is not { } showSearchUi)
                return;

            _controller.SearchBoxFocused -= OnAnySearchBoxGotKeyboardFocus;
            _controller.SearchBoxFocused += OnAnySearchBoxGotKeyboardFocus;
            _dispatcher.BeginInvoke(showSearchUi, DispatcherPriority.Input);
        }

        // Must be called while holding _stateLock
        private void ReplayRecordedInputs()
        {
            while (_recordedInputs.Count > 0)
            {
                var input = _recordedInputs.Dequeue();
                NativeMethods.SendKeybdEvent((byte)input.Vk, 0, input.IsDown ? 0 : KeyeventFKeyup, IntPtr.Zero);
            }
        }

        // Must be called while holding _stateLock
        private void CloseStartMenu()
        {
            if (_searchAppHwnd == IntPtr.Zero)
                return;

            var startMenuHwnd = (HWND)_searchAppHwnd;
            _searchAppHwnd = IntPtr.Zero;
            _animationsToRestore ??= SystemSettings.GetSystemAnimationsEnabled();

            // We hand slow operations to the dispatcher
            _dispatcher.BeginInvoke(
                new Action(() =>
                {
                    lock (_stateLock)
                    {
                        // The handover may have been reset (and the animations restored) since.
                        if (_animationsToRestore == null)
                            return;
                    }

                    SystemSettings.SetSystemAnimationsEnabled(false);
                    PInvoke.PostMessage(startMenuHwnd, WmClose, 0, 0);
                })
            );
        }

        private void ResetHandoverState()
        {
            CancelCleanupTimer();
            UnhookStartMenuInput(); // Stops the hook thread first so the state reset cannot race it
            _controller.SearchBoxFocused -= OnAnySearchBoxGotKeyboardFocus;

            lock (_stateLock)
            {
                _recordedInputs.Clear();
                _searchAppHwnd = IntPtr.Zero;
                _isInterceptingKeys = false;
                _isNativeSearchActive = false;
            }

            RestoreAnimations();
        }

        // Must not be called while holding _stateLock: the broadcast this triggers can take far
        // longer than the keyboard hook thread is allowed to wait for the lock.
        private void RestoreAnimations()
        {
            bool? enabled;
            lock (_stateLock)
            {
                enabled = _animationsToRestore;
                _animationsToRestore = null;
            }

            if (enabled is { } value)
                SystemSettings.SetSystemAnimationsEnabled(value);
        }

        private void HookStartMenuInput()
        {
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
            uint length = PInvoke.GetModuleFileNameEx(processHandle, null, nameBuffer);
            foregroundProcessName = nameBuffer[..(int)length].ToString();
        }

        private readonly record struct RecordedKey(ushort Vk, bool IsDown);
    }
}
