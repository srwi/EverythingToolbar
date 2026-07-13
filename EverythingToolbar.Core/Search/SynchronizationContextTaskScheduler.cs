using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingToolbar.Core.Search
{
    public class SynchronizationContextTaskScheduler(SynchronizationContext? synchronizationContext) : TaskScheduler
    {
        protected override void QueueTask(Task task)
        {
            if (synchronizationContext != null)
            {
                synchronizationContext.Post(_ => TryExecuteTask(task), null);
            }
            else
            {
                // Fallback to thread pool if no synchronization context
                Task.Run(() => TryExecuteTask(task));
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return SynchronizationContext.Current == synchronizationContext && TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return [];
        }
    }
}
