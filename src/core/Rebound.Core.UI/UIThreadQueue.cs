using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rebound.Core.UI;

public static class UIThreadQueue
{
    public static readonly ConcurrentQueue<Func<Task>> _actions = new();

    // Fire-and-forget version (existing)
    public static void QueueAction(Func<Task> action)
    {
        _actions.Enqueue(action);
    }

    // Awaitable version (new!)
    public static Task QueueActionAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<bool>();

        _actions.Enqueue(async () =>
        {
            try
            {
                await action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    // Awaitable version with return value (new!)
    public static Task<T> QueueActionAsync<T>(Func<Task<T>> action)
    {
        var tcs = new TaskCompletionSource<T>();

        _actions.Enqueue(async () =>
        {
            try
            {
                var result = await action();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }
}