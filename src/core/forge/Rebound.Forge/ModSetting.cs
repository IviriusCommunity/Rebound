// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace Rebound.Forge;

/// <summary>
/// Info bar severity for Rebound settings, equivalent of <see cref="InfoBarSeverity"/>
/// </summary>
public enum ModInfoBarSeverity
{
    /// <summary>
    /// Equivalent of <see cref="InfoBarSeverity.Informational"/>.
    /// </summary>
    Informational,

    /// <summary>
    /// Equivalent of <see cref="InfoBarSeverity.Success"/>.
    /// </summary>
    Success,

    /// <summary>
    /// Equivalent of <see cref="InfoBarSeverity.Warning"/>.
    /// </summary>
    Warning,

    /// <summary>
    /// Equivalent of <see cref="InfoBarSeverity.Error"/>.
    /// </summary>
    Error,
}

/// <summary>
/// Interface used to query items that are represented as a settings item.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
public interface IModSetting : IModItem { }

/// <summary>
/// Interface used to query items that appear in the settings list.
/// </summary>
public interface IModItem { }
#pragma warning restore CA1040 // Avoid empty interfaces

/// <summary>
/// Converts to <see cref="InfoBar"/>.
/// </summary>
public partial class ModInfoBar : ObservableObject, IModItem
{
    [ObservableProperty] public partial string Title { get; set; }
    [ObservableProperty] public partial string Message { get; set; }
    [ObservableProperty] public partial ModInfoBarSeverity Severity { get; set; }
    [ObservableProperty] public partial bool IsClosable { get; set; }
}

/// <summary>
/// Converts to <see cref="TextBlock"/>.
/// </summary>
public partial class ModLabel : ObservableObject, IModItem
{
    [ObservableProperty] public partial string Text { get; set; }
}

/// <summary>
/// Converts to a settings item with a boolean value.
/// </summary>
public partial class ModBoolSetting : ObservableObject, IModSetting
{
    [ObservableProperty] public partial string Name { get; set; } = "";
    [ObservableProperty] public partial string Description { get; set; } = "";
    [ObservableProperty] public partial string Identifier { get; set; } = "";
    [ObservableProperty] public partial string AppName { get; set; } = "";
    [ObservableProperty] public partial string IconGlyph { get; set; } = "";

    [ObservableProperty] public partial bool Value { get; set; } = false;

    /// <summary>
    /// Creates an instance of the <see cref="ModBoolSetting"/> class.
    /// </summary>
    /// <param name="defaultValue">Default value for the stored setting.</param>
    public ModBoolSetting(bool defaultValue = default)
    {
        Value = SettingsManager.GetValue(Identifier, AppName, defaultValue);
    }

    partial void OnValueChanged(bool value)
    {
        SettingsManager.SetValue(Identifier, AppName, value);
    }
}

/// <summary>
/// Converts to <see cref="InfoBar"/>
/// </summary>
public partial class ModStringSetting : ObservableObject, IModSetting
{
    [ObservableProperty] public partial string Name { get; set; } = "";
    [ObservableProperty] public partial string Description { get; set; } = "";
    [ObservableProperty] public partial string Identifier { get; set; } = "";
    [ObservableProperty] public partial string AppName { get; set; } = "";
    [ObservableProperty] public partial string IconGlyph { get; set; } = "";

    [ObservableProperty] public partial string PlaceholderText { get; set; } = "";
    [ObservableProperty] public partial string Value { get; set; } = "";

    /// <summary>
    /// Creates an instance of the <see cref="ModBoolSetting"/> class.
    /// </summary>
    /// <param name="defaultValue">Default value for the stored setting.</param>
    public ModStringSetting(string defaultValue = "")
    {
        Value = SettingsManager.GetValue(Identifier, AppName, defaultValue)!;
    }

    partial void OnValueChanged(string value)
    {
        SettingsManager.SetValue(Identifier, AppName, value);
    }
}

/// <summary>
/// Converts to <see cref="InfoBar"/>
/// </summary>
public partial class ModEnumSetting : ObservableObject, IModSetting
{
    [ObservableProperty] public partial string Name { get; set; } = "";
    [ObservableProperty] public partial string Description { get; set; } = "";
    [ObservableProperty] public partial string Identifier { get; set; } = "";
    [ObservableProperty] public partial string AppName { get; set; } = "";
    [ObservableProperty] public partial string IconGlyph { get; set; } = "";

    [ObservableProperty] public partial int Value { get; set; }
    public ObservableCollection<string> Options { get; set; } = [];

    /// <summary>
    /// Creates an instance of the <see cref="ModBoolSetting"/> class.
    /// </summary>
    /// <param name="defaultValue">Default value for the stored setting.</param>
    public ModEnumSetting(int defaultValue = default)
    {
        Value = SettingsManager.GetValue(Identifier, AppName, defaultValue);
    }

    partial void OnValueChanged(int value)
    {
        SettingsManager.SetValue(Identifier, AppName, value);
    }
}