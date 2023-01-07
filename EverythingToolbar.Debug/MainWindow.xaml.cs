using EverythingToolbar.Helpers;
using System.Windows;
using System.Windows.Input;

namespace EverythingToolbar.Debug
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ToolbarLogger.Initialize();

            if (!ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
                                                       (Key)EverythingToolbar.Properties.Settings.Default.shortcutKey,
                                                       (ModifierKeys)EverythingToolbar.Properties.Settings.Default.shortcutModifiers,
                                                       SearchWindow.Instance.Show))
            {
                ShortcutManager.Instance.SetShortcut(Key.None, ModifierKeys.None);
                MessageBox.Show(EverythingToolbar.Properties.Resources.MessageBoxFailedToRegisterHotkey,
                    EverythingToolbar.Properties.Resources.MessageBoxErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            ResourceManager.Instance.ResourceChanged += (sender, e) => { Resources = e.NewResource; };
            ResourceManager.Instance.AutoApplyTheme();
        }
    }
}
