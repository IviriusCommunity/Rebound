using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using Windows.Graphics.Printing;
using PrintDocument = Microsoft.UI.Xaml.Printing.PrintDocument;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.ExplorerPhotoPrintingDialog;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public async Task<byte[]> RenderUIElementToBytes(FrameworkElement element)
    {
        // Create a RenderTargetBitmap object
        var renderTargetBitmap = new RenderTargetBitmap();

        // Render the element
        await renderTargetBitmap.RenderAsync(element);

        // Get the pixel buffer
        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

        // Convert the pixel buffer to byte array
        var pixels = pixelBuffer.ToArray();

        // You can now use these bytes to send to the printer
        return pixels;
    }

    public static string GetFirstPrinterName()
    {
        // Get all available printers
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            if (printer.Contains("EPSON"))
            {
                return printer;
            }

            Debug.WriteLine(printer);
            // Return the name of the first available printer
            //return printer;
        }

        return null; // If no printer is found
    }

    private async void myButton_Click(object sender, RoutedEventArgs e)
    {
        myButton.Content = "Clicked";
        // Render the element to a byte array
        var elementBytes = await RenderUIElementToBytes(fff);

        // Allocate memory for the byte array
        var pUnmanagedBytes = Marshal.AllocCoTaskMem(elementBytes.Length);
        Marshal.Copy(elementBytes, 0, pUnmanagedBytes, elementBytes.Length);

        // Send the bytes to the printer

        _ = RawPrintingService.SendStringToPrinter(GetFirstPrinterName(), "Eeeeeeeeeeeeeeeeeee");
        return;
        var result = RawPrintingService.SendBytesToPrinter(GetFirstPrinterName(), pUnmanagedBytes, elementBytes.Length);

        /*RegisterPrint();
        try
        {
            // Show print UI
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
        }
        catch
        {
            // Printing cannot proceed at this time
            ContentDialog noPrintingDialog = new ContentDialog()
            {
                XamlRoot = (sender as Button).XamlRoot,
                Title = "Printing error",
                Content = "\nSorry, printing can' t proceed at this time.",
                PrimaryButtonText = "OK"
            };
            await noPrintingDialog.ShowAsync();
        }*/
    }

    private PrintManager printMan
    {
        get; set;
    }
    private PrintDocument printDoc
    {
        get; set;
    }
    private IPrintDocumentSource printDocSource
    {
        get; set;
    }

    private void RegisterPrint()
    {
        // Register for PrintTaskRequested event
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        printMan = PrintManagerInterop.GetForWindow(hWnd);
        printMan.PrintTaskRequested += PrintTaskRequested;

        // Build a PrintDocument and register for callbacks
        printDoc = new PrintDocument();
        printDocSource = printDoc.DocumentSource;
        printDoc.Paginate += Paginate;
        printDoc.GetPreviewPage += GetPreviewPage;
        printDoc.AddPages += AddPages;
    }

    private void Paginate(object sender, PaginateEventArgs e) =>
        // As I only want to print one Rectangle, so I set the count to 1
        printDoc.SetPreviewPageCount(1, PreviewPageCountType.Final);

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        // Provide a UIElement as the print preview.
        //printDoc.SetPreviewPage(e.PageNumber, REBItems);
    }

    private void AddPages(object sender, AddPagesEventArgs e) =>
        /*var children = REBItems.Children.ToList();  // Create a copy of the children

foreach (FrameworkElement ui in children)
{
//REBItems.Children.Remove(ui);  // Remove the child from the stack panel
printDoc.AddPage(ui);          // Add the child to the print document
//await Task.Delay(200);
}*/

        // Indicate that all of the print pages have been provided
        printDoc.AddPagesComplete();

    private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
    {
        printMan.PrintTaskRequested -= PrintTaskRequested;
        // Notify the user when the print operation fails.
        if (args.Completion == PrintTaskCompletion.Failed)
        {
            _ = DispatcherQueue.TryEnqueue(async () =>
            {
                var noPrintingDialog = new ContentDialog()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "Printing error",
                    Content = "\nSorry, failed to print.",
                    PrimaryButtonText = "OK"
                };
                _ = await noPrintingDialog.ShowAsync();
            });
        }
    }

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        // Create the PrintTask.
        // Defines the title and delegate for PrintTaskSourceRequested
        var printTask = args.Request.CreatePrintTask("Print", PrintTaskSourceRequrested);
        printTask.IsPreviewEnabled = false;

        // Handle PrintTask.Completed to catch failed print jobs
        printTask.Completed += PrintTaskCompleted;
    }

    private void PrintTaskSourceRequrested(PrintTaskSourceRequestedArgs args) =>
        // Set the document source.
        args.SetSource(printDocSource);
}

public class RawPrintingService
{
    // Structure and API declarions:
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    // SendBytesToPrinter()
    // When the function is given a printer name and an unmanaged array
    // of bytes, the function sends those bytes to the print queue.
    // Returns true on success, false on failure.
    public static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, int dwCount)
    {
        _ = new IntPtr(0);
        var di = new DOCINFOA();
        var bSuccess = false; // Assume failure unless you specifically succeed.

        di.pDocName = "RAW Document";
        // Win7
        di.pDataType = "RAW";
        nint hPrinter;
        // Win8+
        // di.pDataType = "XPS_PASS";

        // Open the printer.
        if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
        {
            // Start a document.
            if (StartDocPrinter(hPrinter, 1, di))
            {
                // Start a page.
                if (StartPagePrinter(hPrinter))
                {
                    // Write your bytes.
                    bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out _);
                    _ = EndPagePrinter(hPrinter);
                }
                _ = EndDocPrinter(hPrinter);
            }
            _ = ClosePrinter(hPrinter);
        }
        // If you did not succeed, GetLastError may give more information
        // about why not.
        if (bSuccess == false)
        {
            _ = Marshal.GetLastWin32Error();
        }
        return bSuccess;
    }

    public static bool SendFileToPrinter(string szPrinterName, string szFileName)
    {
        // Open the file.
        var fs = new FileStream(szFileName, FileMode.Open);
        // Create a BinaryReader on the file.
        var br = new BinaryReader(fs);
        // Dim an array of bytes big enough to hold the file's contents.
        _ = new byte[fs.Length];
        // Your unmanaged pointer.
        _ = new IntPtr(0);
        int nLength;

        nLength = Convert.ToInt32(fs.Length);
        // Read the contents of the file into the array.
        var bytes = br.ReadBytes(nLength);
        // Allocate some unmanaged memory for those bytes.
        var pUnmanagedBytes = Marshal.AllocCoTaskMem(nLength);
        // Copy the managed byte array into the unmanaged array.
        Marshal.Copy(bytes, 0, pUnmanagedBytes, nLength);
        // Send the unmanaged bytes to the printer.
        var bSuccess = SendBytesToPrinter(szPrinterName, pUnmanagedBytes, nLength);
        // Free the unmanaged memory that you allocated earlier.
        Marshal.FreeCoTaskMem(pUnmanagedBytes);
        fs.Close();
        fs.Dispose();
        return bSuccess;
    }
    public static bool SendStringToPrinter(string szPrinterName, string szString)
    {
        IntPtr pBytes;
        int dwCount;
        // How many characters are in the string?
        dwCount = szString.Length;
        // Assume that the printer is expecting ANSI text, and then convert
        // the string to ANSI text.
        pBytes = Marshal.StringToCoTaskMemAnsi(szString);
        // Send the converted ANSI string to the printer.
        _ = SendBytesToPrinter(szPrinterName, pBytes, dwCount);
        Marshal.FreeCoTaskMem(pBytes);
        return true;
    }
}