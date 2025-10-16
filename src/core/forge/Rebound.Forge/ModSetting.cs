using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Rebound.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rebound.Forge;

internal enum ModInfoBarSeverity
{
    Informational,
    Success,
    Warning,
    Error,
}

internal interface IModSetting : IModItem { }

internal interface IModItem { }

internal partial class ModInfoBar : ObservableObject, IModItem
{
    [ObservableProperty] public partial string Title { get; set; }
    [ObservableProperty] public partial string Message { get; set; }
    [ObservableProperty] public partial ModInfoBarSeverity Severity { get; set; }
    [ObservableProperty] public partial bool IsClosable { get; set; }
}

internal partial class ModLabel : ObservableObject, IModItem
{
    [ObservableProperty] public partial string Text { get; set; }
}

internal partial class ModBoolSetting : ObservableObject, IModSetting
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string Description { get; set; }
    [ObservableProperty] public partial string Identifier { get; set; }
    [ObservableProperty] public partial string AppName { get; set; }
    [ObservableProperty] public partial string IconGlyph { get; set; }

    [ObservableProperty] public partial bool Value { get; set; }

    public ModBoolSetting(bool defaultValue = default)
    {
        Value = SettingsHelper.GetValue(Identifier, AppName, defaultValue);
    }

    partial void OnValueChanged(bool value)
    {
        SettingsHelper.SetValue(Identifier, AppName, value);
    }
}

internal partial class ModStringSetting : ObservableObject, IModSetting
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string Description { get; set; }
    [ObservableProperty] public partial string Identifier { get; set; }
    [ObservableProperty] public partial string AppName { get; set; }
    [ObservableProperty] public partial string IconGlyph { get; set; }
    [ObservableProperty] public partial string PlaceholderText { get; set; }

    [ObservableProperty] public partial string Value { get; set; }

    public ModStringSetting(string defaultValue = default)
    {
        Value = SettingsHelper.GetValue(Identifier, AppName, defaultValue);
    }

    partial void OnValueChanged(string value)
    {
        SettingsHelper.SetValue(Identifier, AppName, value);
    }
}

internal partial class ModEnumSetting : ObservableObject, IModSetting
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string Description { get; set; }
    [ObservableProperty] public partial string Identifier { get; set; }
    [ObservableProperty] public partial string AppName { get; set; }
    [ObservableProperty] public partial string IconGlyph { get; set; }

    [ObservableProperty] public partial int Value { get; set; }

    public ObservableCollection<string> Options { get; set; } = new();

    public ModEnumSetting(int defaultValue = default)
    {
        Value = SettingsHelper.GetValue(Identifier, AppName, defaultValue);
    }

    partial void OnValueChanged(int value)
    {
        SettingsHelper.SetValue(Identifier, AppName, value);
    }
}