// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Settings;
using Rebound.Core;
using Rebound.Core.Native.Helpers;
using System;
using System.IO;
using System.Threading;
using Windows.ApplicationModel;
using WinRT;

namespace Rebound.Cleanup;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        // Unlock the System Composition Engine via LimitedAccessFeatures
        var featureId = "com.microsoft.windows.composition.engine";
        var code = "26ef12c7-bf7e-4fa7-ac71-9665b27be6f7";
        var token = FeatureTokenGenerator.GenerateTokenFromFeatureId(featureId, code);
        var attestation = FeatureTokenGenerator.GenerateAttestation(featureId);

        var accessResult = LimitedAccessFeatures.TryUnlockFeature(featureId, token, attestation);

        // If experimental system compositor support is requested, enable it
        if (File.Exists(Path.Combine(Variables.ReboundDataFolder, "use_system_compositor")))
            CompositionEngine.TrySetProcessEngine(CompositionEngineType.System);

        XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);
        XamlOptionalChanges.EnableChange(XamlChangeId.DeferContextFlyoutInit);
        XamlOptionalChanges.EnableChange(XamlChangeId.OptimizeApplyStyles);
        XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations);

        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            var app = new App();
        });
    }
}