// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.UI.UWP;

public static class LocalizedResource
{
    private static readonly Windows.ApplicationModel.Resources.ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

    /// <summary>
    /// Retrieves a localized string from resource files.
    /// </summary>
    /// <param name="stringName">
    /// The name of the string resource.
    /// </param>
    /// <returns>
    /// The string corresponding to the current system language. If none, falls back to en-US.
    /// </returns>
    public static string GetLocalizedString(string stringName)
    {
        return resourceLoader.GetString(stringName);
    }

    /// <summary>
    /// Retrieves a formatted localized string from resource files with args.
    /// </summary>
    /// <param name="templateName">
    /// The name of the template string resource.
    /// </param>
    /// <param name="args">
    /// The arguments to pass onto the template string.
    /// </param>
    /// <returns>
    /// The string corresponding to the current system language. If none, falls back to en-US.
    /// </returns>
    public static string GetLocalizedStringFromTemplate(string templateName, params object?[] args)
    {
        string template = resourceLoader.GetString(templateName);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args);
    }
}