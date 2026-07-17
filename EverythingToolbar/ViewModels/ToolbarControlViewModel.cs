using System;
using CommunityToolkit.Mvvm.Messaging;

namespace EverythingToolbar.ViewModels
{
    public sealed class ToolbarControlViewModel
    {
        private readonly SearchWindowController _controller;

        public ToolbarControlViewModel(SearchWindowController controller)
        {
            _controller = controller;
        }

        // Forwarded so the control depends only on this view-model, not on SearchWindowController directly.
        public event EventHandler Hiding
        {
            add => _controller.Hiding += value;
            remove => _controller.Hiding -= value;
        }

        public void RegisterSearchBox(Func<bool> isFocused, Action focus) =>
            _controller.RegisterToolbarSearchBox(isFocused, focus);

        public void UnregisterSearchBox(Action focus) =>
            _controller.UnregisterToolbarSearchBox(focus);

        public void ShowSearchWindow() => _controller.Show();

        // The genuinely cross-assembly deskband focus signal (the one surviving message).
        public void NotifyToolbarFocusChanged(bool isFocused) =>
            WeakReferenceMessenger.Default.Send(new ToolbarFocusChanged(isFocused));
    }
}
