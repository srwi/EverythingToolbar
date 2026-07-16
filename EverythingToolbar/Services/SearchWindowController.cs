using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Services
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