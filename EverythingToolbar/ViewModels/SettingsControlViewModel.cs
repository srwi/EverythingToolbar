
namespace EverythingToolbar.ViewModels
{
    public sealed class SettingsControlViewModel
    {
        public ISearchWindowController SearchWindowController { get; }
        public ISettings Settings { get; }
        public IEverythingClient EverythingClient { get; }

        public SettingsControlViewModel(
            ISearchWindowController searchWindowController,
            ISettings settings,
            IEverythingClient everythingClient)
        {
            SearchWindowController = searchWindowController;
            Settings = settings;
            EverythingClient = everythingClient;
        }
    }
}
