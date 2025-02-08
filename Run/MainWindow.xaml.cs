using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graphics.Display;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Rebound.Run.Helpers;
using Rebound.Run.Languages;
using Windows.Storage.Pickers;
using Windows.System;
using WinUIEx;

#pragma warning disable IDE0044 // Add readonly modifier
#nullable enable

namespace Rebound.Run;

public sealed partial class MainWindow : WindowEx
{
    public double Scale()
    {
        try
        {
            return DisplayInformation.CreateForWindowId(AppWindow.Id).RawPixelsPerViewPixel;
        }
        catch
        {
            return 0;
        }
    }

    public MainWindow()
    {
        this?.InitializeComponent();
        StringTable.ReadLanguage();
        this.MoveAndResize(15 * Scale(), (WindowsDisplayAPI.Display.GetDisplays().ToList<WindowsDisplayAPI.Display>()[0].CurrentSetting.Resolution.Height - (295 / Scale())) / Scale(), 450, 235);
        IsMinimizable = false;
        IsMaximizable = false;
        IsResizable = false;
        AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        this.SetIcon($"{AppContext.BaseDirectory}/Assets/RunBox.ico");
        Title = StringTable.AppTitle;
        SystemBackdrop = new MicaBackdrop();
        Load();
        //LoadRunHistory();
    }

    public async void Load()
    {
        await Task.Delay(100);
        RunBox.Focus(FocusState.Keyboard);
    }

    private void LoadRunHistory(bool clear = false)
    {
        var runMRUPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";
        var runMRUKey = Registry.CurrentUser.OpenSubKey(runMRUPath);

        if (runMRUKey != null)
        {
            // Read the MRUList to determine the order of the entries
            var mruList = runMRUKey.GetValue("MRUList")?.ToString();
            if (mruList != null)
            {
                List<string> runHistory = [];

                // Iterate over each character in the MRUList to get the entries in order
                foreach (var entry in mruList)
                {
                    var entryValue = runMRUKey.GetValue(entry.ToString())?.ToString();
                    if (!string.IsNullOrEmpty(entryValue))
                    {
                        // Remove the '/1' suffix if it exists
                        if (entryValue.EndsWith("\\1"))
                        {
                            entryValue = entryValue[..^2];
                            // Same as entryValue = entryValue.Substring(0, entryValue.Length - 2);
                        }
                        if (clear == false)
                        {
                            runHistory.Add(entryValue);
                        }
                        else
                        {
                            _ = runHistory.Remove(entryValue);
                            runHistory.Clear();
                        }
                    }
                }

                // Display the ordered entries in the ListBox
                RunBox.ItemsSource = runHistory;
                RunBox.SelectedIndex = 0;
            }

            runMRUKey.Close();
        }
    }

    public async Task Run(bool runLegacy = false, bool admin = false)
    {
        var newEntry = RunBox.Text.ToString();

        if (newEntry.Contains("://") == true)
        {
            await Launcher.LaunchUriAsync(new Uri(newEntry));
            Close();
            return;
        }

        switch (RunBox.Text.ToString().ToLower())
        {
            case "":
                {
                    return;
                }
            case "settings":
                {
                    if (!runLegacy)
                    {
                        await Launcher.LaunchUriAsync(new Uri("ms-settings:///"));
                    }
                    else
                    {
                        await RunPowershell("control", ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "dfrgui" or "dfrgui.exe" or @"c:\windows\system32\dfrgui.exe" or "rdfrgui" or "rdfrgui.exe":
                {
                    var path = @"C:\Rebound11\rdfrgui.exe";
                    if (File.Exists(path) && !runLegacy)
                    {
                        await RunPowershell(path, ArgsBox.Text, admin);
                        return;
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "control" or "control.exe" or @"c:\windows\system32\control.exe" or "rcontrol" or "rcontrol.exe":
                {
                    var path = @"C:\Rebound11\rcontrol.exe";
                    if (File.Exists(path) && !runLegacy)
                    {
                        await RunPowershell(path, ArgsBox.Text, admin);
                        return;
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "tpm" or "tpm.msc" or @"c:\windows\system32\tpm.msc" or "rtpm" or "rtpm.exe":
                {
                    var path = @"C:\Rebound11\rtpm.exe";
                    if (File.Exists(path) && !runLegacy)
                    {
                        await RunPowershell(path, ArgsBox.Text, admin);
                        return;
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "cleanmgr" or "cleanmgr.exe" or @"c:\windows\system32\cleanmgr.exe" or "rcleanmgr" or "rcleanmgr.exe":
                {
                    var path = @"C:\Rebound11\rcleanmgr.exe";
                    if (File.Exists(path) && !runLegacy)
                    {
                        await RunPowershell(path, ArgsBox.Text, admin);
                        return;
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "osk" or "osk.exe" or @"c:\windows\system32\osk.exe" or "rosk" or "rosk.exe":
                {
                    var path = @"C:\Rebound11\rosk.exe";
                    if (File.Exists(path) && !runLegacy)
                    {
                        await RunPowershell(path, ArgsBox.Text, admin);
                        return;
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "useraccountcontrolsettings" or "useraccountcontrolsettings.exe" or @"c:\windows\system32\useraccountcontrolsettings.exe" or "ruacsettings" or "ruacsettings.exe":
                {
                    var path = @"C:\Rebound11\ruacsettings.exe";
                    if (File.Exists(path) && !runLegacy)
                    {
                        await RunPowershell(path, ArgsBox.Text, admin);
                        return;
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "winver" or "winver.exe" or @"c:\windows\system32\winver.exe" or "rwinver" or "rwinver.exe":
                {
                    var path = @"C:\Rebound11\rwinver.exe";
                    if (File.Exists(path) && !runLegacy)
                    {
                        await RunPowershell(path, ArgsBox.Text, admin);
                        return;
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "taskmgr" or "taskmgr.exe" or @"c:\windows\system32\taskmgr.exe":
                {
                    if (runLegacy == true)
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            Arguments = "taskmgr -d"
                        };

                        if (admin == true)
                        {
                            startInfo.Verb = "runas";
                        }

                        try
                        {
                            var res = Process.Start(startInfo);
                            if (res != null)
                            {
                                await res.WaitForExitAsync();
                            }
                            if (res?.ExitCode == 0)
                            {
                                Close();
                            }
                        }
                        catch (Exception)
                        {
                            await ShowMessageDialogAsync("Error");
                        }
                    }
                    else
                    {
                        await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    }
                    Close();
                    return;
                }
            case "run" or "rrun" or "rrun.exe":
                {
                    if (runLegacy == true)
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            Arguments = "(New-Object -ComObject \"Shell.Application\").FileRun()"
                        };

                        if (admin == true)
                        {
                            startInfo.Verb = "runas";
                        }

                        try
                        {
                            await ShowMessageDialogAsync("Warning", "Warning");
                            var res = Process.Start(startInfo);
                            Close();
                            Process.GetCurrentProcess().Kill();
                        }
                        catch (Exception)
                        {
                            await ShowMessageDialogAsync("Warning");
                        }
                    }
                    else
                    {
                        await ShowMessageDialogAsync("Warning", "Error");
                        return;
                    }
                    Close();
                    return;
                }
            default:
                {
                    await RunPowershell(RunBox.Text, ArgsBox.Text, admin);
                    return;
                }
        }
    }

    public async Task RunPowershell(string fileLocation, string arguments, bool admin)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Handle arguments with spaces by wrapping them in double quotes
        var runBoxText = fileLocation;
        var argsBoxText = arguments;

        startInfo.Arguments = !string.IsNullOrWhiteSpace(argsBoxText)
            ? $"Start-Process -FilePath '{runBoxText}' -ArgumentList '{argsBoxText}'"
            : $"Start-Process -FilePath '{runBoxText}' ";

        // Handle running as administrator
        if (admin)
        {
            startInfo.Arguments += " -Verb RunAs";
            startInfo.Verb = "runas";
        }

        try
        {
            var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }

            if (process?.ExitCode == 0)
            {
                Close();
            }
            else
            {
                throw new Exception($"Process exited with code {process?.ExitCode}");
            }
        }
        catch (Exception)
        {
            await ShowMessageDialogAsync("The system cannot find the file specified or the command line arguments are invalid.", "Error");
        }
    }

    private async void SplitButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        var newEntry = RunBox.Text;

        await Run();

        var runMRUPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";
        var runMRUKey = Registry.CurrentUser.OpenSubKey(runMRUPath, true);

        if (runMRUKey != null)
        {
            // Read the current MRUList
            var mruList = runMRUKey.GetValue("MRUList")?.ToString();
            if (mruList != null)
            {
                // Check for duplicates and remove the existing entry if found
                char? existingEntryKey = null;
                foreach (var entry in mruList)
                {
                    var entryValue = runMRUKey.GetValue(entry.ToString())?.ToString();
                    if (entryValue != null && entryValue.StartsWith(newEntry))
                    {
                        existingEntryKey = entry;
                        break;
                    }
                }

                if (existingEntryKey.HasValue)
                {
                    // Remove the existing entry
                    mruList = mruList.Replace(existingEntryKey.Value.ToString(), string.Empty);
                }

                // Determine the new entry key
                var newEntryKey = 'a';
                if (mruList.Length > 0)
                {
                    newEntryKey = (char)(mruList[0] + 1);
                }

                // Add the new entry to the registry
                runMRUKey.SetValue(newEntryKey.ToString(), newEntry);

                // Update the MRUList
                mruList = newEntryKey + mruList;
                runMRUKey.SetValue("MRUList", mruList);

                runMRUKey.Close();

                // Reload the Run history to refresh the ListBox
                LoadRunHistory();
            }
        }
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) => await Run(true);

    private async void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e) => await Run();

    private async void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e) => await Run(false, true);

    private async void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e) => await Run(true, true);

    private HashSet<VirtualKey> PressedKeys = [];

    private async void RunBox_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape &&
            !PressedKeys.Contains(VirtualKey.Control) &&
            !PressedKeys.Contains(VirtualKey.Menu) &&
            !PressedKeys.Contains(VirtualKey.Shift))
        {
            Close();
            return;
        }
        else if (e.Key == VirtualKey.Enter &&
            PressedKeys.Contains(VirtualKey.Control) &&
            PressedKeys.Contains(VirtualKey.Menu) &&
            PressedKeys.Contains(VirtualKey.Shift))
        {
            await Run(true, true);
            return;
        }
        else if (e.Key == VirtualKey.Enter &&
            PressedKeys.Contains(VirtualKey.Control) &&
            !PressedKeys.Contains(VirtualKey.Menu) &&
            PressedKeys.Contains(VirtualKey.Shift))
        {
            await Run(false, true);
            return;
        }
        else if (e.Key == VirtualKey.Enter &&
            !PressedKeys.Contains(VirtualKey.Control) &&
            PressedKeys.Contains(VirtualKey.Menu) &&
            !PressedKeys.Contains(VirtualKey.Shift))
        {
            await Run(true, false);
            return;
        }
        else if (e.Key == VirtualKey.Enter &&
            !PressedKeys.Contains(VirtualKey.Control) &&
            !PressedKeys.Contains(VirtualKey.Menu) &&
            !PressedKeys.Contains(VirtualKey.Shift))
        {
            await Run();
            return;
        }

        PressedKeys.Remove(e.Key);

        CheckRunBoxText();
    }

    public void CheckRunBoxText()
    {
        if (!string.IsNullOrWhiteSpace(RunBox.Text))
        {
            RunButton.IsEnabled = true;
            VisualStateManager.GoToState(RunButton, "Normal", true);
            return;
        }
        RunButton.IsEnabled = false;
        VisualStateManager.GoToState(RunButton, "Disabled", true);
    }

    private void Button_Click(object sender, RoutedEventArgs e) => Close();

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        // Create a file picker
        var openPicker = new FileOpenPicker();

        // See the sample code below for how to make the window accessible from the App class.
        var window = this;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the file picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your file picker
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.CommitButtonText = "Select file to run";
        openPicker.FileTypeFilter.Add(".exe");
        openPicker.FileTypeFilter.Add(".pif");
        openPicker.FileTypeFilter.Add(".com");
        openPicker.FileTypeFilter.Add(".bat");
        openPicker.FileTypeFilter.Add(".cmd");
        openPicker.FileTypeFilter.Add("*");

        // Open the picker for the user to pick a file
        var file = await openPicker.PickSingleFileAsync();
        if (file != null)
        {
            RunBox.Text = file.Path;
        }
        else
        {

        }

    }

    private void RunBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        PressedKeys.Add(e.Key);

        CheckRunBoxText();
    }

    private async void WindowEx_Activated(object sender, WindowActivatedEventArgs args)
    {
        await Task.Delay(5);

        if (GroupPolicyHelper.IsGroupPolicyEnabled(GroupPolicyHelper.EXPLORER_GROUP_POLICY_PATH, "NoRun", 1) == true)
        {
            Close();
        }
    }

    private void RunBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => CheckRunBoxText();

    private void RunBox_LostFocus(object sender, RoutedEventArgs e) => CheckRunBoxText();

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        App.MainWindow = null;
    }
}
