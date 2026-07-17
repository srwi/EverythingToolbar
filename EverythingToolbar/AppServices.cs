using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Core.Platform;
using EverythingToolbar.Search;
using EverythingToolbar.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EverythingToolbar
{
    public static class AppServices
    {
        private static bool IsInitialized;

        public static void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;

            var services = new ServiceCollection();
            services.AddSettings().AddPlatformAdapters().AddSearchEngine().AddShellServices().AddViewModels();

            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);

            provider
                .GetRequiredService<IEverythingClient>()
                .SetInstanceName(provider.GetRequiredService<ISettings>().InstanceName);
        }
    }

    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSettings(this IServiceCollection services)
        {
            return services.AddSingleton<ISettings>(_ => ToolbarSettings.User);
        }

        // The OS-abstraction seam: each interface is the app-facing contract, the adapter the Win32 impl.
        public static IServiceCollection AddPlatformAdapters(this IServiceCollection services)
        {
            return services
                .AddSingleton<IEverythingClient, EverythingIpcClient>()
                .AddSingleton<IClipboard, ClipboardAdapter>()
                .AddSingleton<IShellDialogs, ShellDialogsAdapter>()
                .AddSingleton<INotifier, NotifierAdapter>()
                .AddSingleton<IFileLauncher, FileLauncherAdapter>()
                .AddSingleton<IFilePreviewer, FilePreviewerAdapter>()
                .AddSingleton<IAutostart, AutostartAdapter>();
        }

        // Search domain: query state, sessions, filters, commands, and result actions.
        public static IServiceCollection AddSearchEngine(this IServiceCollection services)
        {
            return services
                .AddSingleton<SearchHistory>()
                .AddSingleton<IFilterNames, FilterNames>()
                .AddSingleton<DefaultFilterProvider>()
                .AddSingleton<EverythingFilterProvider>()
                .AddSingleton<FilterProvider>()
                .AddSingleton<SearchState>()
                .AddSingleton<SearchSession>()
                .AddSingleton<SearchCommands>()
                .AddSingleton<CustomActionService>()
                .AddSingleton<SearchResultActions>()
                .AddSingleton<EverythingSearchLauncher>();
        }

        // Windowing, theming, OS policy, and the global input hooks that drive the search window.
        public static IServiceCollection AddShellServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<TaskbarInfoProvider>()
                .AddSingleton<WindowsPolicy>()
                .AddSingleton<ThemeService>()
                .AddSingleton<StartMenuSearchInterceptor>()
                .AddSingleton<GlobalShortcutListener>()
                .AddSingleton<SearchWindow>()
                .AddSingleton<SearchWindowController>()
                .AddSingleton<ISearchWindowController>(sp => sp.GetRequiredService<SearchWindowController>())
                .AddSingleton<SearchHost>();
        }

        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            return services
                .AddTransient<SearchResultsViewModel>()
                .AddTransient<SearchResultPreviewPaneViewModel>()
                .AddTransient<SearchBoxViewModel>()
                .AddTransient<FilterSelectorViewModel>()
                .AddTransient<SettingsControlViewModel>()
                .AddTransient<SearchWindowViewModel>()
                .AddTransient<SearchButtonViewModel>()
                .AddTransient<ToolbarControlViewModel>();
        }
    }
}
