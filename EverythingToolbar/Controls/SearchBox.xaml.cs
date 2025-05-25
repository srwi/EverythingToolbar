﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Controls
{
    public partial class SearchBox
    {
        public SearchBox()
        {
            InitializeComponent();

            DataContext = EverythingSearch.Instance;
            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            // IsEnabled property of matchWholeWord button needs to be handled in code
            // because DataTriggers are not compatible with DynamicResources as MenuItem styles
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
            EverythingSearch.Instance.PropertyChanged += OnSettingsChanged;

            EventDispatcher.Instance.SearchTermReplaced += (s, searchTerm) => { UpdateSearchTerm(searchTerm); };
            EventDispatcher.Instance.SearchBoxFocusRequested += OnFocusRequested;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ToolbarSettings.User.IsSearchAsYouType)
            {
                EverythingSearch.Instance.SearchTerm = TextBox.Text;
            }
        }

        private void OnFocusRequested(object sender, EventArgs e)
        {
            // Only visible SearchBoxes should respond to focus requests
            if (Visibility == Visibility.Visible)
                Focus();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
            {
                UpdateSearchTerm(HistoryManager.Instance.GetPreviousItem());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                UpdateSearchTerm(HistoryManager.Instance.GetNextItem());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter && !ToolbarSettings.User.IsSearchAsYouType)
            {
                EverythingSearch.Instance.SearchTerm = TextBox.Text;
                e.Handled = true;
            }
            else if ((e.Key == Key.Home || e.Key == Key.End) && Keyboard.Modifiers != ModifierKeys.Shift && ToolbarSettings.User.IsAutoSelectFirstResult ||
                e.Key == Key.PageDown || e.Key == Key.PageUp ||
                e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Escape || e.Key == Key.Enter ||
                (((e.Key >= Key.D0 && e.Key <= Key.D9) || 
                  e.Key == Key.I ||
                  e.Key == Key.B ||
                  e.Key == Key.U ||
                  e.Key == Key.R
                 ) && Keyboard.Modifiers == ModifierKeys.Control))
            {
                EventDispatcher.Instance.InvokeGlobalKeyEvent(this, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                // The down stroke of the Tab key is not always consistent. Therefore it's handled by the up stroke event.
                e.Handled = true;
            }
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                var offset = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
                EverythingSearch.Instance.CycleFilters(offset);
                e.Handled = true;
            }
        }

        private void UpdateSearchTerm(string newSearchTerm)
        {
            TextBox.Text = newSearchTerm;
            TextBox.CaretIndex = TextBox.Text.Length;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToolbarSettings.User.IsShowQuickToggles))
                UpdateQuickTogglesVisibility();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateQuickTogglesVisibility();
        }

        private void UpdateQuickTogglesVisibility()
        {
            if (ToolbarSettings.User.IsShowQuickToggles && ActualWidth > 200)
            {
                QuickToggleButtons.Visibility = Visibility.Visible;
                TextBox.Padding = new Thickness(37, 0, 130, 0);
            }
            else
            {
                QuickToggleButtons.Visibility = Visibility.Collapsed;
                TextBox.Padding = new Thickness(37, 0, 10, 0);
            }
        }

        public new void Focus()
        {
            if (PresentationSource.FromVisual(TextBox) is HwndSource hwnd)
            {
                NativeMethods.ForciblySetForegroundWindow(hwnd.Handle);
            }

            TextBox.Focus();
            Keyboard.Focus(TextBox);
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox.SelectAll();
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null)  // New focus outside application
            {
                SearchWindow.Instance.Hide();
            }
        }

        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
            }
        }

        private void OnPasteClicked(object sender, RoutedEventArgs args) { TextBox.Paste(); }
        private void OnCopyClicked(object sender, RoutedEventArgs args) { TextBox.Copy(); }
        private void OnCutClicked(object sender, RoutedEventArgs args) { TextBox.Cut(); }
    }
}
