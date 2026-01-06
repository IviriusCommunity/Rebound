// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.SystemInformation;

internal class Normalizer
{
    public static string NormalizeTrademarkSymbols(string input) => input
            .Replace("(R)", "®", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(r)", "®", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(TM)", "™", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(tm)", "™", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(C)", "©", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(c)", "©", StringComparison.InvariantCultureIgnoreCase);
}