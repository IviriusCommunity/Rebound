using System;
using Microsoft.UI.Xaml;
using ApplicationData = Microsoft.Windows.Storage.ApplicationData;

namespace Riverside.Toolkit.Controls;

public partial class TitleBarEx
{
    /// <summary>
    /// Method to prevent the title bar from continuing its operations after the window has been closed. Invoke this right before closing the window to ensure no crashes occur.
    /// </summary>
    public void Dispose()
    {
        _closed = true;
    }

    /// <summary>
    /// Method used to retrieve window properties from the app's settings. Only works in packaged apps. For unpackaged apps, override this method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual T? GetValue<T>(string key)
    {
        try
        {
            var userSettings = ApplicationData.GetDefault();
            return (T)userSettings.LocalSettings.Values[key];
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Method used to set window properties in the app's settings. Only works in packaged apps. For unpackaged apps, override this method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="newValue"></param>
    public virtual void SetValue<T>(string key, T newValue)
    {
        try
        {
            var userSettings = ApplicationData.GetDefault();
            userSettings.LocalSettings.Values[key] = newValue;
        }
        catch
        {
            return;
        }
    }

    private T GetTemplateChild<T>(string name) where T : DependencyObject
    {
        DependencyObject child = GetTemplateChild(name);
        return child is T typedChild
            ? typedChild
            : throw new InvalidOperationException($"The template child '{name}' is not of type {typeof(T).FullName}.");
    }
}
