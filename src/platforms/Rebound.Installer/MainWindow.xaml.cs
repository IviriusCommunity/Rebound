using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Rebound.Helpers.Windowing;
using WinUIEx;

namespace ReboundHubInstaller;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        this.SetWindowIcon($"{AppContext.BaseDirectory}\\Assets\\ReboundHubInstaller.ico");
        this.CenterOnScreen();
        ExtendsContentIntoTitleBar = true;
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args) => Process.GetCurrentProcess().Kill();

    [RelayCommand]
    public void CloseApp()
    {
        Close();
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        InstallingProgressRing.IsIndeterminate = true;
        CloseButton.IsEnabled = false;
        try
        {
            // Load the certificate from file
            var certificate = X509CertificateLoader.LoadCertificateFromFile($"{AppContext.BaseDirectory}\\Certificate.cer");

            // Define the store location and name
            var storeLocation = StoreLocation.LocalMachine;
            var storeName = StoreName.Root;

            // Open the certificate store
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                // Add the certificate to the store
                store.Add(certificate);

                // Close the store
                store.Close();
            }
            await Task.Delay(50);
        }
        catch (System.Exception)
        {
            await Task.Delay(50);
        }

        if (Directory.Exists(@"C:\ReboundTemp") != true)
        {
            _ = Directory.CreateDirectory(@"C:\ReboundTemp");
        }

        File.Copy($"{AppContext.BaseDirectory}\\Package.msix", @"C:\ReboundTemp\Package.msix", true);

        await Task.Delay(50);

        // Setup the process start info
        var procFolder2 = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Verb = "runas",                 // Run as administrator
            UseShellExecute = false,
            CreateNoWindow = true,// Required to redirect output
            Arguments = @$"-Command ""Add-AppxPackage -Path 'C:\ReboundTemp\Package.msix'""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Start the process
        var resFolder2 = Process.Start(procFolder2);

        // Read output and errors
        _ = await resFolder2.StandardOutput.ReadToEndAsync();
        _ = await resFolder2.StandardError.ReadToEndAsync();

        // Wait for the process to exit
        await resFolder2.WaitForExitAsync();

        File.Delete("C:\\ReboundTemp\\Package.msix");

        await Task.Delay(500);

        // Setup the process start info
        var procFolder3 = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Verb = "runas",                 // Run as administrator
            UseShellExecute = false,
            CreateNoWindow = true,// Required to redirect output
            Arguments = @"Start-Process ""shell:AppsFolder\Rebound.Hub_yejd587sfa94t!App""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Start the process
        var resFolder3 = Process.Start(procFolder3);

        // Read output and errors
        _ = await resFolder3.StandardOutput.ReadToEndAsync();
        _ = await resFolder3.StandardError.ReadToEndAsync();

        // Wait for the process to exit
        await resFolder2.WaitForExitAsync();

        if (Directory.Exists(@"C:\ReboundTemp") == true)
        {
            Directory.Delete(@"C:\ReboundTemp");
        }

        Close();
    }
}