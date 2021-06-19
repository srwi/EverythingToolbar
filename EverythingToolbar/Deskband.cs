using EverythingToolbar;
using EverythingToolbar.Properties;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace CSDeskBand
{
    [ComVisible(true)]
    [Guid("9d39b79c-e03c-4757-b1b6-ecce843748f3")]
    [CSDeskBandRegistration(Name = "Everything Toolbar")]
    public class Deskband : CSDeskBandWpf
    {
        private static ToolbarControl toolbarControl;
        protected override UIElement UIElement => toolbarControl;

        public Deskband()
        {
            try
            {
                if (Settings.Default.isUpgradeRequired)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.isUpgradeRequired = false;
                    Settings.Default.Save();
                }

                toolbarControl = new ToolbarControl();

                Options.MinHorizontalSize = new Size(18, 30);
                Options.MinVerticalSize = new Size(30, 40);

                toolbarControl.FocusRequested += OnFocusRequested;
                toolbarControl.UnfocusRequested += OnUnfocusRequested;
                TaskbarInfo.TaskbarEdgeChanged += OnTaskbarEdgeChanged;
                TaskbarInfo.TaskbarSizeChanged += OnTaskbarSizeChanged;

                SearchResultsPopup.taskbarEdge = TaskbarInfo.Edge;
            }
            catch (Exception e)
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Unhandled exception");
                if (MessageBox.Show(e.ToString() + "\n\n" + EverythingToolbar.Properties.Resources.MessageBoxCopyException,
                    EverythingToolbar.Properties.Resources.MessageBoxUnhandledExceptionTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    Clipboard.SetText(e.ToString());
                }
            }
        }

        private void OnTaskbarSizeChanged(object sender, TaskbarSizeChangedEventArgs e)
        {
            if (TaskbarInfo.Edge == Edge.Left || TaskbarInfo.Edge == Edge.Right)
            {
                SearchResultsPopup.taskbarWidth = TaskbarInfo.Size.Width;
                SearchResultsPopup.taskbarHeight = 0;
            }

            if (TaskbarInfo.Edge == Edge.Top || TaskbarInfo.Edge == Edge.Bottom)
            {
                SearchResultsPopup.taskbarHeight = TaskbarInfo.Size.Height;
                SearchResultsPopup.taskbarWidth = 0;
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
            SearchResultsPopup.taskbarEdge = e.Edge;
            OnTaskbarSizeChanged(sender, null);
        }

        protected override void DeskbandOnClosed()
        {
            base.DeskbandOnClosed();
            toolbarControl.Destroy();
            toolbarControl = null;
        }
    }
}
