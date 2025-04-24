using System;
using System.Windows.Input;

namespace EverythingToolbar.Helpers
{
    public class EventDispatcher
    {
        public static readonly EventDispatcher Instance = new EventDispatcher();

        public event EventHandler<EventArgs> FocusRequested;
        public void InvokeFocusRequested(object sender, EventArgs e)
        {
            FocusRequested?.Invoke(sender, e);
        }

        public event EventHandler<EventArgs> UnfocusRequested;
        public void InvokeUnfocusRequested(object sender, EventArgs e)
        {
            UnfocusRequested?.Invoke(sender, e);
        }

        public event EventHandler<EventArgs> SearchBoxFocusRequested;
        public void InvokeSearchBoxFocused(object sender, EventArgs e)
        {
            SearchBoxFocusRequested?.Invoke(sender, e);
        }

        public event EventHandler<KeyEventArgs> GlobalKeyEvent;
        public void InvokeGlobalKeyEvent(object sender, KeyEventArgs e)
        {
            GlobalKeyEvent?.Invoke(sender, e);
        }

        public event EventHandler<string> SearchTermReplaced;
        public void InvokeSearchTermReplaced(object sender, string newSearchTerm)
        {
            SearchTermReplaced?.Invoke(sender, newSearchTerm);
        }
    }
}