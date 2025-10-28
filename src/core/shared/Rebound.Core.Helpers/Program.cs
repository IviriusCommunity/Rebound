using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rebound.Core.Helpers;

internal static class UIThreadQueue
{
    public static readonly ConcurrentQueue<Func<Task>> _actions = new();
    public static readonly SemaphoreSlim _actionSignal = new(0);

    public static void QueueAction(Func<Task> action)
    {
        _actions.Enqueue(action);
        _actionSignal.Release(); // signal that a new action is available
    }
}