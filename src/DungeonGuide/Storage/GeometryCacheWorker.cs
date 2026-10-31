using System;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonGuide;

/// <summary>Serializes cache I/O jobs without waiting on workers or accessing live game objects.</summary>
internal sealed class GeometryCacheWorker
{
    private Task _tail = Task.CompletedTask;

    /// <summary>Queues private data work in submission order; callers observe completion on the game thread.</summary>
    public Task<T> Enqueue<T>(Func<T> work)
    {
        var next = _tail.ContinueWith(previous =>
        {
            if (previous.IsFaulted)
            {
                _ = previous.Exception;
            }

            return work();
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        _tail = next;
        return next;
    }
}
