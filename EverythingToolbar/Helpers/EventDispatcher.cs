using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EverythingToolbar.Helpers
{
    public class EventDispatcher
    {
        // Events

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

        public event EventHandler<KeyEventArgs> KeyPressed;
        public void InvokeKeyPressed(object sender, KeyEventArgs e)
        {
            KeyPressed?.Invoke(sender, e);
        }

        // Delegates

        public delegate void ShowWindowDelegate();
        public event ShowWindowDelegate ShowWindow;
        public void InvokeShowWindow()
        {
            ShowWindow?.Invoke();
        }

        public delegate void HideWindowDelegate();
        public event HideWindowDelegate HideWindow;
        public void InvokeHideWindow()
        {
            HideWindow?.Invoke();
        }
    }
}
