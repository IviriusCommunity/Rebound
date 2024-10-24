using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using Rebound.Helpers;
using Rebound.Run.Helpers;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using WinUIEx;
using File = System.IO.File;
using Task = System.Threading.Tasks.Task;

#nullable enable
#pragma warning disable CA2211 // Non-constant fields should not be visible

namespace Rebound.Run
{
    public partial class App : Application
    {
        private readonly SingleInstanceDesktopApp _singleInstanceApp;

        public App()
        {
            this?.InitializeComponent();

            _singleInstanceApp = new SingleInstanceDesktopApp("Rebound.Run");
            _singleInstanceApp.Launched += OnSingleInstanceLaunched;
        }

        public static WindowEx? BackgroundWindow
        {
            get; set;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _singleInstanceApp?.Launch(args.Arguments);
        }

        private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
        {
            if (e.IsFirstLaunch)
            {
                await LaunchWork();
            }
            else
            {
                // Get the current process
                Process currentProcess = Process.GetCurrentProcess();

                // Start a new instance of the application
                if (currentProcess.MainModule != null) Process.Start(currentProcess.MainModule.FileName);

                // Terminate the current process
                currentProcess?.Kill();
                return;
            }
        }

        public async Task<int> LaunchWork()
        {
            CreateShortcut();

            // Initialize the background window
            InitializeBackgroundWindow();

            MainWindow = new MainWindow();

            // Delay for task execution
            await Task.Delay(5);

            RunBoxReplace();

            // If started with the "STARTUP" argument, exit early
            if (IsStartupArgumentPresent() == true)
            {
                return 0;
            }

            // Try to activate the main window
            await ActivateMainWindowAsync();

            return 0;
        }

        public async void RunBoxReplace()
        {
            await Task.Delay(50);
            if (LegacyRunBoxExists() == true)
            {
                try
                {
                    MainWindow.Activate();
                    MainWindow.BringToFront();
                }
                catch
                {
                    MainWindow = new MainWindow();
                    MainWindow.Activate();
                    MainWindow.BringToFront();
                }
            }

            RunBoxReplace();
        }

        // Importing the user32.dll to use GetWindowThreadProcessId
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static bool IsExplorerWindow(IntPtr hWnd)
        {
            // Get the process ID of the window handle (hWnd)
            GetWindowThreadProcessId(hWnd, out uint processId);

            try
            {
                // Get the process by ID
                Process process = Process.GetProcessById((int)processId);

                // Check if the process name is "explorer"
                return process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // If there is an issue retrieving the process, assume it's not Explorer
                return false;
            }
        }

        public static bool LegacyRunBoxExists()
        {
            // Find the window with the title "Run"
            IntPtr hWnd = Win32Helper.FindWindow(null, "Run");
            //IntPtr hWndtaskmgr2 = Win32Helper.FindWindow("#32770", "Create new task");

            if (hWnd != IntPtr.Zero && IsExplorerWindow(hWnd) == true)
            {
                // Send WM_CLOSE to close the window
                bool sent = Win32Helper.PostMessage(hWnd, Win32Helper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                Debug.Write(IsExplorerWindow(hWnd));
                if (sent == true)
                {
                    return true;
                }
            }
            return false;
        }

        private static void InitializeBackgroundWindow()
        {
            try
            {
                BackgroundWindow = new()
                {
                    SystemBackdrop = new TransparentTintBackdrop(),
                    IsMaximizable = false
                };
                BackgroundWindow?.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
                BackgroundWindow?.SetWindowStyle(WindowStyle.Visible);
                BackgroundWindow?.Activate();
                BackgroundWindow?.MoveAndResize(0, 0, 0, 0);
                BackgroundWindow?.Minimize();
                BackgroundWindow?.SetWindowOpacity(0);
            }
            catch
            {

            }
        }

        private static bool IsStartupArgumentPresent()
        {
            return string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("STARTUP");
        }

        private static async Task ActivateMainWindowAsync()
        {
            try
            {
                MainWindow?.Activate();
            }
            catch
            {
                // Handle activation error
            }

            await Task.Delay(100);  // Ensure main window focus

            try
            {
                MainWindow?.Activate();  // Reactivate to ensure focus
            }
            catch
            {
                // Handle activation error
            }

            MainWindow?.Show();  // Ensure window is visible

            // Bring to front explicitly
            ((WindowEx?)MainWindow)?.BringToFront();
        }

        private void CreateShortcut()
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string oldShortcutPath = System.IO.Path.Combine(startupFolderPath, "Rebound.Run.lnk");
            try
            {
                File.Delete(oldShortcutPath);
            }
            catch
            {

            }
            string shortcutPath = System.IO.Path.Combine(startupFolderPath, "Rebound.RunStartup.lnk");
            if (!File.Exists(shortcutPath))
            {
                WshShell shell = new();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.Description = "Rebound Run";
                shortcut.TargetPath = "C:\\Rebound11\\rrunSTARTUP.exe";
                shortcut?.Save();
            }
        }

        public static WindowEx? MainWindow;
    }
}
