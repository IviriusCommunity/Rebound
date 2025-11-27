// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Windows.UI;

namespace Rebound.Hub.Cards;

public class LinkCard
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public string? Link { get; set; }
}

internal class AppCard
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public string? PicturePath { get; set; }
    public string? Link { get; set; }
    public string? Publisher { get; set; }
    public Color AccentColor { get; set; }
    public Color AccentTextColor { get; set; }
}
