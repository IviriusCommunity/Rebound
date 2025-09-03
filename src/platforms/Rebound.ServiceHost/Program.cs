using System;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;
using static TerraFX.Interop.Windows.WS;
using static TerraFX.Interop.WinRT.WinRT;
using Windows.System;
using Windows.Win32;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using System.Diagnostics;
using Rebound.ServiceHost;

namespace Rebound.ServiceHost
{ 
    internal class Program
    {
        [STAThread]
        static unsafe void Main(string[] args)
        {
            // Create a named mutex to check for existing instances
            using (Mutex mutex = new Mutex(true, "Rebound.ServiceHost", out bool isNewInstance))
            {
                if (isNewInstance)
                {
                    // If this is the first instance, start the application
                    RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
                    _ = new App();
                }
                else
                {
                    // If another instance is running, activate the existing instance
                    var processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                    foreach (var proc in processes)
                    {
                        if (proc.Id != Process.GetCurrentProcess().Id)
                        {
                            var a = proc.MainWindowHandle;
                            HWND localHandle = new HWND(&a);
                            if (localHandle != IntPtr.Zero)
                                SetForegroundWindow(localHandle);
                        }
                    }
                    Process.GetCurrentProcess().Kill();
                }
            }
        }
    }
}
