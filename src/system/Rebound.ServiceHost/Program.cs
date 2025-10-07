// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers.Services;
using System;
using System.Collections.Concurrent;
using System.Threading;
using TerraFX.Interop.WinRT;
using static TerraFX.Interop.WinRT.WinRT;

namespace Rebound.ServiceHost;

internal class Program
{
    public static BlockingCollection<Action> _actions = new();

    [STAThread]
    static unsafe void Main(string[] args)
    {
        // If this is the first instance, start the application
        RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
        _ = new App();

        // Tell the SingleInstanceAppService to "launch"
        // This will fire OnSingleInstanceLaunched in the first instance
        var serviceField = typeof(App).GetField("_singleInstanceAppService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (serviceField?.GetValue(null) is SingleInstanceAppService service)
        {
            service.Launch(string.Join(" ", args));
        }

        while (true)
        {
            if (_actions.TryTake(out var action, Timeout.Infinite))
            {
                try { action(); }
                catch { }
            }
        }
    }
}