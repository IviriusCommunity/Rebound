// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;
using Windows.ApplicationModel;

namespace Rebound.Core.Native.Helpers;

public static class FeatureTokenGenerator
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