using EverythingToolbar.Helpers;
using NHotkey.Wpf;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class ShortcutSelector : Window
    {
        public Key Key { get; private set; }
        public ModifierKeys Modifiers { get; private set; }
        private ModifierKeys TempMods { get; set; }

        public ShortcutSelector()
        {
            InitializeComponent();

            ShortcutManager.Instance.UnhookStartMenu();
            HotkeyManager.Current.IsEnabled = false;

            Modifiers = (ModifierKeys)Properties.Settings.Default.shortcutModifiers;
            Key = (Key)Properties.Settings.Default.shortcutKey;
            UpdateTextBox();
        }

        private void OnKeyPressedReleased(object sender, ShortcutManager.WinKeyEventArgs e)
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
                        Key = e.Key;
                        Modifiers = TempMods;
                    }
                    break;
            }

            UpdateTextBox();
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ShortcutManager.Instance.CaptureKeyboard(OnKeyPressedReleased);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ShortcutManager.Instance.ReleaseKeyboard();
        }

        private void UpdateTextBox()
        {
            StringBuilder shortcutText = new StringBuilder();
            if ((Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append(Properties.Resources.KeyCtrl);
            }
            if ((Modifiers & ModifierKeys.Windows) != 0)
            {
                shortcutText.Append(shortcutText.Length > 0 ? "+" : "");
                shortcutText.Append(Properties.Resources.KeyWin);
            }
            if ((Modifiers & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append(shortcutText.Length > 0 ? "+" : "");
                shortcutText.Append(Properties.Resources.KeyAlt);
            }
            if ((Modifiers & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append(shortcutText.Length > 0 ? "+" : "");
                shortcutText.Append(Properties.Resources.KeyShift);
            }
            if (Key != Key.None)
            {
                shortcutText.Append(shortcutText.Length > 0 ? "+" : "");
                shortcutText.Append(Key.ToString());
            }

            ShortcutTextBox.Text = shortcutText.ToString();
        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnClosed(object sender, System.EventArgs e)
        {
            HotkeyManager.Current.IsEnabled = true;
            ShortcutManager.Instance.ReleaseKeyboard();
            if (Properties.Settings.Default.isReplaceStartMenuSearch)
            {
                ShortcutManager.Instance.HookStartMenu();
            }
        }
    }
}
