using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.Win32;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;
using static TerraFX.Interop.Windows.WS;
using static TerraFX.Interop.WinRT.WinRT;

namespace Rebound.Shell.ExperienceHost
{
    internal class Program
    {
        public static BlockingCollection<Action> _actions = new();

        [STAThread]
        static unsafe void Main(string[] args)
        {
            // If this is the first instance, start the application
            RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
            _ = new App();

            while (true)
            {
                if (_actions.TryTake(out var action, Timeout.Infinite))
                {
                    try { action(); }
                    catch { }
                }
            }
        }
    }
}
