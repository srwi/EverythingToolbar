using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Core.Platform;
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
            services.AddSingleton<SearchHistoryService>();
            services.AddSingleton<TaskbarStateService>();
            services.AddSingleton<IFilterNames, FilterNames>();
            services.AddSingleton<DefaultFilterService>();
            services.AddSingleton<EverythingFilterService>();
            services.AddSingleton<FilterService>();
            services.AddSingleton<SearchState>();
            services.AddSingleton<SearchSession>();
            services.AddSingleton<SearchCommands>();
            services.AddSingleton<CustomActionService>();
            services.AddSingleton<WindowsPolicyService>();
            services.AddSingleton<ThemeService>();
            services.AddSingleton<StartMenuService>();
            services.AddSingleton<SearchWindow>();
            services.AddSingleton<SearchWindowController>();
            services.AddSingleton<ISearchWindowController>(sp => sp.GetRequiredService<SearchWindowController>());
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
            services.AddTransient<SearchResultPreviewPaneViewModel>();
            services.AddTransient<SearchBoxViewModel>();
            services.AddTransient<FilterSelectorViewModel>();
            services.AddTransient<SettingsControlViewModel>();
            services.AddTransient<SearchWindowViewModel>();
            services.AddTransient<SearchButtonViewModel>();
            services.AddTransient<ToolbarControlViewModel>();

            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);

            provider.GetRequiredService<IEverythingClient>().SetInstanceName(provider.GetRequiredService<ISettings>().InstanceName);
        }
    }
}