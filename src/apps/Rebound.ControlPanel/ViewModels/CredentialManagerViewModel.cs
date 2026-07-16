// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Native.TerraFX;
using Rebound.Core.Native.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.Security.Credentials;
using Windows.Security.Credentials.UI;
using WinUIEx;
using static TerraFX.Interop.Windows.CRED;
using static TerraFX.Interop.Windows.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.ControlPanel.ViewModels;

/// <summary>
/// Web credential (WinRT credential vault) wrapper class for data binding.
/// </summary>
internal partial class WebCredential : ObservableObject
{
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
        // If the user is already authenticated, don't show the prompt again, just display the password
        if (CredentialManagerViewModel.Singleton.IsAuthenticated)
        {
            IsPasswordDisplayed = true;
            return;
        }

        // Checks if the machine actually has Windows Hello or passwords configured
        var availability = await UserConsentVerifier.CheckAvailabilityAsync();

        // If the user hates security then so shall be it
        bool verified =
            availability != UserConsentVerifierAvailability.Available 
                || (await UserConsentVerifier.RequestVerificationAsync(
                        "Verify your identity to view the password."
                    )) == UserConsentVerificationResult.Verified;

        // If the user isn't verified, return immediately
        if (!verified)
            return;

        // Since the user is verified, set the singleton's IsAuthenticated to true so they don't have to verify again
        CredentialManagerViewModel.Singleton.IsAuthenticated = true;

        // Prevent window capture so screen recording software doesn't get a peek at the password
        unsafe
        {
            SetWindowDisplayAffinity(
                new((void*)App.MainWindow!.GetWindowHandle()),
                WDA_MONITOR
            );
        }

        IsPasswordDisplayed = true;
    }

    [RelayCommand]
    public async Task DeleteCredentialAsync()
    {
        // Confirmaton dialog
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Delete",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Are you sure you want to delete this credential?",
            Content = $"This will delete the credential for {Url} with username {Username}. This action cannot be undone.",
            DefaultButton = ContentDialogButton.Primary,
            CloseButtonText = "Cancel"
        };

        // Request confirmation
        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            new PasswordVault().Remove(PasswordCredential);
            CredentialManagerViewModel.Singleton.WebCredentials.Remove(this);
            CredentialManagerViewModel.Singleton.RefreshDisplayedWebCredentials(CredentialManagerViewModel.Singleton.WebCredentialSearchQuery);
        }
    }

    [RelayCommand]
    public async Task EditCredentialAsync()
    {
        // Edit dialog
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Save",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Edit web credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        // Parent container for the textboxes
        var sp = new StackPanel() { Spacing = 8 };

        // Input fields
        var urlBox = new TextBox()
        {
            Header = "URL",
            Text = Url
        };
        var usernameBox = new TextBox()
        {
            Header = "Username",
            Text = Username
        };
        var passwordBox = new PasswordBox()
        {
            Header = "Password",
            Password = Password
        };

        // Input validation fields
        urlBox.TextChanged += UrlBox_TextChanged;
        usernameBox.TextChanged += UrlBox_TextChanged;
        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

        // Build the tree
        sp.Children.Add(urlBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        // Show the dialog and wait for the result
        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var vault = new PasswordVault();

            // Cache properties
            var properties = PasswordCredential?.Properties;

            // Remove the old credential from the WinRT vault if it exists (it should, but just in case)
            if (PasswordCredential != null)
            {
                try { vault.Remove(PasswordCredential); } catch { /* Doesn't exist, maybe deleted from an external source */ }
            }

            // Create and add the entirely new credential
            var newCredential = new PasswordCredential(urlBox.Text, usernameBox.Text, passwordBox.Password);

            // Set the cached properties to the new credential if they exist
            foreach (var property in properties!)
                newCredential.Properties.Add(property);

            vault.Add(newCredential);

            // Update the object's properties
            Url = urlBox.Text;
            Username = usernameBox.Text;
            Password = passwordBox.Password;
            PasswordCredential = newCredential;

            // Refresh the displayed credentials
            CredentialManagerViewModel.Singleton.RefreshDisplayedWebCredentials(CredentialManagerViewModel.Singleton.WebCredentialSearchQuery);
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
    [ObservableProperty] public partial string Url { get; set; } = string.Empty;

    [ObservableProperty] public partial string? Username { get; set; } = string.Empty;

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial string LastWritten { get; set; } = string.Empty;

    [ObservableProperty] public partial bool Persist { get; set; }

    [ObservableProperty] public partial uint Type { get; set; }

    [ObservableProperty] public partial string Password { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsPasswordAvailable { get; set; } = true;

    [ObservableProperty] public partial bool IsPasswordDisplayed { get; set; }

    /// <summary>
    /// Request user authentication and display the password if successful.
    /// </summary>
    [RelayCommand]
    public async Task ShowPasswordAsync()
    {
        // If the user is already authenticated, don't show the prompt again, just display the password
        if (CredentialManagerViewModel.Singleton.IsAuthenticated)
        {
            IsPasswordDisplayed = true;
            return;
        }

        // Checks if the machine actually has Windows Hello or passwords configured
        var availability = await UserConsentVerifier.CheckAvailabilityAsync();

        // If the user hates security then so shall be it
        bool verified =
            availability != UserConsentVerifierAvailability.Available
                || (await UserConsentVerifier.RequestVerificationAsync(
                        "Verify your identity to view the password."
                    )) == UserConsentVerificationResult.Verified;

        // If the user isn't verified, return immediately
        if (!verified)
            return;

        // Since the user is verified, set the singleton's IsAuthenticated to true so they don't have to verify again
        CredentialManagerViewModel.Singleton.IsAuthenticated = true;

        // Prevent window capture so screen recording software doesn't get a peek at the password
        unsafe
        {
            SetWindowDisplayAffinity(
                new((void*)App.MainWindow!.GetWindowHandle()),
                WDA_MONITOR
            );
        }

        IsPasswordDisplayed = true;
    }

    [RelayCommand]
    public async Task DeleteCredentialAsync()
    {
        // Confirmaton dialog
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Delete",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Are you sure you want to delete this credential?",
            Content = $"This will delete the credential for {Url} with username {Username}. This action cannot be undone.",
            DefaultButton = ContentDialogButton.Primary,
            CloseButtonText = "Cancel"
        };

        // Request confirmation
        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            DeleteCredential();
    }

    private unsafe void DeleteCredential()
    {
        using ManagedPtr<char> namePtr = Name;
        CredDeleteW(namePtr, Type, 0);
        CredentialManagerViewModel.Singleton.WindowsCredentials.Remove(this);
        CredentialManagerViewModel.Singleton.RefreshDisplayedWindowsCredentials(CredentialManagerViewModel.Singleton.WindowsCredentialSearchQuery);
    }

    [RelayCommand]
    public async Task EditCredentialAsync()
    {
        // Edit dialog
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Save",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Edit Windows Credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        // Parent container for the textboxes
        var sp = new StackPanel() { Spacing = 8 };

        // Input fields
        var urlBox = new TextBox() { Header = "URL", Text = Url };
        var usernameBox = new TextBox() { Header = "Username", Text = Username };
        var passwordBox = new PasswordBox() { Header = "New Password (Optional)" };

        // Input validation fields
        urlBox.TextChanged += InputChanged;
        usernameBox.TextChanged += InputChanged;

        // Build the tree
        sp.Children.Add(urlBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        // Trigger initial validation
        ValidateInputFields();

        // Show the dialog and wait for the result
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
        // Read the existing credential from Windows to preserve all metadata
        using ManagedPtr<char> originalNamePtr = Name;
        CREDENTIALW* pOriginalCred = null;

        // Use the exact same Type it originally had
        if (!CredReadW(originalNamePtr, Type, 0, &pOriginalCred))
        {
            // Fail early if the credential was deleted underneath us
            return;
        }

        try
        {
            // Managed pointer stuff
            using ManagedPtr<char> targetNamePtr = newTargetName;
            using ManagedPtr<char> usernamePtr = newUsername ?? string.Empty;
            using ManagedPtr<char> passwordPtr = newPassword;

            // Clone the original structure, overriding only what the user edited
            CREDENTIALW updatedCredential = *pOriginalCred;

            updatedCredential.TargetName = targetNamePtr;
            updatedCredential.UserName = usernamePtr;
            updatedCredential.CredentialBlob = (byte*)(char*)passwordPtr;
            updatedCredential.CredentialBlobSize = (uint)(newPassword.Length * sizeof(char));
            updatedCredential.Persist = Persist ? 2u : 1u;

            // If the user changed the unique TargetName, we must delete the old record first
            if (Name != newTargetName)
            {
                CredDeleteW(originalNamePtr, Type, 0);
            }

            // Write the updated structure
            if (CredWriteW(&updatedCredential, 0))
            {
                // Sync UI state
                Url = CredentialManagerViewModel.ExtractTargetWithRegex(newTargetName);
                Username = newUsername;
                Name = newTargetName;

                // Update the last written timestamp
                LastWritten = DateTime.Now.ToString((IFormatProvider?)null);

                CredentialManagerViewModel.Singleton.RefreshDisplayedWindowsCredentials(
                    CredentialManagerViewModel.Singleton.WindowsCredentialSearchQuery
                );
            }
        }
        finally
        {
            // Free the memory allocated by CredReadW
#pragma warning disable CA1508 // Avoid dead conditional code
            if (pOriginalCred != null)
            {
                CredFree(pOriginalCred);
            }
#pragma warning restore CA1508 // Avoid dead conditional code
        }
    }
}

internal partial class CredentialManagerViewModel : ObservableObject
{
    // Web credentials

    /// <summary>
    /// Web credentials filtered by search query
    /// </summary>
    public ObservableCollection<WebCredential> DisplayedWebCredentials { get; } = [];

    /// <summary>
    /// Unfiltered list of web credentials
    /// </summary>
    public List<WebCredential> WebCredentials { get; set; } = [];

    [ObservableProperty] public partial string WebCredentialSearchQuery { get; set; } = string.Empty;

    // Windows credentials

    /// <summary>
    /// Windows credentials filtered by search query
    /// </summary>
    public ObservableCollection<WindowsCredential> DisplayedWindowsCredentials { get; } = [];

    /// <summary>
    /// Unfiltered list of Windows credentials
    /// </summary>
    public List<WindowsCredential> WindowsCredentials { get; set; } = [];

    [ObservableProperty] public partial string WindowsCredentialSearchQuery { get; set; } = string.Empty;

    // Other stuff

    /// <summary>
    /// Whether or not the user has verified their identity for viewing passwords.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    public static CredentialManagerViewModel Singleton { get; } = new();

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
        // Add dialog
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Add",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Add web credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        // Parent container for the textboxes
        var sp = new StackPanel() { Spacing = 8 };

        // Input fields
        var urlBox = new TextBox() { Header = "URL" };
        var usernameBox = new TextBox() { Header = "Username" };
        var passwordBox = new PasswordBox() { Header = "Password" };

        // Input validation fields
        urlBox.TextChanged += UrlBox_TextChanged;
        usernameBox.TextChanged += UrlBox_TextChanged;
        passwordBox.PasswordChanged += PasswordBox_PasswordChanged;

        // Build the tree
        sp.Children.Add(urlBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        // Show the dialog and wait for the result
        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // Create the brand new credential
            var vault = new PasswordVault();
            var credential = new PasswordCredential(urlBox.Text, usernameBox.Text, passwordBox.Password);
            vault.Add(credential);

            // Add the item to the credentials list
            WebCredentials.Add(new WebCredential()
            {
                Url = urlBox.Text,
                Username = usernameBox.Text,
                Password = passwordBox.Password,
                PasswordCredential = credential
            });

            // Refresh the displayed credentials
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
        // Add dialog
        var cd = new ContentDialog()
        {
            PrimaryButtonText = "Add",
            XamlRoot = App.MainWindow?.Content.XamlRoot,
            Title = "Add Windows Credential",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "Cancel"
        };

        // Parent container for the textboxes
        var sp = new StackPanel() { Spacing = 8 };

        // Input fields
        var targetNameBox = new TextBox() { Header = "Target Name" };
        var usernameBox = new TextBox() { Header = "Username" };
        var passwordBox = new PasswordBox() { Header = "Password" };

        // Input validation fields
        targetNameBox.TextChanged += InputChanged;
        usernameBox.TextChanged += InputChanged;
        passwordBox.PasswordChanged += PasswordChanged;

        // Build the tree
        sp.Children.Add(targetNameBox);
        sp.Children.Add(usernameBox);
        sp.Children.Add(passwordBox);
        cd.Content = sp;

        // Show the dialog and wait for the result
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
        // Create the brand new credential
        using ManagedPtr<char> targetNamePtr = targetName;
        using ManagedPtr<char> usernamePtr = username;
        using ManagedPtr<char> passwordPtr = password;

        CREDENTIALW cred = new CREDENTIALW()
        {
            Type = CRED_TYPE_GENERIC,
            TargetName = targetNamePtr,
            UserName = usernamePtr,
            CredentialBlob = (byte*)(char*)passwordPtr,
            CredentialBlobSize = (uint)(password.Length * sizeof(char)),
            Persist = CRED_PERSIST_LOCAL_MACHINE,
        };

        // Write the credential
        if (CredWriteW(&cred, 0))
        {
            // Add the item to the credentials list
            WindowsCredentials.Add(new WindowsCredential()
            {
                Name = targetName,
                Url = ExtractTargetWithRegex(targetName),
                Username = username,
                Password = password,
                Type = cred.Type,
                Persist = true,
                LastWritten = DateTime.Now.ToString((IFormatProvider?)null)
            });

            // Refresh the displayed credentials
            RefreshDisplayedWindowsCredentials(WindowsCredentialSearchQuery);
        }
    }

    public static List<WebCredential> GetWebCredentials()
    {
        var credentialsList = new List<WebCredential>();

        try
        {
            // Grab all the credentials available
            var credentialList = new PasswordVault().RetrieveAll();

            foreach (var cred in credentialList)
            {
                var credItem = new WebCredential();

                // Handle password separately
                try
                {
                    // Credentials are lazy-loaded for security; this fills the Password field
                    cred.RetrievePassword();
                    credItem.Password = cred.Password;
                }
                catch
                {
                    // Password unavailable
                    credItem.IsPasswordAvailable = false;
                }

                // Populate the rest
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
        uint count = 0;
        CREDENTIALW** creds;

        // Enumerate all credentials in the Windows Credential Manager
        if (!CredEnumerateW((char*)null, CRED_ENUMERATE_ALL_CREDENTIALS, &count, &creds))
            return []; // Return none if empty

        var list = new List<WindowsCredential>((int)count);
        for (int i = 0; i < count; i++)
        {
            var c = creds[i];

            // Hell
            list.Add(new WindowsCredential
            {
                Url = ExtractTargetWithRegex(Marshal.PtrToStringUni((nint)c->TargetName)!),
                Username = Marshal.PtrToStringUni((nint)c->UserName)!,
                Persist = new BOOL((int)c->Persist),
                LastWritten = c->LastWritten.ToDateTime().ToString((IFormatProvider?)null),
                Type = c->Type,
                Name = Marshal.PtrToStringUni((nint)c->TargetName)!,
                Password = (c->CredentialBlob != null && c->CredentialBlobSize > 0)
                    ? Marshal.PtrToStringUni((nint)c->CredentialBlob, (int)c->CredentialBlobSize / sizeof(char))!
                    : string.Empty,
                IsPasswordAvailable = c->CredentialBlob != null && c->CredentialBlobSize > 0
            });
        }
        CredFree(creds);

        return list;
    }

    public static string ExtractTargetWithRegex(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Strip "namespace:attribute="
        return TargetCleanupRegex().Replace(input, string.Empty);
    }

    // Matches: start of string -> non-colons -> a colon -> non-equals -> an equals sign
    [GeneratedRegex(@"^[^:]+:[^=]+=")]
    private static partial Regex TargetCleanupRegex();
}