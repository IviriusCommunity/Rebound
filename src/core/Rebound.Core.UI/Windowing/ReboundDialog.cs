// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace Rebound.Core.UI.Windowing;

public static class ReboundDialog
{
    public static async Task<ContentDialogResult> ShowAsync(
        string title,
        string content,
        string? primaryButtonText = null,
        string? closeButtonText = null,
        ContentDialogButton defaultButton = ContentDialogButton.Close)
    {
        var hostWindow = new WindowEx()
        {
            IsMinimizable = false,
            IsResizable = false,
            IsShownInSwitchers = false,
            IsTitleBarVisible = false,
            IsAlwaysOnTop = true,
            SystemBackdrop = new TransparentTintBackdrop(),
            WindowState = WindowState.Maximized
        };

        var frame = new Frame();
        hostWindow.Content = frame;
        hostWindow.Activate();

        var tcs = new TaskCompletionSource<ContentDialogResult>();

        frame.Loaded += OnFrameLoaded;

        async void OnFrameLoaded(object sender, RoutedEventArgs e)
        {
            frame.Loaded -= OnFrameLoaded;

            var contentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText ?? string.Empty,
                CloseButtonText = closeButtonText ?? string.Empty,
                DefaultButton = defaultButton,
                XamlRoot = frame.XamlRoot
            };

            var result = await contentDialog.ShowAsync();

            hostWindow.Close();
            tcs.SetResult(result);
        }

        return await tcs.Task.ConfigureAwait(false);
    }
}