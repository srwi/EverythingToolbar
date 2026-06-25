using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using EverythingToolbar.Platform;
using EverythingToolbar.Search;
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
            services.AddSingleton<HistoryManager>();
            services.AddSingleton<TaskbarStateManager>();
            services.AddSingleton<DefaultFilterLoader>();
            services.AddSingleton<EverythingFilterLoader>();
            services.AddSingleton<FilterLoader>();
            services.AddSingleton<SearchState>();
            services.AddSingleton<EverythingOptions>();
            services.AddSingleton<PlacementOptions>();
            services.AddSingleton<FilterOptions>();
            services.AddSingleton<MatchOptions>();
            services.AddSingleton<SortOptions>();
            services.AddSingleton<SearchOptions>();
            services.AddSingleton<ThemeOptions>();
            services.AddSingleton<StartMenuOptions>();
            services.AddSingleton<UpdateOptions>();
            services.AddSingleton<TaskbarWindowOptions>();
            services.AddSingleton<CustomActionsOptions>();
            services.AddSingleton<ShortcutOptions>();
            services.AddSingleton<LanguageOptions>();
            services.AddSingleton<LauncherOptions>();
            services.AddSingleton<IconOptions>();
            services.AddSingleton<WindowsPolicy>();
            services.AddSingleton<StartMenuIntegration>();
            services.AddSingleton<SearchWindow>();
            services.AddSingleton<ISearchWindowController>(sp => sp.GetRequiredService<SearchWindow>());
            services.AddSingleton<IEverythingClient, EverythingIpcClient>();
            services.AddSingleton<IClipboard, ClipboardAdapter>();
            services.AddSingleton<IShellDialogs, ShellDialogsAdapter>();
            services.AddSingleton<INotifier, NotifierAdapter>();
            services.AddSingleton<IFileLauncher, FileLauncherAdapter>();
            services.AddSingleton<SearchResultActions>();
            services.AddSingleton<EverythingSearchLauncher>();

            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);

            provider.GetRequiredService<IEverythingClient>().SetInstanceName(provider.GetRequiredService<EverythingOptions>().InstanceName);
        }
    }
}