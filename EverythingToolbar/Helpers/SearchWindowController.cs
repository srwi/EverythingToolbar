using CommunityToolkit.Mvvm.DependencyInjection;

namespace EverythingToolbar.Helpers
{
    public sealed class SearchWindowController : ISearchWindowController
    {
        private SearchWindow? _window;

        private SearchWindow Window => _window ??= Ioc.Default.GetRequiredService<SearchWindow>();

        public void Show() => Window.Show();

        public void Hide() => Window.Hide();

        public void Toggle() => Window.Toggle();
    }
}