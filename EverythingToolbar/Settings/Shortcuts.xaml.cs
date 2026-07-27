using System.Text;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace EverythingToolbar.Settings
{
    public partial class Shortcuts
    {
        private const int VkControl = 0x11;
        private const int VkLcontrol = 0xA2;
        private const int VkRcontrol = 0xA3;
        private const int VkShift = 0x10;
        private const int VkLshift = 0xA0;
        private const int VkRshift = 0xA1;
        private const int VkMenu = 0x12;
        private const int VkLmenu = 0xA4;
        private const int VkRmenu = 0xA5;
        private const int VkLwin = 0x5B;
        private const int VkRwin = 0x5C;

        private Key Key { get; set; }
        private Key OriginalKey { get; set; }
        private ModifierKeys Modifiers { get; set; }
        private ModifierKeys OriginalModifiers { get; set; }
        private ModifierKeys TempMods { get; set; }

        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();
        private readonly StartMenuSearchInterceptor _startMenuInterceptor =
            Ioc.Default.GetRequiredService<StartMenuSearchInterceptor>();
        private readonly GlobalShortcutListener _shortcutListener =
            Ioc.Default.GetRequiredService<GlobalShortcutListener>();

        private LowLevelKeyboardHook? _keyboardHook;

        public Shortcuts()
        {
            InitializeComponent();
        }

        private void OnKeyPressedReleased(int vk, bool isDown)
        {
            var key = MapVirtualKeyToKey(vk);
            switch (key)
            {
                case Key.LeftCtrl:
                    TempMods = isDown ? TempMods | ModifierKeys.Control : TempMods & ~ModifierKeys.Control;
                    break;
                case Key.LWin:
                    TempMods = isDown ? TempMods | ModifierKeys.Windows : TempMods & ~ModifierKeys.Windows;
                    break;
                case Key.LeftAlt:
                    TempMods = isDown ? TempMods | ModifierKeys.Alt : TempMods & ~ModifierKeys.Alt;
                    break;
                case Key.LeftShift:
                    TempMods = isDown ? TempMods | ModifierKeys.Shift : TempMods & ~ModifierKeys.Shift;
                    break;
                default:
                    if (isDown)
                    {
                        if (TempMods == ModifierKeys.None && key == Key.Escape)
                        {
                            Key = Key.None;
                            Modifiers = ModifierKeys.None;
                        }
                        else
                        {
                            Key = key;
                            Modifiers = TempMods;
                        }
                    }
                    break;
            }

            UpdateTextBox();
        }

        private static Key MapVirtualKeyToKey(int vk)
        {
            return vk switch
            {
                VkControl or VkLcontrol or VkRcontrol => Key.LeftCtrl,
                VkShift or VkLshift or VkRshift => Key.LeftShift,
                VkMenu or VkLmenu or VkRmenu => Key.LeftAlt,
                VkLwin or VkRwin => Key.LWin,
                _ => KeyInterop.KeyFromVirtualKey(vk),
            };
        }

        private bool OnKeyEvent(int vk, bool isDown, bool isInjected)
        {
            Dispatcher.BeginInvoke(() => OnKeyPressedReleased(vk, isDown));

            return true;
        }

        private void CaptureKeyboard()
        {
            ReleaseKeyboard();
            _keyboardHook = new LowLevelKeyboardHook(OnKeyEvent);
            _keyboardHook.Install();
        }

        private void ReleaseKeyboard()
        {
            _keyboardHook?.Uninstall();
            _keyboardHook = null;
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

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            CaptureKeyboard();
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ReleaseKeyboard();
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
    }
}
