// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.UI;

public static class UIThreadQueue
{
    /// <summary>
    /// Queues an asynchronous action to be executed by the scheduler.
    /// </summary>
    /// <param name="action">A delegate that represents the asynchronous action to queue. Cannot be null.</param>
    public static void QueueAction(Func<Task> action)
    {
        action();
    }

    /// <summary>
    /// Queues the specified action for execution on the internal action queue.
    /// </summary>
    /// <param name="action">The action to execute. Cannot be null.</param>
    public static void QueueAction(Action action)
    {
        action();
    }

    /// <summary>
    /// Queues the specified asynchronous action to be executed in the background and returns a task that completes when
    /// the action has finished.
    /// </summary>
    /// <remarks>The action is executed asynchronously in the order it was queued. Exceptions thrown by the
    /// action are propagated to the returned task. This method does not block the calling thread.</remarks>
    /// <param name="action">The asynchronous action to queue for execution. Cannot be null.</param>
    /// <returns>A task that represents the queued action. The task completes when the action has finished executing, or faults
    /// if the action throws an exception.</returns>
    public static async Task QueueActionAsync(Func<Task> action)
    {
        await action();
    }

    /// <summary>
    /// Queues the specified asynchronous action for execution and returns a task that represents the queued operation.
    /// </summary>
    /// <remarks>The action is not executed immediately, but is instead enqueued for later execution. The
    /// returned task completes when the queued action has finished executing. If the action throws an exception, the
    /// returned task will be faulted with that exception.</remarks>
    /// <typeparam name="T">The type of the result produced by the asynchronous action.</typeparam>
    /// <param name="action">A function that returns a task representing the asynchronous operation to queue. Cannot be null.</param>
    /// <returns>A task that represents the queued operation. The task's result is the value produced by the asynchronous action.</returns>
    public static async Task<T> QueueActionAsync<T>(Func<Task<T>> action)
    {
        return await action();
    }
}