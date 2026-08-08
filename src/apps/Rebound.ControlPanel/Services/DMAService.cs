// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.DOMAIN;
using static TerraFX.Interop.Windows.SE;
using static TerraFX.Interop.Windows.SECURITY;
using static TerraFX.Interop.Windows.TOKEN;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.ControlPanel.Services;

internal static unsafe class DMAService
{
    private static readonly string PolicyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "IntegratedServicesRegionPolicySet.json"
    );

    public sealed record FeatureFlag(
        string Key,
        string Guid,
        string Description
    );

    public const string EDGE_UNINSTALLABLE = "{1bca278a-5d11-4acf-ad2f-f9ab6d7f93a6}";
    public const string EDGE_DEFAULT_LOCK = "{50db02cb-3f22-465b-9205-0e722c2caf0c}";
    public const string DEFAULT_APPS_EXTRA_TYPES = "{8e779eeb-377c-4373-9e80-56e164fdca7e}";
    public const string XBOX_FULLSCREEN_EXPERIENCE = "{cde299a6-4bb6-4623-ae47-cbf055b70873}";
    public const string WIDGETS_DATA_RESTRICTION = "{0dcb52b1-6b3f-4e95-8049-bf2281ae2eda}";
    public const string THIRD_PARTY_WIDGETS_DATA_RESTRICTION = "{4323bb73-d394-4c3a-b9df-224ab359844f}";
    public const string SHARED_ODD_CONSENT = "{1f5403a8-5d44-40b9-a002-dda7ce7b0d01}";
    public const string WINDOWS_COPILOT = "{4ac54d32-0799-405f-9bf4-1fe094cd859c}";
    public const string AUTOMATIC_APP_SIGNIN = "{1d290cdb-499c-4d42-938a-9b8dceffe998}";
    public const string FULLSCREEN_SETUP_PROMOTIONS = "{b5113273-5a79-4488-a7b4-0a4fc5d5b194}";
    public const string SETUP_FLOW_PROMOTIONAL_PAGES = "{75b09d11-2e0d-4029-bd88-b91ec9a229bb}";
    public const string EDGE_PROMOTION_OVERRIDE_DEFAULT_BROWSER = "{2bf706de-6dbb-4692-b7ef-84d80c47e927}";
    public const string CAMPAIGN_SEGMENT_TARGETING = "{36996754-e327-483a-902f-523e2ba03239}";
    public const string PERSONALIZED_OFFERS = "{b59b2b22-db72-4e2b-867a-4bfc53290abb}";
    public const string COPILOT_PWA_PREPIN = "{fc5f578d-597b-47ee-8e0e-051cf79fb2c5}";
    public const string ACCOUNT_SYNC_CONSENT = "{c8e6c136-c6d1-419c-919a-4f8935662914}";
    public const string START_EXPERIENCES_UNINSTALLABLE = "{c74bb959-f763-4b9b-9b14-6b312df2c937}";
    public const string PRIVACY_UX_MODIFIED_LAYOUT = "{dbfadcb8-0302-4de0-87be-4dfbb71950f9}";
    public const string RECOMMENDED_ACTIONS = "{f3a8c2d4-5b7e-4c9a-8f3d-1e2b3c4d5e6f}";
    public const string STORE_REGION_SPECIFIC_OPTIONS = "{9a453b66-5ea7-4322-9aba-b054e914cc67}";

    public static readonly IReadOnlyList<FeatureFlag> FeatureFlags = new[]
    {
        new FeatureFlag(nameof(EDGE_UNINSTALLABLE),
            EDGE_UNINSTALLABLE,
            "Edge is uninstallable"),

        new FeatureFlag(nameof(EDGE_DEFAULT_LOCK),
            EDGE_DEFAULT_LOCK,
            "SetAppAsDefault Public API exception for setting Edge as default"),

        new FeatureFlag(nameof(DEFAULT_APPS_EXTRA_TYPES),
            DEFAULT_APPS_EXTRA_TYPES,
            "Default Apps settings extra types, one click PDF, pinning"),

        new FeatureFlag(nameof(XBOX_FULLSCREEN_EXPERIENCE),
            XBOX_FULLSCREEN_EXPERIENCE,
            "Xbox full screen experience"),

        new FeatureFlag(nameof(WIDGETS_DATA_RESTRICTION),
            WIDGETS_DATA_RESTRICTION,
            "Restrict Widgets data sharing"),

        new FeatureFlag(nameof(THIRD_PARTY_WIDGETS_DATA_RESTRICTION),
            THIRD_PARTY_WIDGETS_DATA_RESTRICTION,
            "Restrict third-party Widgets data sharing"),

        new FeatureFlag(nameof(SHARED_ODD_CONSENT),
            SHARED_ODD_CONSENT,
            "Shared ODD consent"),

        new FeatureFlag(nameof(WINDOWS_COPILOT),
            WINDOWS_COPILOT,
            "Windows Copilot"),

        new FeatureFlag(nameof(AUTOMATIC_APP_SIGNIN),
            AUTOMATIC_APP_SIGNIN,
            "Automatic app sign-in"),

        new FeatureFlag(nameof(FULLSCREEN_SETUP_PROMOTIONS),
            FULLSCREEN_SETUP_PROMOTIONS,
            "Full screen user setup promotional surfaces are allowed"),

        new FeatureFlag(nameof(SETUP_FLOW_PROMOTIONAL_PAGES),
            SETUP_FLOW_PROMOTIONAL_PAGES,
            "Individual promotional pages within larger user setup flows are allowed"),

        new FeatureFlag(nameof(EDGE_PROMOTION_OVERRIDE_DEFAULT_BROWSER),
            EDGE_PROMOTION_OVERRIDE_DEFAULT_BROWSER,
            "Promotion and direct launch of Edge instead of the default browser from campaigns is allowed"),

        new FeatureFlag(nameof(CAMPAIGN_SEGMENT_TARGETING),
            CAMPAIGN_SEGMENT_TARGETING,
            "Campaign segment targeting is allowed"),

        new FeatureFlag(nameof(PERSONALIZED_OFFERS),
            PERSONALIZED_OFFERS,
            "Replace tailored experience with personalized offers"),

        new FeatureFlag(nameof(COPILOT_PWA_PREPIN),
            COPILOT_PWA_PREPIN,
            "Windows Copilot PWA is pre-pinned during LCU upgrades (EEA excluded)"),

        new FeatureFlag(nameof(ACCOUNT_SYNC_CONSENT),
            ACCOUNT_SYNC_CONSENT,
            "Windows Account Sync Consent is applicable"),

        new FeatureFlag(nameof(START_EXPERIENCES_UNINSTALLABLE),
            START_EXPERIENCES_UNINSTALLABLE,
            "Make StartExperiencesApp uninstallable"),

        new FeatureFlag(nameof(PRIVACY_UX_MODIFIED_LAYOUT),
            PRIVACY_UX_MODIFIED_LAYOUT,
            "Show modified UX layout for Privacy-related settings"),

        new FeatureFlag(nameof(RECOMMENDED_ACTIONS),
            RECOMMENDED_ACTIONS,
            "Recommended actions show in File Explorer and Desktop"),

        new FeatureFlag(nameof(STORE_REGION_SPECIFIC_OPTIONS),
            STORE_REGION_SPECIFIC_OPTIONS,
            "Microsoft Store has specific options available in some regions")
    };
    /// <summary>
    /// Checks if a target DMA feature GUID is enabled for the current region.
    /// </summary>
    public static bool CheckIsDmaFeatureEnabled(string targetGuid)
    {
        if (string.IsNullOrEmpty(targetGuid))
        {
            ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), "Target GUID was null or empty.", LogMessageSeverity.Warning);
            return false;
        }

        string policyPath = PolicyFilePath;

        if (!File.Exists(policyPath))
        {
            ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), $"Policy file not found at: {policyPath}", LogMessageSeverity.Error);
            return false;
        }

        string currentUserCountryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;

        try
        {
            var json = JsonNode.Parse(File.ReadAllText(policyPath));
            var policies = json?["policies"]?.AsArray();

            if (policies == null)
            {
                ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), "Failed to parse policies array from JSON file.", LogMessageSeverity.Error);
                return false;
            }

            foreach (var policyNode in policies)
            {
                if (policyNode is not JsonObject policy) continue;

                var locatedGuid = policy["guid"]?.ToString();
                if (!string.Equals(locatedGuid, targetGuid, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var regionNode = policy["conditions"]?["region"];
                var enabledRegions = regionNode?["enabled"]?.AsArray();
                var disabledRegions = regionNode?["disabled"]?.AsArray();

                // 1. Whitelist Check ('enabled' array)
                if (enabledRegions != null && enabledRegions.Count > 0)
                {
                    bool isWhitelisted = enabledRegions.Any(r => string.Equals(r?.ToString(), currentUserCountryCode, StringComparison.OrdinalIgnoreCase));
                    ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), $"Feature {targetGuid} (enabled array) status for region '{currentUserCountryCode}': {isWhitelisted}", LogMessageSeverity.Message);
                    return isWhitelisted;
                }

                // 2. Blacklist Check ('disabled' array)
                if (disabledRegions != null && disabledRegions.Count > 0)
                {
                    bool isBlacklisted = disabledRegions.Any(r => string.Equals(r?.ToString(), currentUserCountryCode, StringComparison.OrdinalIgnoreCase));
                    bool isEnabled = !isBlacklisted;
                    ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), $"Feature {targetGuid} (disabled array) status for region '{currentUserCountryCode}': {isEnabled}", LogMessageSeverity.Message);
                    return isEnabled;
                }

                // 3. Fallback to defaultState
                string defaultState = policy["defaultState"]?.ToString();
                if (!string.IsNullOrEmpty(defaultState))
                {
                    bool isEnabled = string.Equals(defaultState, "enabled", StringComparison.OrdinalIgnoreCase);
                    ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), $"Feature {targetGuid} using defaultState '{defaultState}': {isEnabled}", LogMessageSeverity.Message);
                    return isEnabled;
                }

                ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), $"Policy GUID {targetGuid} found, but no valid region conditions or defaultState were present.", LogMessageSeverity.Warning);
                return false;
            }

            ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), $"Target GUID {targetGuid} was not found in policy file.", LogMessageSeverity.Warning);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(nameof(CheckIsDmaFeatureEnabled), $"Failed to check DMA feature state for GUID {targetGuid}.", LogMessageSeverity.Error, ex);
        }

        return false;
    }

    /// <summary>
    /// Grants access permissions and updates the JSON policy file for the specified feature.
    /// </summary>
    public static void ToggleDmaFeature(string targetGuid, bool enable)
    {
        if (string.IsNullOrEmpty(targetGuid))
        {
            ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), "Target GUID was null or empty.", LogMessageSeverity.Warning);
            return;
        }

        string policyPath = PolicyFilePath;

        try
        {
            string currentUserCountryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;

            TakeOwnershipAndGrantAccess(policyPath);

            var jsonStr = File.ReadAllText(policyPath);
            var json = JsonNode.Parse(jsonStr);
            var policies = json?["policies"]?.AsArray();

            if (policies == null)
            {
                ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), "Failed to parse policies array from JSON file.", LogMessageSeverity.Error);
                return;
            }

            bool modified = false;

            foreach (var policyNode in policies)
            {
                if (policyNode is not JsonObject policy) continue;

                var locatedGuid = policy["guid"]?.ToString();
                if (!string.Equals(locatedGuid, targetGuid, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 1. Determine baseline policy type (defaultState defaults to 'disabled' if unspecified)
                string defaultState = policy["defaultState"]?.ToString() ?? "disabled";
                bool isDefaultEnabled = string.Equals(defaultState, "enabled", StringComparison.OrdinalIgnoreCase);

                // 2. Ensure nested JSON structures exist down to conditions.region
                if (policy["conditions"] is not JsonObject conditionsNode)
                {
                    conditionsNode = new JsonObject();
                    policy["conditions"] = conditionsNode;
                }

                if (conditionsNode["region"] is not JsonObject regionNode)
                {
                    regionNode = new JsonObject();
                    conditionsNode["region"] = regionNode;
                }

                // 3. Execute logic based on defaultState strategy
                if (isDefaultEnabled)
                {
                    // DEFAULT ENABLED -> Use Blacklist ('disabled' array)
                    var disabledArray = regionNode["disabled"]?.AsArray();
                    if (disabledArray == null)
                    {
                        disabledArray = new JsonArray();
                        regionNode["disabled"] = disabledArray;
                    }

                    // Locate existing entry matching region code
                    var existingItem = disabledArray.FirstOrDefault(r => string.Equals(r?.ToString(), currentUserCountryCode, StringComparison.OrdinalIgnoreCase));

                    if (enable && existingItem != null)
                    {
                        // To ENABLE -> Remove from blacklist
                        disabledArray.Remove(existingItem);
                        modified = true;
                        ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), $"Removed region '{currentUserCountryCode}' from 'disabled' array for GUID {targetGuid}.", LogMessageSeverity.Message);
                    }
                    else if (!enable && existingItem == null)
                    {
                        // To DISABLE -> Add to blacklist
                        disabledArray.Add(JsonValue.Create(currentUserCountryCode));
                        modified = true;
                        ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), $"Added region '{currentUserCountryCode}' to 'disabled' array for GUID {targetGuid}.", LogMessageSeverity.Message);
                    }
                }
                else
                {
                    // DEFAULT DISABLED -> Use Whitelist ('enabled' array)
                    var enabledArray = regionNode["enabled"]?.AsArray();
                    if (enabledArray == null)
                    {
                        enabledArray = new JsonArray();
                        regionNode["enabled"] = enabledArray;
                    }

                    // Locate existing entry matching region code
                    var existingItem = enabledArray.FirstOrDefault(r => string.Equals(r?.ToString(), currentUserCountryCode, StringComparison.OrdinalIgnoreCase));

                    if (enable && existingItem == null)
                    {
                        // To ENABLE -> Add to whitelist
                        enabledArray.Add(JsonValue.Create(currentUserCountryCode));
                        modified = true;
                        ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), $"Added region '{currentUserCountryCode}' to 'enabled' array for GUID {targetGuid}.", LogMessageSeverity.Message);
                    }
                    else if (!enable && existingItem != null)
                    {
                        // To DISABLE -> Remove from whitelist
                        enabledArray.Remove(existingItem);
                        modified = true;
                        ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), $"Removed region '{currentUserCountryCode}' from 'enabled' array for GUID {targetGuid}.", LogMessageSeverity.Message);
                    }
                }
            }

            if (modified)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
                };
                File.WriteAllText(policyPath, json.ToJsonString(options));
                ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), $"Successfully updated policy file for GUID {targetGuid}.", LogMessageSeverity.Message);
            }
            else
            {
                ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), $"No changes required for GUID {targetGuid} (state already matches enable={enable}).", LogMessageSeverity.Message);
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(nameof(ToggleDmaFeature), $"Failed to toggle DMA feature GUID {targetGuid}.", LogMessageSeverity.Error, ex);
        }
    }
    /// <summary>
    /// Uses TerraFX raw P/Invoke to adjust process token, take ownership, and set DACL.
    /// </summary>
    private static void TakeOwnershipAndGrantAccess(string path)
    {
        HANDLE hToken;
        if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
        {
            // Enable BOTH Take Ownership and Restore privileges
            EnablePrivilege(hToken, "SeTakeOwnershipPrivilege");
            EnablePrivilege(hToken, "SeRestorePrivilege");

            CloseHandle(hToken);
        }
        else
        {
            ReboundLogger.WriteToLog(nameof(TakeOwnershipAndGrantAccess), "Failed to open process token via OpenProcessToken.", LogMessageSeverity.Error);
        }

        void* pAdminSid = null;
        SID_IDENTIFIER_AUTHORITY ntAuthority = default;
        ntAuthority.Value[5] = 5;

        if (!AllocateAndInitializeSid(&ntAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &pAdminSid))
        {
            ReboundLogger.WriteToLog(nameof(TakeOwnershipAndGrantAccess), "Failed to allocate and initialize Administrator SID.", LogMessageSeverity.Error);
            throw new InvalidOperationException("Failed to initialize Administrator SID.");
        }

        using ManagedPtr<char> pPath = path;

        // 1. Take ownership
        uint resultOwner = SetNamedSecurityInfoW(
            (char*)pPath,
            SE_OBJECT_TYPE.SE_FILE_OBJECT,
            OWNER_SECURITY_INFORMATION,
            pAdminSid,
            null, null, null);

        if (resultOwner != 0)
        {
            FreeSid(pAdminSid);
            var ex = new UnauthorizedAccessException($"Failed to set owner. Win32 Error Code: {resultOwner}");
            ReboundLogger.WriteToLog(nameof(TakeOwnershipAndGrantAccess), $"SetNamedSecurityInfoW (Owner) failed for path: {path}", LogMessageSeverity.Error, ex);
            throw ex;
        }

        // 2. Build DACL granting Administrators full control
        EXPLICIT_ACCESS_W ea = default;
        ea.grfAccessPermissions = GENERIC_ALL;
        ea.grfAccessMode = ACCESS_MODE.SET_ACCESS;
        ea.grfInheritance = NO_INHERITANCE;
        ea.Trustee.TrusteeForm = TRUSTEE_FORM.TRUSTEE_IS_SID;
        ea.Trustee.TrusteeType = TRUSTEE_TYPE.TRUSTEE_IS_GROUP;
        ea.Trustee.ptstrName = (char*)pAdminSid;

        ACL* pNewDacl = null;
        uint resultAcl = SetEntriesInAclW(1, &ea, null, &pNewDacl);
        if (resultAcl != 0)
        {
            FreeSid(pAdminSid);
            var ex = new UnauthorizedAccessException($"Failed to build ACL. Win32 Error Code: {resultAcl}");
            ReboundLogger.WriteToLog(nameof(TakeOwnershipAndGrantAccess), "SetEntriesInAclW failed while constructing new DACL.", LogMessageSeverity.Error, ex);
            throw ex;
        }

        // 3. Apply new DACL
        uint resultDacl = SetNamedSecurityInfoW(
            (char*)pPath,
            SE_OBJECT_TYPE.SE_FILE_OBJECT,
            DACL_SECURITY_INFORMATION,
            null,
            null,
            pNewDacl,
            null);

        // Clean up memory
        if (pNewDacl != null) LocalFree((HLOCAL)pNewDacl);
        if (pAdminSid != null) FreeSid(pAdminSid);

        if (resultDacl != 0)
        {
            var ex = new UnauthorizedAccessException($"Failed to set DACL. Win32 Error Code: {resultDacl}");
            ReboundLogger.WriteToLog(nameof(TakeOwnershipAndGrantAccess), $"SetNamedSecurityInfoW (DACL) failed for path: {path}", LogMessageSeverity.Error, ex);
            throw ex;
        }

        ReboundLogger.WriteToLog(nameof(TakeOwnershipAndGrantAccess), $"Successfully took ownership and granted DACL full permissions for path: {path}", LogMessageSeverity.Message);
    }

    private static void EnablePrivilege(HANDLE hToken, string privilegeName)
    {
        using ManagedPtr<char> privName = privilegeName;
        using ManagedPtr<TOKEN_PRIVILEGES> tp = new(default(TOKEN_PRIVILEGES));

        TOKEN_PRIVILEGES* pTp = tp;
        pTp->PrivilegeCount = 1;

        LUID localLuid;
        if (LookupPrivilegeValueW(null, privName, &localLuid))
        {
            pTp->Privileges[0].Luid = localLuid;
            pTp->Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

            if (!AdjustTokenPrivileges(hToken, FALSE, tp, (uint)sizeof(TOKEN_PRIVILEGES), null, null))
            {
                ReboundLogger.WriteToLog(nameof(EnablePrivilege), $"AdjustTokenPrivileges failed when enabling privilege: {privilegeName}", LogMessageSeverity.Warning);
            }
        }
        else
        {
            ReboundLogger.WriteToLog(nameof(EnablePrivilege), $"LookupPrivilegeValueW failed for privilege: {privilegeName}", LogMessageSeverity.Warning);
        }
    }
}