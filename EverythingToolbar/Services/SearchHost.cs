using System.Windows;
using EverythingToolbar.Behaviors;
using Microsoft.Xaml.Behaviors;

namespace EverythingToolbar.Services
{
    public sealed class SearchHost
    {
        private readonly SearchWindowController _controller;
        private readonly GlobalShortcutListener _shortcutListener;
        private readonly StartMenuSearchInterceptor _startMenuInterceptor;
        private readonly SearchWindow _searchWindow;
        private readonly TaskbarInfoProvider _taskbarInfo;
        private readonly ISettings _settings;
        private readonly WindowsPolicy _windowsPolicy;

        private SearchWindowPlacement? _placement;

        public SearchHost(
            SearchWindowController controller,
            GlobalShortcutListener shortcutListener,
            StartMenuSearchInterceptor startMenuInterceptor,
            SearchWindow searchWindow,
            TaskbarInfoProvider taskbarInfo,
            ISettings settings,
            WindowsPolicy windowsPolicy)
        {
            _controller = controller;
            _shortcutListener = shortcutListener;
            _startMenuInterceptor = startMenuInterceptor;
            _searchWindow = searchWindow;
            _taskbarInfo = taskbarInfo;
            _settings = settings;
            _windowsPolicy = windowsPolicy;
        }

        public void Attach(FrameworkElement? placementTarget, bool iconMode)
        {
            _controller.SetIconMode(iconMode);
            _shortcutListener.Initialize(_controller.ToggleSearchUi);
            _startMenuInterceptor.Initialize();

            _placement = new SearchWindowPlacement(_taskbarInfo, _settings, _windowsPolicy)
            {
                PlacementTarget = placementTarget
            };
            Interaction.GetBehaviors(_searchWindow).Add(_placement);
        }

        public void SetPlacementTarget(FrameworkElement? target)
        {
            if (_placement != null)
                _placement.PlacementTarget = target;
        }

        public void Detach()
        {
            _startMenuInterceptor.Disable();
        }
    }
}
