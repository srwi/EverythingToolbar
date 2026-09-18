using System;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingToolbar.App.Search
{
    public interface ISearchResultDispatcher
    {
        // Queue result application on the UI thread after pending input, without a timer.
        // Never execute inline: callers must have time to subscribe and may cancel before dispatch.
        Task InvokeAsync(Action action, CancellationToken cancellationToken);
    }
}
