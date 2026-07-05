using System;
using System.Runtime.InteropServices;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
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
        private TaskbarStateManager _taskbarState = null!;
        protected override UIElement UIElement => _toolbarControl!;

        public Server()
        {
            try
            {
                AppServices.Initialize();

                _taskbarState = Ioc.Default.GetRequiredService<TaskbarStateManager>();

                // Apply saved UI language
                CultureHelper.ApplyUILanguage(Ioc.Default.GetRequiredService<ISettings>().UILanguage);

                _toolbarControl = new ToolbarControl();

                Options.MinHorizontalSize = new Size(24, 30);
                Options.MinVerticalSize = new Size(24, 30);

                WeakReferenceMessenger.Default.Register<ToolbarFocusChanged>(this, (_, m) => UpdateFocus(m.IsFocused));
                WeakReferenceMessenger.Default.Register<SearchWindowActiveChanged>(this, (_, m) =>
                {
                    if (m.IsActive)
                        UpdateFocus(true);
                });
                TaskbarInfo.TaskbarEdgeChanged += OnTaskbarEdgeChanged;
                TaskbarInfo.TaskbarSizeChanged += OnTaskbarSizeChanged;

                _taskbarState.TaskbarEdge = (Helpers.Edge)TaskbarInfo.Edge;
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

        private void OnTaskbarEdgeChanged(object? sender, TaskbarEdgeChangedEventArgs e)
        {
            _taskbarState.TaskbarEdge = (Helpers.Edge)e.Edge;
        }

        private void OnTaskbarSizeChanged(object? sender, TaskbarSizeChangedEventArgs e)
        {
            _taskbarState.TaskbarSize = new Size(e.Size.Width, e.Size.Height);
        }

        protected override void DeskbandOnClosed()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            Ioc.Default.GetRequiredService<StartMenuIntegration>().Disable();

            base.DeskbandOnClosed();

            if (_toolbarControl != null)
            {
                _toolbarControl.Content = null;
                _toolbarControl = null;
            }
        }
    }
}