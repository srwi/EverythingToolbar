﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using EverythingToolbar;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NLog;

namespace CSDeskBand
{
    [ComVisible(true)]
    [Guid("9d39b79c-e03c-4757-b1b6-ecce843748f3")]
    [CSDeskBandRegistration(Name = "EverythingToolbar")]
    public class Deskband : CSDeskBandWpf
    {
        private static readonly ILogger _logger = ToolbarLogger.GetLogger<Deskband>();
        private static ToolbarControl ToolbarControl;
        protected override UIElement UIElement => ToolbarControl;

        public Deskband()
        {
            try
            {
                ToolbarControl = new ToolbarControl();

                Options.MinHorizontalSize = new Size(18, 30);
                Options.MinVerticalSize = new Size(30, 40);

                EventDispatcher.Instance.FocusRequested += OnFocusRequested;
                EventDispatcher.Instance.UnfocusRequested += OnUnfocusRequested;
                TaskbarInfo.TaskbarEdgeChanged += OnTaskbarEdgeChanged;

                TaskbarStateManager.Instance.TaskbarEdge = (EverythingToolbar.Helpers.Edge)TaskbarInfo.Edge;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unhandled exception");
                if (MessageBox.Show(e.ToString() + "\n\n" + Resources.MessageBoxCopyException,
                    Resources.MessageBoxUnhandledExceptionTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    Clipboard.SetText(e.ToString());
                }
            }
        }

        private void OnUnfocusRequested(object sender, EventArgs e)
        {
            UpdateFocus(false);
        }

        private void OnFocusRequested(object sender, EventArgs e)
		{
            UpdateFocus(true);
		}

		private void OnTaskbarEdgeChanged(object sender, TaskbarEdgeChangedEventArgs e)
        {
            TaskbarStateManager.Instance.TaskbarEdge = (EverythingToolbar.Helpers.Edge)e.Edge;
        }

        protected override void DeskbandOnClosed()
        {
            base.DeskbandOnClosed();
            ToolbarControl.Content = null;
            ToolbarControl = null;
        }
    }
}
