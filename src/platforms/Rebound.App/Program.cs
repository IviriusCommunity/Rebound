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

namespace Rebound
{
    internal class Program
    {
        private static TerraFX.Interop.Windows.ComPtr<IDispatcherQueueController> s_dqController;

        static private App? _xamlApp;

        [STAThread]
        static unsafe void Main(string[] args)
        {
            RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
            //EnsureDispatcherQueue();
            _xamlApp = new App();
        }

        private unsafe static void EnsureDispatcherQueue()
        {
            // If a DispatcherQueue already exists for this thread, nothing to do.
            if (DispatcherQueue.GetForCurrentThread() != null)
                return;

            if (s_dqController.Get() is null)
            {
                var opts = new DispatcherQueueOptions
                {
                    dwSize = (uint)sizeof(DispatcherQueueOptions),
                    threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT,
                    apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA
                };

                int hr = CreateDispatcherQueueController(opts, s_dqController.GetAddressOf());
                if (hr < 0) Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
