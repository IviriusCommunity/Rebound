using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Rebound.Forge;
using Rebound.Helpers.Windowing;
using Rebound.Modding.Instructions;
using Windows.Management.Deployment;
using WinUIEx;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace ReboundHubInstaller;

public sealed partial class MainWindow : WindowEx
{
    private readonly string reboundPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rebound");
    private readonly string tempPath;

    public MainWindow()
    {
        InitializeComponent();
        tempPath = Path.Combine(reboundPath, "Temp");
        this.SetWindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "ReboundHubInstaller.ico"));
        this.CenterOnScreen();
        ExtendsContentIntoTitleBar = true;
    }

    public ObservableCollection<ReboundAppInstructions> Instructions { get; } =
    [
        new WinverInstructions(),
        new OnScreenKeyboardInstructions(),
        new DiskCleanupInstructions(),
        new UserAccountControlSettingsInstructions(),
        new ControlPanelInstructions(),
        // new ShellInstructions() // To be reimplemented Soon™
    ];

    private void WindowEx_Closed(object sender, WindowEventArgs args) => Process.GetCurrentProcess().Kill();

    [RelayCommand]
    public void CloseApp() => Close();

    [RelayCommand]
    public async Task RemoveReboundAsync()
    {
        ShowProgress("Removing Rebound...");
        await Task.Delay(1000);

        foreach (var instruction in Instructions)
            await instruction.Uninstall();

        ReboundWorkingEnvironment.RemoveFolder();
        ReboundWorkingEnvironment.RemoveTasksFolder();
        Close();
    }

    [RelayCommand]
    public async Task RepairReboundHubAsync()
    {
        ShowProgress("Repairing Rebound Hub...");
        await Task.Delay(1000);

        try
        {
            PrepareDirectories();
            await InstallMsixAsync();
        }
        catch
        {
            DescriptionBox.Text = "Couldn't repair Rebound Hub.";
            await Task.Delay(500);
        }

        Close();
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        ShowProgress("Installing Rebound Hub...");
        await Task.Delay(1000);

        PrepareDirectories();

        await InstallCertificateAsync();
        await InstallMsixAsync();
        await InstallDotNetRuntimeAsync();

        await CleanupAsync();

        DescriptionBox.Text = "Done!";
        InstallingProgressRing.Value = 100;
        await Task.Delay(500);
        Close();
    }

    private void ShowProgress(string message)
    {
        InstallingProgressRing.IsIndeterminate = true;
        ButtonsStackPanel.Visibility = Visibility.Collapsed;
        DescriptionBox.Visibility = Visibility.Visible;
        DescriptionBox.Text = message;
    }

    private void PrepareDirectories()
    {
        Directory.CreateDirectory(reboundPath);
        Directory.CreateDirectory(tempPath);
    }

    private async Task InstallCertificateAsync()
    {
        try
        {
            ShowProgress("Installing Rebound Hub certificate in Trusted Root Certification Authorities...");
            InstallingProgressRing.Value = 5;
            await Task.Delay(50);

            var certSource = Path.Combine(AppContext.BaseDirectory, "Certificate.cer");
            var certDest = Path.Combine(tempPath, "Certificate.cer");
            File.Copy(certSource, certDest, true);

            var certificate = X509CertificateLoader.LoadCertificateFromFile(certDest);
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
        }
        catch
        {
            DescriptionBox.Text = "Couldn't install certificate.";
            await Task.Delay(1000);
        }
    }

    private async Task InstallMsixAsync()
    {
        try
        {
            ShowProgress("Installing Rebound Hub MSIX package. This may take a while...");
            InstallingProgressRing.Value = 8;
            await Task.Delay(50);

            var msixSource = Path.Combine(AppContext.BaseDirectory, "Package.msix");
            var msixTemp = Path.Combine(tempPath, "Package.msix");
            File.Copy(msixSource, msixTemp, true);

            var packageManager = new PackageManager();
            await packageManager.AddPackageAsync(new Uri(msixTemp), null, DeploymentOptions.ForceUpdateFromAnyVersion);

            File.Delete(msixTemp);
        }
        catch
        {
            DescriptionBox.Text = "Couldn't install package.";
            await Task.Delay(50);
        }
    }

    private async Task InstallDotNetRuntimeAsync()
    {
        try
        {
            ShowProgress("Installing .NET 9.0 Runtime. This may take a while...");
            InstallingProgressRing.Value = 42;
            await Task.Delay(50);

            var runtimeSource = Path.Combine(AppContext.BaseDirectory, "dotNET9Runtime.exe");
            var runtimeTemp = Path.Combine(tempPath, "dotNET9Runtime.exe");
            File.Copy(runtimeSource, runtimeTemp, true);

            var psi = new ProcessStartInfo
            {
                FileName = runtimeTemp,
                Arguments = "/quiet /norestart",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process != null)
                await process.WaitForExitAsync();

            File.Delete(runtimeTemp);
        }
        catch
        {
            DescriptionBox.Text = "Couldn't install .NET 9.0 Runtime.";
            await Task.Delay(50);
        }
    }

    private async Task CleanupAsync()
    {
        ShowProgress("Cleaning up...");
        InstallingProgressRing.Value = 94;
        await Task.Delay(1000);

        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }
}