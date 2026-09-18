using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EverythingToolbar.Core.Search;
using EverythingToolbar.Services;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        if (args.Contains("--legacy-control"))
            return Run(
                "legacy immediate result application must fail input-priority assertion",
                () => InputWins(new ImmediateDispatcher())
            );

        var failures = 0;
        failures += Run(
            "pending keyboard input runs before result reset",
            () => InputWins(new SearchResultDispatcher())
        );
        failures += Run("result application stays on the owning dispatcher", RunsOnUiThread);
        failures += Run("cancelled dispatcher callback does not run", DispatcherCancellation);
        failures += Run("superseded count cannot publish after queuing", StaleCount);
        failures += Run("disposed collection cannot publish queued count", DisposedCount);
        failures += Run("superseded page cannot publish after queuing", StalePage);
        failures += Run("disposed collection cannot publish queued page", DisposedPage);
        failures += Run("current page replacement is still delivered", CurrentPage);
        failures += Run("scrollbar synchronous mode remains immediate", SynchronousMode);
        failures += Run("physical key guard accepts only nonzero own foreground handles", PhysicalKeyForegroundGuard);
        Console.WriteLine(failures == 0 ? "All search regression tests passed." : $"{failures} test(s) failed.");
        return failures == 0 ? 0 : 1;
    }

    private static int Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine("PASS " + name);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL " + name + ": " + e.Message);
            return 1;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void PhysicalKeyForegroundGuard()
    {
        var window = new IntPtr(1);
        var parent = new IntPtr(2);
        var foreign = new IntPtr(3);
        var cases = new[]
        {
            (Name: "own window", Foreground: window, Window: window, Parent: parent, Expected: true),
            (Name: "own parent", Foreground: parent, Window: window, Parent: parent, Expected: true),
            (
                Name: "own window without parent",
                Foreground: window,
                Window: window,
                Parent: IntPtr.Zero,
                Expected: true
            ),
            (Name: "foreign window", Foreground: foreign, Window: window, Parent: parent, Expected: false),
            (
                Name: "foreign window without parent",
                Foreground: foreign,
                Window: window,
                Parent: IntPtr.Zero,
                Expected: false
            ),
            (Name: "null foreground", Foreground: IntPtr.Zero, Window: window, Parent: parent, Expected: false),
            (
                Name: "null foreground without parent",
                Foreground: IntPtr.Zero,
                Window: window,
                Parent: IntPtr.Zero,
                Expected: false
            ),
            (
                Name: "null foreground without window",
                Foreground: IntPtr.Zero,
                Window: IntPtr.Zero,
                Parent: parent,
                Expected: false
            ),
            (
                Name: "all null handles",
                Foreground: IntPtr.Zero,
                Window: IntPtr.Zero,
                Parent: IntPtr.Zero,
                Expected: false
            ),
        };
        foreach (var test in cases)
        {
            var actual = PhysicalKeys.CanSendToForeground(test.Foreground, test.Window, test.Parent);
            Assert(actual == test.Expected, $"{test.Name}: expected {test.Expected}, got {actual}.");
        }
    }

    private static void Drain()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false)
        );
        Dispatcher.PushFrame(frame);
    }

    private static void InputWins(ISearchResultDispatcher dispatcher)
    {
        var order = new List<string>();
        using var collection = new VirtualizingCollection<string>(new Provider(10, "row"), 2, dispatcher);
        var countAtInput = -1;
        collection.CollectionChanged += (_, _) => order.Add("results");
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                countAtInput = collection.Count;
                order.Add("input");
            })
        );
        Drain();
        Assert(countAtInput == 0, $"Results were applied before pending input (count={countAtInput}).");
        Assert(collection.Count == 10, "Latest results were not applied when the dispatcher drained.");
        Assert(order.SequenceEqual(new[] { "input", "results" }), "Wrong input/result notification order.");
    }

    private static void RunsOnUiThread()
    {
        int owner = Environment.CurrentManagedThreadId,
            applied = -1;
        var dispatcher = new SearchResultDispatcher();
        var submitted = Task.Run(() =>
            dispatcher.InvokeAsync(() => applied = Environment.CurrentManagedThreadId, default)
        );
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (!submitted.IsCompleted && DateTime.UtcNow < timeout)
        {
            Drain();
            Thread.Yield();
        }
        Assert(submitted.IsCompletedSuccessfully, "Background submission did not complete.");
        Assert(applied == owner, "Collection update ran off the UI thread.");
    }

    private static void DispatcherCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var called = false;
        var task = new SearchResultDispatcher().InvokeAsync(() => called = true, cancellation.Token);
        cancellation.Cancel();
        Drain();
        Assert(!called && task.IsCanceled, "Cancelled result operation ran.");
    }

    private static void StaleCount()
    {
        var dispatcher = new QueuedDispatcher();
        using var collection = new VirtualizingCollection<string>(new Provider(100, "old"), 2, dispatcher);
        collection.UpdateProvider(new Provider(2, "new"));
        dispatcher.RunNext(); // Deliberately ignores cancellation; the collection must recheck too.
        Assert(collection.Count == 0, "Superseded count changed the result list.");
        dispatcher.RunNext();
        Assert(collection.Count == 2 && collection[0] == "new", "Latest count/first-page snapshot was lost.");
        Drain();
    }

    private static void DisposedCount()
    {
        var dispatcher = new QueuedDispatcher();
        var collection = new VirtualizingCollection<string>(new Provider(100, "old"), 2, dispatcher);
        collection.Dispose();
        dispatcher.RunNext();
        Assert(collection.Count == 0, "Disposed collection published a count.");
        Drain();
    }

    private static void StalePage()
    {
        var dispatcher = new QueuedDispatcher();
        using var collection = new VirtualizingCollection<string>(new Provider(2, "old", cached: false), 2, dispatcher);
        dispatcher.RunNext();
        _ = collection[0];
        Drain();
        Assert(dispatcher.Count == 1, "Page application was not queued.");
        var replacements = 0;
        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
                replacements++;
        };
        collection.UpdateProvider(new Provider(2, "new"));
        dispatcher.RunNext();
        Assert(replacements == 0, "Old page raised replacement notifications after a newer query.");
        dispatcher.RunNext();
        Assert(collection[0] == "new", "Old page poisoned the newer provider's cache.");
        Drain();
    }

    private static void DisposedPage()
    {
        var dispatcher = new QueuedDispatcher();
        var collection = new VirtualizingCollection<string>(new Provider(2, "old", cached: false), 2, dispatcher);
        dispatcher.RunNext();
        _ = collection[0];
        Drain();
        int replacements = 0;
        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
                replacements++;
        };
        collection.Dispose();
        dispatcher.RunNext();
        Assert(replacements == 0, "Disposed collection published a page.");
        Drain();
    }

    private static void CurrentPage()
    {
        var dispatcher = new QueuedDispatcher();
        using var collection = new VirtualizingCollection<string>(
            new Provider(2, "current", cached: false),
            2,
            dispatcher
        );
        dispatcher.RunNext();
        _ = collection[0];
        Drain();
        int replacements = 0;
        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
                replacements++;
        };
        dispatcher.RunNext();
        Assert(collection[0] == "current" && replacements == 1, "Current visible row was not replaced exactly once.");
        Drain();
    }

    private static void SynchronousMode()
    {
        var dispatcher = new QueuedDispatcher();
        using var collection = new VirtualizingCollection<string>(new Provider(2, "old"), 2, dispatcher);
        dispatcher.RunNext();
        collection.IsAsync = false;
        collection.UpdateProvider(new Provider(7, "sync", cached: false));
        Assert(collection.Count == 7 && collection[0] == "sync", "Synchronous scrollbar request was delayed.");
        Assert(dispatcher.Count == 0, "Synchronous path queued background work.");
        Drain();
    }

    private sealed class ImmediateDispatcher : ISearchResultDispatcher
    {
        public Task InvokeAsync(Action action, CancellationToken cancellationToken)
        {
            action();
            return Task.CompletedTask;
        }
    }

    private sealed class QueuedDispatcher : ISearchResultDispatcher
    {
        private readonly Queue<(Action Action, TaskCompletionSource Completion)> _queue = new();
        public int Count => _queue.Count;

        public Task InvokeAsync(Action action, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue((action, completion));
            return completion.Task;
        }

        public void RunNext()
        {
            Assert(_queue.Count > 0, "No result operation was queued.");
            var item = _queue.Dequeue();
            item.Action();
            item.Completion.SetResult();
        }
    }

    private sealed class Provider(int count, string row, bool cached = true) : IItemsProvider<string>
    {
        public bool IsBusy => false;
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public Task<int> FetchCount(int pageSize, bool isAsync, CancellationToken cancellationToken) =>
            Task.FromResult(count);

        public Task<IList<string>> FetchRange(
            int startIndex,
            int pageSize,
            bool isAsync,
            CancellationToken cancellationToken
        ) => Task.FromResult<IList<string>>(Enumerable.Repeat(row, pageSize).ToArray());

        public bool TryFetchCachedFirstPage(out IList<string> items)
        {
            items = new[] { row, row };
            return cached;
        }
    }
}
