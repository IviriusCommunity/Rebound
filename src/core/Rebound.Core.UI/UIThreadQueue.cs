using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rebound.Core.UI;

public static class UIThreadQueue
{
#if WINUI3
    // Fire-and-forget version (existing)
    public static void QueueAction(Func<Task> action)
    {
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(action);
    }

    // Awaitable version (new!)
    public static Task QueueActionAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<bool>();

        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
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

        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
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
#else
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
#endif
}