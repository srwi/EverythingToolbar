using System;
using System.Runtime.InteropServices;
using System.Windows;
using EverythingToolbar;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NLog;

namespace EverythingToolbar.Deskband
{
    [ComVisible(true)]
    [Guid("c51ca15b-2073-4239-a12b-468c7b62563e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IServer
    {
        void Dummy(); // Dummy method to allow COM registration
    }

    [ComVisible(true)]
    [Guid("9d39b79c-e03c-4757-b1b6-ecce843748f3")]
    [CSDeskBandRegistration(Name = "EverythingToolbar")]
    public class Server : CSDeskBandWpf, IServer
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<Server>();
        private static ToolbarControl? _toolbarControl;
        protected override UIElement UIElement => _toolbarControl!;

        public Server()
        {
            try
            {
                // Apply saved UI language
                CultureHelper.ApplyUILanguage(ToolbarSettings.User.UILanguage);

                _toolbarControl = new ToolbarControl();

                Options.MinHorizontalSize = new Size(24, 30);
                Options.MinVerticalSize = new Size(24, 30);

                EventDispatcher.Instance.FocusRequested += OnFocusRequested;
                EventDispatcher.Instance.UnfocusRequested += OnUnfocusRequested;
                TaskbarInfo.TaskbarEdgeChanged += OnTaskbarEdgeChanged;
                TaskbarInfo.TaskbarSizeChanged += OnTaskbarSizeChanged;

                TaskbarStateManager.Instance.TaskbarEdge = (Helpers.Edge)TaskbarInfo.Edge;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unhandled exception");
                if (
                    MessageBox.Show(
                        e + "\n\n" + Resources.MessageBoxCopyException,
                        Resources.MessageBoxUnhandledExceptionTitle,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error
                    ) == MessageBoxResult.Yes
                )
                {
                    Clipboard.SetText(e.ToString());
                }
            }
        }

        public void Dummy() { }

        private void OnUnfocusRequested(object? sender, EventArgs e)
        {
            UpdateFocus(false);
        }

        private void OnFocusRequested(object? sender, EventArgs e)
        {
            UpdateFocus(true);
        }

        private void OnTaskbarEdgeChanged(object? sender, TaskbarEdgeChangedEventArgs e)
        {
            TaskbarStateManager.Instance.TaskbarEdge = (Helpers.Edge)e.Edge;
        }

        private void OnTaskbarSizeChanged(object? sender, TaskbarSizeChangedEventArgs e)
        {
            TaskbarStateManager.Instance.TaskbarSize = new Size(e.Size.Width, e.Size.Height);
        }

        protected override void DeskbandOnClosed()
        {
            StartMenuIntegration.Instance.Disable();

            base.DeskbandOnClosed();

            if (_toolbarControl != null)
            {
                _toolbarControl.Content = null;
                _toolbarControl = null;
            }
        }
    }
}
