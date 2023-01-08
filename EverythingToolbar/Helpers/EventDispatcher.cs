using System;
using System.Windows.Input;

namespace EverythingToolbar.Helpers
{
    public class EventDispatcher
    {
        public static readonly EventDispatcher Instance = new EventDispatcher();

        // Events

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

        public event EventHandler<KeyEventArgs> KeyPressed;
        public void InvokeKeyPressed(object sender, KeyEventArgs e)
        {
            KeyPressed?.Invoke(sender, e);
        }
    }
}
