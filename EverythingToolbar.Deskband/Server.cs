using System;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
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
        private static ToolbarControl? ToolbarControl;
        private TaskbarInfoProvider _taskbarState = null!;
        private SearchWindowController _controller = null!;
        protected override UIElement UIElement => ToolbarControl!;

        static Server()
        {
            // The deskband is COM-hosted by explorer, so its assemblies load in an isolated
            // AssemblyLoadContext backed by this component's deps.json. WPF's BAML loader, however,
            // resolves assemblies via Assembly.Load on the *default* context, which knows nothing of
            // that deps.json — so assemblies referenced only from XAML (e.g. Microsoft.Xaml.Behaviors)
            // fail to load. Bridge the default context to this component's own context/resolver, so a
            // single shared instance is returned (avoiding a duplicate assembly identity).
            var componentContext =
                AssemblyLoadContext.GetLoadContext(typeof(Server).Assembly) ?? AssemblyLoadContext.Default;
            var resolver = new AssemblyDependencyResolver(typeof(Server).Assembly.Location);
            AssemblyLoadContext.Default.Resolving += (_, name) =>
            {
                var path = resolver.ResolveAssemblyToPath(name);
                return path != null ? componentContext.LoadFromAssemblyPath(path) : null;
            };
        }

        public Server()
        {
            try
            {
                EnsureDispatcherSynchronizationContext();

                AppServices.Initialize();

                _taskbarState = Ioc.Default.GetRequiredService<TaskbarInfoProvider>();

                // Apply saved UI language
                CultureHelper.ApplyUILanguage(Ioc.Default.GetRequiredService<ISettings>().UILanguage);

                ToolbarControl = new ToolbarControl();

                Options.MinHorizontalSize = new Size(24, 30);
                Options.MinVerticalSize = new Size(24, 30);

                WeakReferenceMessenger.Default.Register<ToolbarFocusChanged>(this, (_, m) => UpdateFocus(m.IsFocused));
                _controller = Ioc.Default.GetRequiredService<SearchWindowController>();
                _controller.ActiveChanged += OnSearchWindowActiveChanged;
                TaskbarInfo.TaskbarEdgeChanged += OnTaskbarEdgeChanged;
                TaskbarInfo.TaskbarSizeChanged += OnTaskbarSizeChanged;

                _taskbarState.TaskbarEdge = (Services.Edge)TaskbarInfo.Edge;
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

        // Explorer owns this thread's message loop, so WPF installs no synchronization context of its
        // own and awaits can resume off the UI thread, losing search results (issue #762).
        private static void EnsureDispatcherSynchronizationContext()
        {
            if (SynchronizationContext.Current is DispatcherSynchronizationContext)
                return;

            Logger.Debug(
                "No dispatcher synchronization context on the deskband UI thread (found: {context}); installing one.",
                SynchronizationContext.Current?.GetType().Name ?? "none"
            );
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher)
            );
        }

        private void OnSearchWindowActiveChanged(object? sender, bool isActive)
        {
            if (isActive)
                UpdateFocus(true);
        }

        private void OnTaskbarEdgeChanged(object? sender, TaskbarEdgeChangedEventArgs e)
        {
            _taskbarState.TaskbarEdge = (Services.Edge)e.Edge;
        }

        private void OnTaskbarSizeChanged(object? sender, TaskbarSizeChangedEventArgs e)
        {
            _taskbarState.TaskbarSize = new Size(e.Size.Width, e.Size.Height);
        }

        protected override void DeskbandOnClosed()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            _controller.ActiveChanged -= OnSearchWindowActiveChanged;
            Ioc.Default.GetRequiredService<SearchHost>().Detach();

            base.DeskbandOnClosed();

            if (ToolbarControl != null)
            {
                ToolbarControl.Content = null;
                ToolbarControl = null;
            }
        }
    }
}
