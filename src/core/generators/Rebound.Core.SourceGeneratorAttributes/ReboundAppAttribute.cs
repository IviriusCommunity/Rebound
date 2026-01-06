// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;

namespace Rebound.Generators;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReboundAppAttribute(string singleProcessTaskName, string legacyLaunchItems) : Attribute
{
    public string SingleProcessTaskName { get; } = singleProcessTaskName;
    public string LegacyLaunchItems { get; } = legacyLaunchItems;
}

public class LegacyLaunchItem(string name, string launchArg, string iconPath)
{
    public string Name { get; } = name;
    public string LaunchArg { get; } = launchArg;
    public string IconPath { get; } = iconPath;
}