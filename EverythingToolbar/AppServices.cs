using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using EverythingToolbar.Platform;
using EverythingToolbar.Search;
using EverythingToolbar.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EverythingToolbar
{
    public static class AppServices
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) // idempotent: guards double-init / repeat COM activation
                return;
            _initialized = true;

            var services = new ServiceCollection();
            services.AddSingleton<ISettings>(_ => ToolbarSettings.User);
            services.AddSingleton<HistoryManager>();
            services.AddSingleton<TaskbarStateManager>();
            services.AddSingleton<IFilterNames, FilterNames>();
            services.AddSingleton<DefaultFilterLoader>();
            services.AddSingleton<EverythingFilterLoader>();
            services.AddSingleton<FilterLoader>();
            services.AddSingleton<SearchState>();
            services.AddSingleton<SearchSession>();
            services.AddSingleton<SearchCommands>();
            services.AddSingleton<CustomActionService>();
            services.AddSingleton<WindowsPolicy>();
            services.AddSingleton<ThemeService>();
            services.AddSingleton<StartMenuIntegration>();
            services.AddSingleton<SearchWindow>();
            services.AddSingleton<ISearchWindowController, SearchWindowController>();
            services.AddSingleton<IEverythingClient, EverythingIpcClient>();
            services.AddSingleton<IClipboard, ClipboardAdapter>();
            services.AddSingleton<IShellDialogs, ShellDialogsAdapter>();
            services.AddSingleton<INotifier, NotifierAdapter>();
            services.AddSingleton<IFileLauncher, FileLauncherAdapter>();
            services.AddSingleton<IFilePreviewer, FilePreviewerAdapter>();
            services.AddSingleton<IAutostartService, AutostartService>();
            services.AddSingleton<SearchResultActions>();
            services.AddSingleton<EverythingSearchLauncher>();
            services.AddTransient<SearchResultsViewModel>();
            services.AddTransient<SearchBoxViewModel>();
            services.AddTransient<FilterSelectorViewModel>();
            services.AddTransient<SettingsControlViewModel>();
            services.AddTransient<SearchWindowViewModel>();

            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);

            provider.GetRequiredService<IEverythingClient>().SetInstanceName(provider.GetRequiredService<ISettings>().InstanceName);
        }
    }
}