using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
using EverythingToolbar.Behaviors;

namespace EverythingToolbar.Helpers
{
    public static class IconLoader
    {
        private readonly record struct Request(WeakReference<ResultImages> Item, bool UseThumbnail);

        private const int WorkerCount = 2;
        private const int MaxBacklog = 128;

        private static readonly object Gate = new();
        private static readonly LinkedList<Request> Backlog = new();
        private static readonly List<(ResultImages Item, ImageSource Icon)> CompletedBatch = new();
        private static Dispatcher? _dispatcher;
        private static bool _flushScheduled;
        private static bool _workersStarted;

        // Must be called from the UI thread (the dispatcher is captured on first use)
        public static void Enqueue(ResultImages item, bool useThumbnail)
        {
            lock (Gate)
            {
                _dispatcher ??= Dispatcher.CurrentDispatcher;

                if (!_workersStarted)
                {
                    _workersStarted = true;
                    for (var i = 0; i < WorkerCount; i++)
                    {
                        new Thread(WorkerLoop)
                        {
                            IsBackground = true,
                            Name = "IconLoader",
                            Priority = ThreadPriority.BelowNormal,
                        }.Start();
                    }
                }

                Backlog.AddFirst(new Request(new WeakReference<ResultImages>(item), useThumbnail));

                while (Backlog.Count > MaxBacklog)
                    Backlog.RemoveLast();

                Monitor.Pulse(Gate);
            }
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                Request request;
                lock (Gate)
                {
                    while (Backlog.Count == 0)
                        Monitor.Wait(Gate);

                    request = Backlog.First!.Value;
                    Backlog.RemoveFirst();
                }

                if (!request.Item.TryGetTarget(out var item))
                    continue;

                ImageSource? icon = null;
                try
                {
                    icon = item.LoadRefinedIcon(request.UseThumbnail);
                }
                catch
                {
                    // The row keeps its extension-based icon; never kill the worker
                }

                if (icon == null)
                    continue;

                lock (Gate)
                {
                    CompletedBatch.Add((item, icon));

                    if (!_flushScheduled)
                    {
                        _flushScheduled = true;
                        _dispatcher!.BeginInvoke(FlushCompleted, DispatcherPriority.Background);
                    }
                }
            }
        }

        private static void FlushCompleted()
        {
            List<(ResultImages Item, ImageSource Icon)> batch;
            lock (Gate)
            {
                batch = new List<(ResultImages, ImageSource)>(CompletedBatch);
                CompletedBatch.Clear();
                _flushScheduled = false;
            }

            foreach (var (item, icon) in batch)
                item.ApplyRefinedIcon(icon);
        }
    }
}
