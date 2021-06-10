using EverythingToolbar.Helpers;
using NHotkey.Wpf;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class ShortcutSelector : Window
    {
        public ModifierKeys Modifiers { get; private set; }
        public Key Key { get; private set; }
        private bool isWinKeyDown = false;

        public ShortcutSelector()
        {
            InitializeComponent();

            HotkeyManager.Current.IsEnabled = false;

            Modifiers = (ModifierKeys)Properties.Settings.Default.shortcutModifiers;
            Key = (Key)Properties.Settings.Default.shortcutKey;
            UpdateTextBox();

            ShortcutManager.Instance.LockWindowsKey(OnWinKeyPressedReleased);
        }

        private void OnWinKeyPressedReleased(object sender, ShortcutManager.WinKeyEventArgs e)
        {
            isWinKeyDown = e.IsDown;

            if (isWinKeyDown)
            {
                if (Keyboard.Modifiers != ModifierKeys.None)
                    Modifiers |= ModifierKeys.Windows;
                else
                    Modifiers = ModifierKeys.Windows;
                Key = Key.None;
            }
            else
            {
                UpdateTextBox();
            }
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt)
            {
                return;
            }

            Modifiers = Keyboard.Modifiers | (isWinKeyDown ? ModifierKeys.Windows : ModifierKeys.None);
            Key = key;

            UpdateTextBox();
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
            ShortcutManager.Instance.ReleaseWindowsKey();
        }
    }
}
