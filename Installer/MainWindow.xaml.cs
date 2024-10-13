using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WinUIEx;

namespace ReboundHubInstaller
{
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            this.SetIcon($"{AppContext.BaseDirectory}\\Assets\\ReboundHub.ico");
            this.Title = $"Install Rebound Hub";
            this.SetWindowSize(475, 335);
            this.CenterOnScreen();
            this.IsMaximizable = false;
            this.IsResizable = false;
            SetDarkMode(this);
        }

        // Get the directory of the current process
        string GetCurrentProcessDirectory()
        {
            // Get the current process
            Process currentProcess = Process.GetCurrentProcess();

            // Get the path of the executable file
            string executablePath = currentProcess.MainModule.FileName;

            // Get the directory of the executable file
            string directoryPath = Path.GetDirectoryName(executablePath);

            return directoryPath;
        }

        private void WindowEx_Closed(object sender, WindowEventArgs args)
        {
            Process.GetCurrentProcess().Kill();
        }

        [DllImport("dwmapi.dll", SetLastError = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        public static void SetDarkMode(WindowEx window)
        {
            int i = 1;
            if (App.Current.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light)
            {
                i = 0;
            }
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            DwmSetWindowAttribute(hWnd, 20, ref i, sizeof(int));
            CheckTheme();
            async void CheckTheme()
            {
                await Task.Delay(100);
                try
                {
                    int i = 1;
                    if (App.Current.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light)
                    {
                        i = 0;
                    }
                    IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    DwmSetWindowAttribute(hWnd, 20, ref i, sizeof(int));
                    CheckTheme();
                }
                catch
                {

                }
            }
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            Install.IsEnabled = false;
            try
            {
                // Load the certificate from file
                X509Certificate2 certificate = new X509Certificate2($"{AppContext.BaseDirectory}\\ReboundHub.cer");

                // Define the store location and name
                StoreLocation storeLocation = StoreLocation.LocalMachine;
                StoreName storeName = StoreName.Root;

                // Open the certificate store
                using (X509Store store = new X509Store(storeName, storeLocation))
                {
                    store.Open(OpenFlags.ReadWrite);

                    // Add the certificate to the store
                    store.Add(certificate);

                    // Close the store
                    store.Close();
                }
                await Task.Delay(50);
            }
            catch (System.Exception ex)
            {
                await Task.Delay(50);
            }
            Progress.Value++;

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = $@"{AppContext.BaseDirectory}\VCRedistx64.exe",
                    Arguments = "/quiet /norestart",  // Modify the arguments as needed
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();

                    // Optionally capture output or error messages
                    string output4 = await process.StandardOutput.ReadToEndAsync();
                    string error4 = await process.StandardError.ReadToEndAsync();

                    // Wait for the process to exit
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        throw new System.Exception($"Installer exited with code {process.ExitCode}. Error: {error4}");
                    }

                    // Optional: handle the output or error as needed
                    Debug.WriteLine($"Installer output: {output4}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Failed to install VCRUNTIME: {ex.Message}");
                // Handle exceptions as necessary, e.g., log the error or show a message to the user
            }

            Progress.Value++;

            if (Directory.Exists(@"C:\Rebound11Temp") != true)
            {
                Directory.CreateDirectory(@"C:\Rebound11Temp");
            }

            File.Copy($"{AppContext.BaseDirectory}\\Microsoft.WindowsAppRuntime.1.6-experimental2.msix", @"C:\Rebound11Temp\Runtime.msix", true);

            // Setup the process start info
            var procFolder = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Verb = "runas",                 // Run as administrator
                UseShellExecute = false,
                CreateNoWindow = true,// Required to redirect output
                Arguments = @$"-Command ""Add-AppxPackage -Path 'C:\Rebound11Temp\Runtime.msix'""",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Start the process
            var resFolder = Process.Start(procFolder);

            // Read output and errors
            string output = await resFolder.StandardOutput.ReadToEndAsync();
            string error = await resFolder.StandardError.ReadToEndAsync();

            // Wait for the process to exit
            await resFolder.WaitForExitAsync();
            Progress.Value++;

            File.Delete("C:\\Rebound11Temp\\Runtime.msix");

            File.Copy($"{AppContext.BaseDirectory}\\ReboundHub.msix", @"C:\Rebound11Temp\ReboundHub.msix", true);

            await Task.Delay(50);

            // Setup the process start info
            var procFolder2 = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Verb = "runas",                 // Run as administrator
                UseShellExecute = false,
                CreateNoWindow = true,// Required to redirect output
                Arguments = @$"-Command ""Add-AppxPackage -Path 'C:\Rebound11Temp\ReboundHub.msix'""",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Start the process
            var resFolder2 = Process.Start(procFolder2);

            // Read output and errors
            string output2 = await resFolder2.StandardOutput.ReadToEndAsync();
            string error2 = await resFolder2.StandardError.ReadToEndAsync();

            // Wait for the process to exit
            await resFolder2.WaitForExitAsync();
            Progress.Value++;

            File.Delete("C:\\Rebound11Temp\\ReboundHub.msix");

            await Task.Delay(500);

            // Setup the process start info
            var procFolder3 = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Verb = "runas",                 // Run as administrator
                UseShellExecute = false,
                CreateNoWindow = true,// Required to redirect output
                Arguments = @"Start-Process ""shell:AppsFolder\d6ef5e04-e9da-4e22-9782-8031af8beae7_yejd587sfa94t!App""",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Start the process
            var resFolder3 = Process.Start(procFolder3);

            // Read output and errors
            string output3 = await resFolder3.StandardOutput.ReadToEndAsync();
            string error3 = await resFolder3.StandardError.ReadToEndAsync();

            // Wait for the process to exit
            await resFolder2.WaitForExitAsync();

            if (Directory.Exists(@"C:\Rebound11Temp") == true)
            {
                Directory.Delete(@"C:\Rebound11Temp");
            }

            Close();
        }
    }
}
