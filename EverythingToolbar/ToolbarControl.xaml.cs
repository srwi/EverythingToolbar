using EverythingToolbar.Helpers;
using NHotkey;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace EverythingToolbar
{
    public partial class ToolbarControl : UserControl
    {
        public event EventHandler<EventArgs> FocusRequested;
        public event EventHandler<EventArgs> UnfocusRequested;

        public ToolbarControl()
        {
            InitializeComponent();

            ApplicationResources.Instance.ResourceChanged += (object sender, ResourcesChangedEventArgs e) =>
            {
                try
                {
                    Resources.MergedDictionaries.Add(e.NewResource);
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    ToolbarLogger.GetLogger("EverythingToolbar").Error(ex, "Failed to apply resource.");
                }
            };
            ApplicationResources.Instance.LoadDefaults();

            SearchBox.LostKeyboardFocus += (object sender, KeyboardFocusChangedEventArgs e) =>
            {
                Keyboard.Focus(KeyboardFocusCapture);
                UnfocusRequested?.Invoke(this, new EventArgs());
            };

            if (!ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
                   (Key)Properties.Settings.Default.shortcutKey,
                   (ModifierKeys)Properties.Settings.Default.shortcutModifiers,
                   FocusSearchBox))
            {
                ShortcutManager.Instance.SetShortcut(Key.None, ModifierKeys.None);
                MessageBox.Show(Properties.Resources.MessageBoxFailedToRegisterHotkey,
                    Properties.Resources.MessageBoxErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            ShortcutManager.Instance.SetFocusCallback(FocusSearchBox);
            if (Properties.Settings.Default.isReplaceStartMenuSearch)
                ShortcutManager.Instance.HookStartMenu();
        }

        public void Destroy()
        {
            Content = null;
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
            {
                Keyboard.Focus(SearchBox);
                EverythingSearch.Instance.SearchTerm = HistoryManager.Instance.GetPreviousItem();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                Keyboard.Focus(SearchBox);
                EverythingSearch.Instance.SearchTerm = HistoryManager.Instance.GetNextItem();
            }
            else if (e.Key == Key.Up)
            {
                SearchResultsPopup.SearchResultsView.SelectPreviousSearchResult();
            }
            else if (e.Key == Key.Down)
            {
                SearchResultsPopup.SearchResultsView.SelectNextSearchResult();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                string path = "";
                if (SearchResultsPopup.SearchResultsView.SearchResultsListView.SelectedIndex >= 0)
                    path = (SearchResultsPopup.SearchResultsView.SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
                EverythingSearch.Instance.OpenLastSearchInEverything(path);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                SearchResultsPopup.SearchResultsView.OpenFilePath(null, null);
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Enter)
            {
                SearchResultsPopup.SearchResultsView.RunAsAdmin(null, null);
            }
            else if (e.Key == Key.Enter)
            {
                SearchResultsPopup.SearchResultsView.OpenSelectedSearchResult();
            }
            else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                SearchResultsPopup.SearchResultsView.PreviewSelectedFile();
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                int index = e.Key == Key.D0 ? 9 : e.Key - Key.D1;
                EverythingSearch.Instance.SelectFilterFromIndex(index);
            }
            else if (e.Key == Key.Escape)
            {
                HistoryManager.Instance.AddToHistory(EverythingSearch.Instance.SearchTerm);
                EverythingSearch.Instance.SearchTerm = null;
                Keyboard.ClearFocus();
            }
            else if (e.Key == Key.PageUp)
            {
                SearchResultsPopup.SearchResultsView.PageUp();
            }
            else if (e.Key == Key.PageDown)
            {
                SearchResultsPopup.SearchResultsView.PageDown();
            }
            else if (e.Key == Key.Home)
            {
                SearchResultsPopup.SearchResultsView.ScrollToHome();
            }
            else if (e.Key == Key.End)
            {
                SearchResultsPopup.SearchResultsView.ScrollToEnd();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }

        private void OnKeyReleased(object sender, KeyEventArgs e)
        {
            if (!SearchResultsPopup.IsOpen)
                return;

            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    EverythingSearch.Instance.CycleFilters(-1);
                else
                    EverythingSearch.Instance.CycleFilters(1);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusRequested?.Invoke(this, new EventArgs());
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void FocusSearchBox(object sender, HotkeyEventArgs e)
        {
            if (SearchResultsPopup.IsOpen)
            {
                EverythingSearch.Instance.SearchTerm = null;
            }
            else
            {
                SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(this)).Handle);
                FocusRequested?.Invoke(this, new EventArgs());
                Keyboard.Focus(SearchBox);

                if (Properties.Settings.Default.isIconOnly)
                    EverythingSearch.Instance.SearchTerm = "";
            }
        }
    }
}
