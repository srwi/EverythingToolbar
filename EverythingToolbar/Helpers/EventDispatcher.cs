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
        public static readonly EventDispatcher Instance = new EventDispatcher();

        public event EventHandler<EventArgs> FocusRequested;
        public void DispatchFocusRequested(object sender, EventArgs e)
        {
            FocusRequested?.Invoke(sender, e);
        }

        public event EventHandler<EventArgs> UnfocusRequested;
        public void DispatchUnfocusRequested(object sender, EventArgs e)
        {
            UnfocusRequested?.Invoke(sender, e);
        }

        public event EventHandler<KeyEventArgs> KeyPressed;
        public void DispatchKeyPressed(object sender, KeyEventArgs e)
        {
            KeyPressed?.Invoke(sender, e);
        }

        public event EventHandler<KeyEventArgs> WindowShowRequested;
        public void DispatchWindowShowRequested(object sender, KeyEventArgs e)
        {
            WindowShowRequested?.Invoke(sender, e);
        }

        public event EventHandler<KeyEventArgs> WindowHideRequested;
        public void DispatchWindowHideRequested(object sender, KeyEventArgs e)
        {
            WindowHideRequested?.Invoke(sender, e);
        }
    }
}
