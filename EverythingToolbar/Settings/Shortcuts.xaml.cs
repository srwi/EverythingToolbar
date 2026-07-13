using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace EverythingToolbar.Settings
{
    public partial class Shortcuts
    {
        private Key Key { get; set; }
        private Key OriginalKey { get; set; }
        private ModifierKeys Modifiers { get; set; }
        private ModifierKeys OriginalModifiers { get; set; }
        private ModifierKeys TempMods { get; set; }

        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();
        private readonly StartMenuSearchInterceptor _startMenuInterceptor = Ioc.Default.GetRequiredService<StartMenuSearchInterceptor>();
        private readonly GlobalShortcutListener _shortcutListener = Ioc.Default.GetRequiredService<GlobalShortcutListener>();

        private static event EventHandler<WinKeyEventArgs>? WinKeyEventHandler;

        private static NativeMethods.LowLevelKeyboardProc? _llKeyboardHookCallback;
        private static IntPtr _llKeyboardHookId = IntPtr.Zero;

        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmSyskeydown = 0x0104;

        public Shortcuts()
        {
            InitializeComponent();
        }

        private void OnKeyPressedReleased(object? sender, WinKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Control : TempMods & ~ModifierKeys.Control;
                    break;
                case Key.LWin:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Windows : TempMods & ~ModifierKeys.Windows;
                    break;
                case Key.LeftAlt:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Alt : TempMods & ~ModifierKeys.Alt;
                    break;
                case Key.LeftShift:
                    TempMods = e.IsDown ? TempMods | ModifierKeys.Shift : TempMods & ~ModifierKeys.Shift;
                    break;
                default:
                    if (e.IsDown)
                    {
                        if (TempMods == ModifierKeys.None && e.Key == Key.Escape)
                        {
                            Key = Key.None;
                            Modifiers = ModifierKeys.None;
                        }
                        else
                        {
                            Key = e.Key;
                            Modifiers = TempMods;
                        }
                    }
                    break;
            }

            UpdateTextBox();
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
                return NativeMethods.CallNextHookEx(_llKeyboardHookId, nCode, wParam, lParam);

            var vkCode = (Keys)Marshal.ReadInt32(lParam);
            var isDown = (int)wParam == WmKeydown || (int)wParam == WmSyskeydown;
            switch (vkCode)
            {
                case Keys.Control:
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftCtrl));
                    break;
                case Keys.Shift:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftShift));
                    break;
                case Keys.Alt:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LeftAlt));
                    break;
                case Keys.LWin:
                case Keys.RWin:
                    WinKeyEventHandler?.Invoke(null, new WinKeyEventArgs(isDown, Key.LWin));
                    break;
                default:
                    WinKeyEventHandler?.Invoke(
                        null,
                        new WinKeyEventArgs(isDown, KeyInterop.KeyFromVirtualKey((int)vkCode))
                    );
                    break;
            }

            return 1;
        }

        private static void CaptureKeyboard(EventHandler<WinKeyEventArgs> callback)
        {
            ReleaseKeyboard();
            WinKeyEventHandler += callback;
            _llKeyboardHookCallback = KeyboardHookCallback;
            _llKeyboardHookId = NativeMethods.SetWindowsHookEx(WhKeyboardLl, _llKeyboardHookCallback, IntPtr.Zero, 0);
        }

        private static void ReleaseKeyboard()
        {
            WinKeyEventHandler = null;
            NativeMethods.UnhookWindowsHookEx(_llKeyboardHookId);
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CaptureKeyboard(OnKeyPressedReleased);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ReleaseKeyboard();
        }

        private void UpdateTextBox()
        {
            var shortcutText = new StringBuilder();
            if ((Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append(Properties.Resources.KeyCtrl);
            }
            if ((Modifiers & ModifierKeys.Windows) != 0)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Properties.Resources.KeyWin);
            }
            if ((Modifiers & ModifierKeys.Alt) != 0)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Properties.Resources.KeyAlt);
            }
            if ((Modifiers & ModifierKeys.Shift) != 0)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Properties.Resources.KeyShift);
            }
            if (Key != Key.None)
            {
                if (shortcutText.Length > 0)
                    shortcutText.Append('+');
                shortcutText.Append(Key.ToString());
            }

            ShortcutTextBox.Text = shortcutText.ToString();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _startMenuInterceptor.Disable();
            _shortcutListener.IsEnabled = false;

            Modifiers = (ModifierKeys)_settings.ShortcutModifiers;
            Key = (Key)_settings.ShortcutKey;

            OriginalKey = Key;
            OriginalModifiers = Modifiers;

            UpdateTextBox();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _shortcutListener.IsEnabled = true;
            ReleaseKeyboard();
            _startMenuInterceptor.Initialize();

            if (Key != OriginalKey || Modifiers != OriginalModifiers)
            {
                _shortcutListener.SetShortcut(Key, Modifiers);
            }
        }

        public class WinKeyEventArgs(bool isDown, Key key) : EventArgs
        {
            public bool IsDown { get; set; } = isDown;
            public Key Key { get; set; } = key;
        }
    }
}
