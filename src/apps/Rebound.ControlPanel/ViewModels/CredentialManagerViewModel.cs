// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Native.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Credentials.UI;
using Windows.Win32;
using Windows.Win32.Security.Credentials;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.ControlPanel.ViewModels;

/// <summary>
/// Web credential (WinRT credential vault) wrapper class for data binding.
/// </summary>
internal partial class WebCredential : ObservableObject
{
    private readonly CredentialManagerViewModel _owner;

    public WebCredential(CredentialManagerViewModel owner)
    {
        _owner = owner;
    }

    [ObservableProperty] public partial string Url { get; set; } = string.Empty;

    [ObservableProperty] public partial string Username { get; set; } = string.Empty;

    [ObservableProperty] public partial string Password { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsPasswordAvailable { get; set; } = true;

    [ObservableProperty] public partial bool IsPasswordDisplayed { get; set; }

    public PasswordCredential? PasswordCredential { get; set; }

    /// <summary>
    /// Request user authentication and display the password if successful.
    /// </summary>
    [RelayCommand]
    public async Task ShowPasswordAsync()
    {
        if (_owner.IsAuthenticated)
        {
            IsPasswordDisplayed = true;
            return;
        }

        var availability = await UserConsentVerifier.CheckAvailabilityAsync();

        bool verified =
            availability != UserConsentVerifierAvailability.Available
                || (await UserConsentVerifier.RequestVerificationAsync(
                        "Verify your identity to view the password."
                    )) == UserConsentVerificationResult.Verified;

        if (!verified)
            return;

        _owner.IsAuthenticated = true;

        unsafe
        {
            PInvoke.SetWindowDisplayAffinity(
                new((void*)App.MainWindow!.GetWindowHandle()),
                WINDOW_DISPLAY_AFFINITY.WDA_MONITOR
            );
        }

        IsPasswordDisplayed = true;
    }

    [RelayCommand]
    public async Task DeleteCredentialAsync()
    {
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Delete",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Are you sure you want to delete this credential?",
            Content = $"This will delete the credential for {Url} with username {Username}. This action cannot be undone.",
            DefaultButton = ContentDialogButton.Primary,
            CloseButtonText = "Cancel"
        };

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            new PasswordVault().Remove(PasswordCredential);
            _owner.WebCredentials.Remove(this);
            _owner.RefreshDisplayedWebCredentials(_owner.WebCredentialSearchQuery);
        }
    }

    [RelayCommand]
    public async Task EditCredentialAsync()
    {
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Save",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Edit web credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        var sp = new StackPanel() { Spacing = 8 };

        var urlBox = new TextBox() { Header = "URL", Text = Url };
        var usernameBox = new TextBox() { Header = "Username", Text = Username };
        var passwordBox = new PasswordBox() { Header = "Password", Password = Password };

        urlBox.TextChanged += UrlBox_TextChanged;
        usernameBox.TextChanged += UrlBox_TextChanged;
        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

        sp.Children.Add(urlBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var vault = new PasswordVault();
            var properties = PasswordCredential?.Properties;

            if (PasswordCredential != null)
            {
                try { vault.Remove(PasswordCredential); } catch { }
            }

            var newCredential = new PasswordCredential(urlBox.Text, usernameBox.Text, passwordBox.Password);

            foreach (var property in properties!)
                newCredential.Properties.Add(property);

            vault.Add(newCredential);

            Url = urlBox.Text;
            Username = usernameBox.Text;
            Password = passwordBox.Password;
            PasswordCredential = newCredential;

            _owner.RefreshDisplayedWebCredentials(_owner.WebCredentialSearchQuery);
        }

        void UrlBox_TextChanged(object sender, TextChangedEventArgs e) => ValidateInputFields();
        void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => ValidateInputFields();

        void ValidateInputFields()
            => cd.IsPrimaryButtonEnabled =
                !string.IsNullOrWhiteSpace(urlBox.Text) &&
                !string.IsNullOrWhiteSpace(usernameBox.Text) &&
                !string.IsNullOrWhiteSpace(passwordBox.Password);
    }
}

/// <summary>
/// Windows credential (Win32 credential vault) wrapper class for data binding.
/// </summary>
internal partial class WindowsCredential : ObservableObject
{
    private readonly CredentialManagerViewModel _owner;

    public WindowsCredential(CredentialManagerViewModel owner)
    {
        _owner = owner;
    }

    [ObservableProperty] public partial string Url { get; set; } = string.Empty;

    [ObservableProperty] public partial string? Username { get; set; } = string.Empty;

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial string LastWritten { get; set; } = string.Empty;

    [ObservableProperty] public partial bool Persist { get; set; }

    [ObservableProperty] public partial CRED_TYPE Type { get; set; }

    [ObservableProperty] public partial string Password { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsPasswordAvailable { get; set; } = true;

    [ObservableProperty] public partial bool IsPasswordDisplayed { get; set; }

    [RelayCommand]
    public async Task ShowPasswordAsync()
    {
        if (_owner.IsAuthenticated)
        {
            IsPasswordDisplayed = true;
            return;
        }

        var availability = await UserConsentVerifier.CheckAvailabilityAsync();

        bool verified =
            availability != UserConsentVerifierAvailability.Available
                || (await UserConsentVerifier.RequestVerificationAsync(
                        "Verify your identity to view the password."
                    )) == UserConsentVerificationResult.Verified;

        if (!verified)
            return;

        _owner.IsAuthenticated = true;

        unsafe
        {
            PInvoke.SetWindowDisplayAffinity(
                new((void*)App.MainWindow!.GetWindowHandle()),
                WINDOW_DISPLAY_AFFINITY.WDA_MONITOR
            );
        }

        IsPasswordDisplayed = true;
    }

    [RelayCommand]
    public async Task DeleteCredentialAsync()
    {
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Delete",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Are you sure you want to delete this credential?",
            Content = $"This will delete the credential for {Url} with username {Username}. This action cannot be undone.",
            DefaultButton = ContentDialogButton.Primary,
            CloseButtonText = "Cancel"
        };

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            DeleteCredential();
    }

    private void DeleteCredential()
    {
        PInvoke.CredDelete(Name, Type);
        _owner.WindowsCredentials.Remove(this);
        _owner.RefreshDisplayedWindowsCredentials(_owner.WindowsCredentialSearchQuery);
    }

    [RelayCommand]
    public async Task EditCredentialAsync()
    {
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Save",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Edit Windows Credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        var sp = new StackPanel() { Spacing = 8 };

        var urlBox = new TextBox() { Header = "URL", Text = Url };
        var usernameBox = new TextBox() { Header = "Username", Text = Username };
        var passwordBox = new PasswordBox() { Header = "New Password (Optional)" };

        urlBox.TextChanged += InputChanged;
        usernameBox.TextChanged += InputChanged;

        sp.Children.Add(urlBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        ValidateInputFields();

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            UpdateWindowsCredential(urlBox.Text, usernameBox.Text, passwordBox.Password);

        void InputChanged(object sender, TextChangedEventArgs e) => ValidateInputFields();

        void ValidateInputFields() =>
            cd.IsPrimaryButtonEnabled =
                !string.IsNullOrWhiteSpace(urlBox.Text) &&
                !string.IsNullOrWhiteSpace(usernameBox.Text);
    }

    private unsafe void UpdateWindowsCredential(string newTargetName, string? newUsername, string newPassword)
    {
        if (!PInvoke.CredRead(Name, Type, out var credential))
        {
            return;
        }

        try
        {
            using StringPtr targetNamePtr = newTargetName;
            using StringPtr usernamePtr = newUsername ?? string.Empty;
            using StringPtr passwordPtr = newPassword;

            CREDENTIALW updatedCredential = *credential;

            updatedCredential.TargetName = new(targetNamePtr.GetChars());
            updatedCredential.UserName = new(usernamePtr.GetChars());
            updatedCredential.CredentialBlob = (byte*)passwordPtr.GetChars();
            updatedCredential.CredentialBlobSize = (uint)(newPassword.Length * sizeof(char));
            updatedCredential.Persist = Persist ? CRED_PERSIST.CRED_PERSIST_LOCAL_MACHINE : CRED_PERSIST.CRED_PERSIST_SESSION;

            if (Name != newTargetName)
            {
                PInvoke.CredDelete(Name, Type);
            }

            if (PInvoke.CredWrite(&updatedCredential, 0))
            {
                Url = CredentialManagerViewModel.ExtractTargetWithRegex(newTargetName);
                Username = newUsername;
                Name = newTargetName;
                LastWritten = DateTime.Now.ToString((IFormatProvider?)null);

                _owner.RefreshDisplayedWindowsCredentials(_owner.WindowsCredentialSearchQuery);
            }
        }
        finally
        {
            PInvoke.CredFree(credential);
        }
    }
}

internal partial class CredentialManagerViewModel : ObservableObject
{
    public ObservableCollection<WebCredential> DisplayedWebCredentials { get; } = [];
    public List<WebCredential> WebCredentials { get; set; } = [];

    [ObservableProperty] public partial string WebCredentialSearchQuery { get; set; } = string.Empty;

    public ObservableCollection<WindowsCredential> DisplayedWindowsCredentials { get; } = [];
    public List<WindowsCredential> WindowsCredentials { get; set; } = [];

    [ObservableProperty] public partial string WindowsCredentialSearchQuery { get; set; } = string.Empty;

    public bool IsAuthenticated { get; set; }

    public void ReloadState()
    {
        IsAuthenticated = false;
        WebCredentialSearchQuery = string.Empty;
        WindowsCredentialSearchQuery = string.Empty;

        DisplayedWebCredentials.Clear();
        DisplayedWindowsCredentials.Clear();

        var webCredentials = GetWebCredentials();
        WebCredentials = webCredentials;
        foreach (var cred in webCredentials)
            DisplayedWebCredentials.Add(cred);

        var windowsCredentials = GetWindowsCredentials();
        WindowsCredentials = windowsCredentials;
        foreach (var cred in windowsCredentials)
            DisplayedWindowsCredentials.Add(cred);

        RefreshDisplayedWebCredentials(string.Empty);
        RefreshDisplayedWindowsCredentials(string.Empty);
    }

    partial void OnWebCredentialSearchQueryChanged(string value)
        => RefreshDisplayedWebCredentials(value);

    partial void OnWindowsCredentialSearchQueryChanged(string value)
        => RefreshDisplayedWindowsCredentials(value);

    public void RefreshDisplayedWebCredentials(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            DisplayedWebCredentials.Clear();
            foreach (var cred in WebCredentials)
                DisplayedWebCredentials.Add(cred);
        }
        else
        {
            var filtered = WebCredentials.FindAll(c => c.Url.Contains(query, StringComparison.OrdinalIgnoreCase) || c.Username.Contains(query, StringComparison.OrdinalIgnoreCase));
            DisplayedWebCredentials.Clear();
            foreach (var cred in filtered)
                DisplayedWebCredentials.Add(cred);
        }
    }

    public void RefreshDisplayedWindowsCredentials(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            DisplayedWindowsCredentials.Clear();
            foreach (var cred in WindowsCredentials)
                DisplayedWindowsCredentials.Add(cred);
        }
        else
        {
            var filtered = WindowsCredentials.FindAll(c => c.Url.Contains(query, StringComparison.OrdinalIgnoreCase) || (c.Username?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
            DisplayedWindowsCredentials.Clear();
            foreach (var cred in filtered)
                DisplayedWindowsCredentials.Add(cred);
        }
    }

    [RelayCommand]
    public async Task AddWebCredentialAsync()
    {
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Add",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Add web credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        var sp = new StackPanel() { Spacing = 8 };

        var urlBox = new TextBox() { Header = "URL" };
        var usernameBox = new TextBox() { Header = "Username" };
        var passwordBox = new PasswordBox() { Header = "Password" };

        urlBox.TextChanged += UrlBox_TextChanged;
        usernameBox.TextChanged += UrlBox_TextChanged;
        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

        sp.Children.Add(urlBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var vault = new PasswordVault();
            var credential = new PasswordCredential(urlBox.Text, usernameBox.Text, passwordBox.Password);
            vault.Add(credential);

            var newCred = new WebCredential(this)
            {
                Url = urlBox.Text,
                Username = usernameBox.Text,
                Password = passwordBox.Password,
                PasswordCredential = credential
            };

            WebCredentials.Add(newCred);
            RefreshDisplayedWebCredentials(WebCredentialSearchQuery);
        }

        void UrlBox_TextChanged(object sender, TextChangedEventArgs e) => ValidateInputFields();
        void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => ValidateInputFields();

        void ValidateInputFields()
            => cd.IsPrimaryButtonEnabled =
                !string.IsNullOrWhiteSpace(urlBox.Text) &&
                !string.IsNullOrWhiteSpace(usernameBox.Text) &&
                !string.IsNullOrWhiteSpace(passwordBox.Password);
    }

    [RelayCommand]
    public async Task AddWindowsCredentialAsync()
    {
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Add",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Add Windows Credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        var sp = new StackPanel() { Spacing = 8 };

        var targetNameBox = new TextBox() { Header = "Target Name" };
        var usernameBox = new TextBox() { Header = "Username" };
        var passwordBox = new PasswordBox() { Header = "Password" };

        targetNameBox.TextChanged += InputChanged;
        usernameBox.TextChanged += InputChanged;
        passwordBox.PasswordChanged += PasswordChanged;

        sp.Children.Add(targetNameBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            AddWindowsCredential(targetNameBox.Text, usernameBox.Text, passwordBox.Password);

        void InputChanged(object sender, TextChangedEventArgs e) => ValidateInputFields();
        void PasswordChanged(object sender, RoutedEventArgs e) => ValidateInputFields();

        void ValidateInputFields()
            => cd.IsPrimaryButtonEnabled =
                !string.IsNullOrWhiteSpace(targetNameBox.Text) &&
                !string.IsNullOrWhiteSpace(usernameBox.Text) &&
                !string.IsNullOrWhiteSpace(passwordBox.Password);
    }

    private unsafe void AddWindowsCredential(string targetName, string username, string password)
    {
        using StringPtr targetNamePtr = targetName;
        using StringPtr usernamePtr = username;
        using StringPtr passwordPtr = password;

        CREDENTIALW cred = new CREDENTIALW()
        {
            Type = CRED_TYPE.CRED_TYPE_GENERIC,
            TargetName = new(targetNamePtr.GetChars()),
            UserName = new(usernamePtr.GetChars()),
            CredentialBlob = (byte*)passwordPtr.GetChars(),
            CredentialBlobSize = (uint)(password.Length * sizeof(char)),
            Persist = CRED_PERSIST.CRED_PERSIST_LOCAL_MACHINE,
        };

        if (PInvoke.CredWrite(&cred, 0))
        {
            WindowsCredentials.Add(new WindowsCredential(this)
            {
                Name = targetName,
                Url = ExtractTargetWithRegex(targetName),
                Username = username,
                Password = password,
                Type = cred.Type,
                Persist = true,
                LastWritten = DateTime.Now.ToString((IFormatProvider?)null)
            });

            RefreshDisplayedWindowsCredentials(WindowsCredentialSearchQuery);
        }
    }

    public List<WebCredential> GetWebCredentials()
    {
        var credentialsList = new List<WebCredential>();

        try
        {
            var credentialList = new PasswordVault().RetrieveAll();

            foreach (var cred in credentialList)
            {
                var credItem = new WebCredential(this);

                try
                {
                    cred.RetrievePassword();
                    credItem.Password = cred.Password;
                }
                catch
                {
                    credItem.IsPasswordAvailable = false;
                }

                credItem.Url = cred.Resource;
                credItem.Username = cred.UserName;
                credItem.PasswordCredential = cred;
                credentialsList.Add(credItem);
            }
        }
        catch
        {
        }

        return credentialsList;
    }

    public unsafe List<WindowsCredential> GetWindowsCredentials()
    {
        if (!PInvoke.CredEnumerate(null, out uint count, out CREDENTIALW** creds))
            return [];

        var list = new List<WindowsCredential>((int)count);
        try
        {
            for (int i = 0; i < count; i++)
            {
                var c = creds[i];

                string password = string.Empty;
                bool isPasswordAvailable = false;

                try
                {
                    if (c->CredentialBlob != null && c->CredentialBlobSize > 0)
                    {
                        password = Marshal.PtrToStringUni((nint)c->CredentialBlob, (int)c->CredentialBlobSize / sizeof(char)) ?? string.Empty;
                        isPasswordAvailable = true;
                    }
                }
                catch
                {
                    // Some Windows credentials protect or restrict blob reading based on token/integrity level
                    password = string.Empty;
                    isPasswordAvailable = false;
                }

                list.Add(new WindowsCredential(this)
                {
                    Url = ExtractTargetWithRegex(c->TargetName.ToString()),
                    Username = c->UserName.ToString(),
                    Persist = c->Persist switch
                    {
                        CRED_PERSIST.CRED_PERSIST_NONE => false,
                        _ => true
                    },
                    LastWritten = (c->LastWritten.dwHighDateTime == 0 && c->LastWritten.dwLowDateTime == 0)
                        ? string.Empty
                        : TryGetFileTime(c->LastWritten),
                    Type = c->Type,
                    Name = c->TargetName.ToString(),
                    Password = password,
                    IsPasswordAvailable = isPasswordAvailable
                });
            }
        }
        finally
        {
            PInvoke.CredFree(creds);
        }

        return list;
    }

    public static string ExtractTargetWithRegex(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return TargetCleanupRegex().Replace(input, string.Empty);
    }

    private static string TryGetFileTime(global::System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        try
        {
            long fileTime = ((long)ft.dwHighDateTime << 32) | (ft.dwLowDateTime & 0xFFFFFFFFL);
            if (fileTime <= 0) return string.Empty;
            return DateTime.FromFileTime(fileTime).ToString((IFormatProvider?)null);
        }
        catch
        {
            return string.Empty;
        }
    }

    [GeneratedRegex(@"^[^:]+:[^=]+=")]
    private static partial Regex TargetCleanupRegex();
}