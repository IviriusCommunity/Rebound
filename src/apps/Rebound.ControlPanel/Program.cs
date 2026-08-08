// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Settings;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Windows.ApplicationModel;
using WinRT;

namespace Rebound.ControlPanel;

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

static class FeatureTokenGenerator
{
    public static string GenerateTokenFromFeatureId(string featureId, string code)
        => GenerateFeatureToken(featureId, code, AppInfo.Current.PackageFamilyName);

    public static string GenerateAttestation(string featureId)
        => $"{AppInfo.Current.PackageFamilyName.Split('_').Last()} has registered their use of {featureId} with Microsoft and agrees to the terms of use.";

    private static string GenerateFeatureToken(string featureId, string featureKey, string packageIdentity)
    {
        var fullBytes = Encoding.UTF8.GetBytes($"{featureId}!{featureKey}!{packageIdentity}");
        var tokenBytes = new byte[16];
        var hash = SHA256.HashData(fullBytes);
        Array.Copy(hash, tokenBytes, tokenBytes.Length);

        return Convert.ToBase64String(tokenBytes);
    }
}