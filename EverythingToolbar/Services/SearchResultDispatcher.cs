using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EverythingToolbar.Services
{
    public sealed class SearchResultDispatcher : ISearchResultDispatcher
    {
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        // Async query continuations otherwise resume at Normal, ahead of Input. Query submission
        // remains immediate; only the UI mutation (Reset/Replace and resulting bindings) yields.
        public Task InvokeAsync(Action action, CancellationToken cancellationToken) =>
            _dispatcher.InvokeAsync(action, DispatcherPriority.Background, cancellationToken).Task;
    }
}
