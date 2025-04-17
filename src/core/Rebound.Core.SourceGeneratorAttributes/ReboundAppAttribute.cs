using System;
using System.Collections.Generic;

namespace Rebound.Generators;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReboundAppAttribute(string singleProcessTaskName, List<LegacyLaunchItem> legacyLaunchItems) : Attribute
{
    public string SingleProcessTaskName { get; } = singleProcessTaskName;
    public List<LegacyLaunchItem> LegacyLaunchItems { get; } = legacyLaunchItems;
}

public class LegacyLaunchItem(string name, string launchArg, string iconPath)
{
    public string Name { get; } = name;
    public string LaunchArg { get; } = launchArg;
    public string IconPath { get; } = iconPath;
}