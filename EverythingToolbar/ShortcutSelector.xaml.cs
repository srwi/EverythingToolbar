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

        public ShortcutSelector()
        {
            InitializeComponent();

            HotkeyManager.Current.IsEnabled = false;

            Modifiers = (ModifierKeys)Properties.Settings.Default.shortcutModifiers;
            Key = (Key)Properties.Settings.Default.shortcutKey;
            UpdateTextBox();
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            Modifiers = Keyboard.Modifiers;
            Key = key;

            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                Modifiers |= ModifierKeys.Windows;

            UpdateTextBox();
        }

        private void UpdateTextBox()
        {
            StringBuilder shortcutText = new StringBuilder();
            if ((Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append(Properties.Resources.KeyCtrl + "+");
            }
            if ((Modifiers & ModifierKeys.Windows) != 0)
            {
                shortcutText.Append(Properties.Resources.KeyWin + "+");
            }
            if ((Modifiers & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append(Properties.Resources.KeyAlt + "+");
            }
            if ((Modifiers & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append(Properties.Resources.KeyShift + "+");
            }
            shortcutText.Append(Key.ToString());

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
        }
    }
}
