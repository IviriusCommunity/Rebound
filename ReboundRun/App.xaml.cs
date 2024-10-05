using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using ReboundRun.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using WinUIEx;
using File = System.IO.File;
using Task = System.Threading.Tasks.Task;

#nullable enable
#pragma warning disable CA2211 // Non-constant fields should not be visible

namespace ReboundRun
{
    public partial class App : Application
    {
        private readonly SingleInstanceDesktopApp _singleInstanceApp;

        public App()
        {
            this?.InitializeComponent();

            _singleInstanceApp = new SingleInstanceDesktopApp("REBOUNDRUN");
            _singleInstanceApp.Launched += OnSingleInstanceLaunched;
        }

        public static WindowEx? BackgroundWindow { get; set; }

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

            // Register any background tasks or hooks
            StartHook();

            // Delay for task execution
            await Task.Delay(5);

            // Check if Windows hotkey shortcuts are disabled by group policy
            if (GroupPolicyHelper.IsGroupPolicyEnabled(GroupPolicyHelper.EXPLORER_GROUP_POLICY_PATH, "NoWinKeys", 1) == true)
            {
                StopHook();
            }

            // If started with the "STARTUP" argument, exit early
            if (IsStartupArgumentPresent())
            {
                return 0;
            }

            // Try to activate the main window
            await ActivateMainWindowAsync();

            return 0;
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
            string oldShortcutPath = System.IO.Path.Combine(startupFolderPath, "ReboundRun.lnk");
            try
            {
                File.Delete(oldShortcutPath);
            }
            catch
            {

            }
            string shortcutPath = System.IO.Path.Combine(startupFolderPath, "ReboundRunStartup.lnk");
            if (!File.Exists(shortcutPath))
            {
                WshShell shell = new();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.Description = "Rebound Run";
                shortcut.TargetPath = "C:\\Rebound11\\rrunSTARTUP.exe";
                shortcut?.Save();
            }
        }

        public static void StartHook()
        {
            Win32Helper.keyboardProc = HookCallback;
            Win32Helper.hookId = SetHook(Win32Helper.keyboardProc);
        }

        public static void StopHook()
        {
            Win32Helper.UnhookWindowsHookEx(Win32Helper.hookId);
        }

        public static bool AllowClosingRunBox { get; set; } = true;

        private static IntPtr SetHook(Win32Helper.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                if (curProcess.MainModule != null)
                {
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        return Win32Helper.SetWindowsHookEx(Win32Helper.WH_KEYBOARD_LL, proc, Win32Helper.GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
            }
            return IntPtr.Zero;
        }

        private static bool winKeyPressed = false;
        private static bool rKeyPressed = false;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Check for keydown events
                if (wParam == Win32Helper.WM_KEYDOWN)
                {
                    // Check if Windows key is pressed
                    if (vkCode is Win32Helper.VK_LWIN or Win32Helper.VK_RWIN)
                    {
                        winKeyPressed = true;
                    }

                    // Check if 'R' key is pressed
                    if (vkCode is Win32Helper.VK_R)
                    {
                        rKeyPressed = true;

                        // If both Win and R are pressed, show the window
                        if (winKeyPressed)
                        {
                            ((WindowEx?)MainWindow)?.Show();
                            ((WindowEx?)MainWindow)?.BringToFront();
                            try
                            {
                                ((WindowEx?)MainWindow)?.Activate();
                            }
                            catch
                            {
                                MainWindow = new MainWindow();
                                MainWindow.Show();
                                ((WindowEx?)MainWindow)?.Activate();
                                ((WindowEx?)MainWindow)?.BringToFront();
                            }

                            // Prevent default behavior of Win + R
                            return 1;
                        }
                    }
                }

                // Check for keyup events
                if (wParam == Win32Helper.WM_KEYUP)
                {
                    // Check if Windows key is released
                    if (vkCode is Win32Helper.VK_LWIN or Win32Helper.VK_RWIN)
                    {
                        winKeyPressed = false;

                        // Suppress the Windows Start menu if 'R' is still pressed
                        if (rKeyPressed == true)
                        {
                            ForceReleaseWin();
                            return 1; // Prevent Windows menu from appearing
                        }
                    }

                    // Check if 'R' key is released
                    if (vkCode is Win32Helper.VK_R)
                    {
                        rKeyPressed = false;
                    }
                }
            }

            return Win32Helper.CallNextHookEx(Win32Helper.hookId, nCode, wParam, lParam);
        }

        public static async void ForceReleaseWin()
        {
            await Task.Delay(10);

            var inj = InputInjector.TryCreate();
            var info = new InjectedInputKeyboardInfo
            {
                VirtualKey = (ushort)VirtualKey.LeftWindows,
                KeyOptions = InjectedInputKeyOptions.KeyUp
            };
            var infoList = new[] { info };

            inj.InjectKeyboardInput(infoList);
        }

        public static Window? MainWindow;
    }
}
