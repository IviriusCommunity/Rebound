// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using OwlCore.Storage.System.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rebound.ControlPanel;

internal struct CplEntry()
{
    public object? Object { get; set; } = null;
    public List<string> Args { get; set; } = [];
}

internal class CplArgs
{
    public static readonly string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);

    public static readonly string intlCplPath = Path.Combine(systemFolder, "intl.cpl");
    public static readonly string appWizCplPath = Path.Combine(systemFolder, "appwiz.cpl");
    public static readonly string SystemPropertiesComputerNameExePath = Path.Combine(systemFolder, "SystemPropertiesComputerName.exe");

    public const string ADMINISTRATIVE_TOOLS_UTIL = @"/name Microsoft.AdministrativeTools";
    public const string ADMINISTRATIVE_TOOLS = @"admintools";
    public const string INTLCPL_DATE = ",,/p:date";
}