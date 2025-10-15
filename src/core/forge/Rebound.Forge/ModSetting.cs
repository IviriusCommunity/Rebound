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

public enum ModSettingType
{
    Boolean,
    String,
    Number,
    Selection
}

internal partial class ModSetting : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string Description { get; set; }
    [ObservableProperty] public partial string Identifier { get; set; }
    [ObservableProperty] public partial string AppName { get; set; }
    [ObservableProperty] public partial int Type { get; set; }

    [ObservableProperty] public partial bool BoolValue { get; set; }
    [ObservableProperty] public partial string StringValue { get; set; } = string.Empty;
    [ObservableProperty] public partial double NumberValue { get; set; }
    [ObservableProperty] public partial int SelectionValue { get; set; } = new();

    [ObservableProperty] public partial ObservableCollection<string> SelectionOptions { get; set; } = new();

    public ModSetting(string name, string description, ModSettingType type)
    {
        Name = name;
        Description = description;
        Type = (int)type;
        this.PropertyChanged += OnAnyPropertyChanged;

        switch (Type)
        {
            case (int)ModSettingType.Boolean:
                BoolValue = SettingsHelper.GetValue<bool>(Identifier, AppName, false);
                break;
            case (int)ModSettingType.String:
                StringValue = SettingsHelper.GetValue<string>(Identifier, AppName, string.Empty) ?? string.Empty;
                break;
            case (int)ModSettingType.Number:
                NumberValue = SettingsHelper.GetValue<double>(Identifier, AppName, 0.0);
                break;
            case (int)ModSettingType.Selection:
                SelectionValue = SettingsHelper.GetValue<int>(Identifier, AppName, 0);
                break;
        }
    }

    public void OnAnyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (Type)
        {
            case (int)ModSettingType.Boolean:
                if (e.PropertyName == nameof(BoolValue))
                {
                    ReboundLogger.Log($"[ModSetting] {Name} changed to {BoolValue}");
                    SettingsHelper.SetValue(Identifier, AppName, BoolValue);
                }
                break;
            case (int)ModSettingType.String:
                if (e.PropertyName == nameof(StringValue))
                {
                    ReboundLogger.Log($"[ModSetting] {Name} changed to {StringValue}");
                    SettingsHelper.SetValue(Identifier, AppName, StringValue);
                }
                break;
            case (int)ModSettingType.Number:
                if (e.PropertyName == nameof(NumberValue))
                {
                    ReboundLogger.Log($"[ModSetting] {Name} changed to {NumberValue}");
                    SettingsHelper.SetValue(Identifier, AppName, NumberValue);
                }
                break;
            case (int)ModSettingType.Selection:
                if (e.PropertyName == nameof(SelectionValue))
                {
                    ReboundLogger.Log($"[ModSetting] {Name} changed to {SelectionValue}");
                    SettingsHelper.SetValue(Identifier, AppName, SelectionValue);
                }
                break;
        }
    }
}