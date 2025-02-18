using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Shell.Desktop;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[ObservableObject]
public sealed partial class DesktopPage : Page
{
    [ObservableProperty]
    public partial ObservableCollection<DesktopItem> Items { get; set; } = new();

    public DesktopPage()
    {
        this.InitializeComponent();
    }
    public static async Task GetDesktopFilesAsync(ObservableCollection<DesktopItem> items)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        if (Directory.Exists(desktopPath))
        {
            // Get both files and folders
            string[] files = Directory.GetFiles(desktopPath);
            string[] folders = Directory.GetDirectories(desktopPath);

            // Create tasks for loading files and folders concurrently
            var tasks = files.Concat(folders)
                             .Select(path => LoadFileAsync(path, items))
                             .ToList();

            await Task.WhenAll(tasks);
        }
    }

    private static async Task LoadFileAsync(string filePath, ObservableCollection<DesktopItem> items)
    {
        DesktopItem desktopFile = new DesktopItem(filePath);
        await desktopFile.LoadThumbnailAsync();

        // Add directly to ObservableCollection for real-time UI update
        items.Add(desktopFile);
    }

    private async void LoadingGrid_Loaded(object sender, RoutedEventArgs e)
    {
        string path = GetWallpaperPath();
        if (System.IO.File.Exists(path))
        {
            wallpaper.Source = new BitmapImage(new Uri(path));
        }

        await Task.Delay(50);

        // Clear items before loading to avoid adding duplicates
        Items.Clear();

        // Run GetDesktopFilesAsync to add items one-by-one
        await GetDesktopFilesAsync(Items);

        foreach (var item in Items)
        {
            var contentControl = new ContentControl()
            {
                ContentTemplate = (DataTemplate)Resources["DesktopItemTemplate"], // Use ContentTemplate instead of Template
                Content = item // Set the content to the current item
            };
            contentControl.PointerPressed += ContentControl_PointerPressed;
            contentControl.PointerMoved += ContentControl_PointerMoved;
            contentControl.PointerReleased += ContentControl_PointerReleased;

            if (item.X is -1 || item.Y is -1)

            {
                var freeSpot = FindFreeSpot();
                if (freeSpot.X != -1 && freeSpot.Y != -1)
                {
                    // Assign the free spot coordinates to the item
                    Canvas.SetLeft(contentControl, freeSpot.X);
                    Canvas.SetTop(contentControl, freeSpot.Y);

                    item.X = freeSpot.X;
                    item.Y = freeSpot.Y;

                    // Add it to the canvas
                    fff.Children.Add(contentControl);
                }
                else
                {

                }
            }
            else
            {
                // Assign the free spot coordinates to the item
                Canvas.SetLeft(contentControl, (double)item.X);
                Canvas.SetTop(contentControl, (double)item.Y);

                // Add it to the canvas
                fff.Children.Add(contentControl);
            }
            // Optionally set any additional properties, such as position on the Canvas
            /*Canvas.SetLeft(contentControl, item.X); // Example of setting X position
            Canvas.SetTop(contentControl, item.Y);  // Example of setting Y position

            fff.Children.Add(contentControl); // Add the ContentControl to the Canvas*/
        }

        LoadingGrid.Visibility = Visibility.Collapsed;
    }

    private void ContentControl_PointerReleased(object sender, PointerRoutedEventArgs e)
    {

    }

    private void ContentControl_PointerMoved(object sender, PointerRoutedEventArgs e)
    {

    }

    private void ContentControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private const int SPI_GETDESKWALLPAPER = 0x0073;
    private const int MAX_PATH = 260;

    // P/Invoke declaration for SystemParametersInfo to get the wallpaper
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

    // P/Invoke to Find the ProgMan window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    // P/Invoke to set the parent of the window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    // ProgMan class name (desktop window)
    private const string ProgManClassName = "Progman";


    // P/Invoke to ShowWindow function
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Constants for ShowWindow
    private const int SW_MAXIMIZE = 3;

    public static string GetWallpaperPath()
    {
        var wallpaperPath = new StringBuilder(MAX_PATH);
        bool result = SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaperPath, 0);

        if (result)
        {
            return wallpaperPath.ToString();
        }
        else
        {
            // Handle error case where SystemParametersInfo fails
            throw new InvalidOperationException("Failed to retrieve the desktop wallpaper path.");
        }
    }

    private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        //eee.ShowAt(aaa);
        //fff.SelectedItem = null;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    // Constants for SHChangeNotify
    private const uint SHCNE_UPDATEITEM = 0x00002000;
    private const uint SHCNF_PATH = 0x00000001;

    public static void RefreshThumbnailCache(string filePath)
    {
        IntPtr pathPtr = Marshal.StringToHGlobalAuto(filePath); // Convert the string to IntPtr

        try
        {
            // Send notification to Explorer to update the thumbnail cache for the file
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATH, pathPtr, IntPtr.Zero);
        }
        finally
        {
            // Free the unmanaged memory
            Marshal.FreeHGlobal(pathPtr);
        }
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        Process.GetCurrentProcess().Kill();
    }

    private async void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
    {
        // Clear items before loading to avoid adding duplicates
        Items.Clear();

        // Run GetDesktopFilesAsync to add items one-by-one
        await GetDesktopFilesAsync(Items);

        LoadingGrid.Visibility = Visibility.Collapsed;
    }

    private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // Get the Grid that was double-tapped
        var grid = sender as Grid;

        // Retrieve the DesktopFile from the DataContext
        var desktopFile = grid?.DataContext as DesktopItem;
        if (desktopFile == null) return;

        // Open the file
        Process.Start(new ProcessStartInfo
        {
            FileName = desktopFile.FilePath,
            UseShellExecute = true // Ensure the shell is used to launch the file
        });
    }

    private void wallpaper_Tapped(object sender, TappedRoutedEventArgs e)
    {
        /*fff.SelectedItem = null;*/
    }

    private void wallpaper_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        /*fff.SelectedItem = null;*/
        SelectionBorder.Margin = new(0);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
        _selectionOn = false;
    }

    bool _selectionOn = false;
    double initialMarginX;
    double initialMarginY;


    private void fff_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control && !_isInItem)
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
        }
        _selectionOn = true;
        initialMarginX = e.GetCurrentPoint(fff).Position.X;
        initialMarginY = e.GetCurrentPoint(fff).Position.Y;
        SelectionBorder.Margin = new(e.GetCurrentPoint(fff).Position.X, e.GetCurrentPoint(fff).Position.Y, 0, 0);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
    }

    Windows.Foundation.Point lastPos;

    double left;
    double top;

    private void fff_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var Pos = e.GetCurrentPoint(fff).Position;
        try
        {
            if (e.Pointer.IsInContact && _selectionOn && !_isDragging)
            {
                left = SelectionBorder.Margin.Left;
                top = SelectionBorder.Margin.Top;

                if (Pos.X < initialMarginX)
                {
                    left = Pos.X;
                    SelectionBorder.Width = initialMarginX - Pos.X;
                }
                else
                {
                    left = initialMarginX;
                    SelectionBorder.Width = Pos.X - lastPos.X - initialMarginX;

                }
                if (Pos.Y < initialMarginY)
                {
                    top = Pos.Y;
                    SelectionBorder.Height = initialMarginY - Pos.Y;
                }
                else
                {
                    top = initialMarginY;
                    SelectionBorder.Height = Pos.Y - lastPos.Y - initialMarginY;
                }

                SelectionBorder.Margin = new(left, top, 0, 0);
            }
        }
        catch
        {

        }
    }

    private void Grid_DragLeave(object sender, DragEventArgs e)
    {

    }

    private void fff_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
        e.DragUIOverride.Caption = "Move on Desktop";
    }

    private Point oldPoint;

    private async void fff_Drop(object sender, DragEventArgs e)
    {
        _isDragging = false;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                var point = e.GetPosition(fff); // Get drop position
                var freeSpot = FindFreeSpotCoordinates(point.X, point.Y) ?? FindFreeSpot(); // Find the first available spot

                List<DesktopItem> existingFiles = new();
                List<DesktopItem> newFiles = new();

                foreach (var storageFile in items)
                {
                    var desktopFile = Items.FirstOrDefault(item => item.FilePath.Equals(storageFile.Path, StringComparison.OrdinalIgnoreCase));

                    if (desktopFile == null) // New file or folder
                    {
                        if (storageFile is StorageFile file)
                        {
                            var desktopFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                            StorageFile? destinationFile = null;

                            try
                            {
                                destinationFile = await desktopFolder.GetFileAsync(Path.GetFileName(storageFile.Path));
                            }
                            catch
                            {
                                destinationFile = await desktopFolder.CreateFileAsync(Path.GetFileName(storageFile.Path), CreationCollisionOption.ReplaceExisting);
                            }

                            await file.CopyAndReplaceAsync(destinationFile);

                            desktopFile = new DesktopItem(destinationFile.Path);
                            await desktopFile.LoadThumbnailAsync();
                            newFiles.Add(desktopFile);
                        }
                        else if (storageFile is StorageFolder folder)
                        {
                            var desktopFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                            StorageFolder? destinationFolder = null;

                            try
                            {
                                destinationFolder = await desktopFolder.GetFolderAsync(Path.GetFileName(storageFile.Path));
                            }
                            catch
                            {
                                destinationFolder = await desktopFolder.CreateFolderAsync(Path.GetFileName(storageFile.Path), CreationCollisionOption.ReplaceExisting);
                            }

                            await CopyStorageFolderAsync(folder, destinationFolder);

                            desktopFile = new DesktopItem(destinationFolder.Path);
                            await desktopFile.LoadThumbnailAsync();
                            newFiles.Add(desktopFile);
                        }

                        Items.Add(desktopFile);
                    }
                    else
                    {
                        existingFiles.Add(desktopFile);
                    }
                }

                // Move existing files together
                if (existingFiles.Count > 0)
                {
                    foreach (var file in existingFiles)
                    {
                        var contentControl = GetContentControlFromDesktopFile(file);

                        // Eliminate pointer position errors
                        var oldPointerPos = FindSpotCoordinates(oldPoint.X, oldPoint.Y);

                        var oldItemRelativePos = new Point(oldPoint.X - oldPoint.X % oldPointerPos.X, oldPoint.Y - oldPoint.Y % oldPointerPos.Y); // Old position of each item inside the grid relative to the position of the item the pointer was on
                        var oldItemPos = new Point(file.X, file.Y); // Old position of the item the pointer was on

                        // Obtain the differences
                        var cellWidthDifference = oldItemPos.X - oldItemRelativePos.X;
                        var cellHeightDifference = oldItemPos.Y - oldItemRelativePos.Y;

                        // Eliminate pointer position errors
                        var newPointerPos = FindSpotCoordinates(point.X, point.Y);

                        var newItemPos = FindSpotCoordinates(point.X - point.X % newPointerPos.X + cellWidthDifference, point.Y - point.Y % newPointerPos.Y + cellHeightDifference); // Theoretical new position it could occupy

                        // If it can occupy it, proceed to occupy it
                        if (CheckIfSpotIsFree(newItemPos.X, newItemPos.Y))
                        {
                            // Apply new position in canvas
                            Canvas.SetLeft(contentControl, newItemPos.X);
                            Canvas.SetTop(contentControl, newItemPos.Y);

                            // Write new position to items
                            file.X = newItemPos.X;
                            file.Y = newItemPos.Y;
                        }
                        // If it can't, find the immediate next one
                        else
                        {
                            // Get the definitive position
                            var definitivePos = FindNextFreeSpot(point.X, point.Y);

                            // Apply new position in canvas
                            Canvas.SetLeft(contentControl, definitivePos.X);
                            Canvas.SetTop(contentControl, definitivePos.Y);

                            // Write new position to items
                            file.X = definitivePos.X;
                            file.Y = definitivePos.Y;
                        }

                    }
                }

                // Stack new files in next available spots
                foreach (var file in newFiles)
                {
                    freeSpot = FindNextFreeSpot(freeSpot.X, freeSpot.Y); // Find next available space

                    var contentControl = new ContentControl()
                    {
                        ContentTemplate = (DataTemplate)Resources["DesktopItemTemplate"],
                        Content = file
                    };
                    contentControl.PointerPressed += ContentControl_PointerPressed;
                    contentControl.PointerMoved += ContentControl_PointerMoved;
                    contentControl.PointerReleased += ContentControl_PointerReleased;

                    Canvas.SetLeft(contentControl, freeSpot.X);
                    Canvas.SetTop(contentControl, freeSpot.Y);

                    file.X = freeSpot.X;
                    file.Y = freeSpot.Y;

                    Items.Add(file);
                    fff.Children.Add(contentControl);
                }
            }
        }
    }

    public Point FindNextFreeSpot(double startX, double startY)
    {
        const int cellWidth = 82;
        const int cellHeight = 102;

        double x = Math.Floor(startX / cellWidth) * cellWidth;
        double y = Math.Floor(startY / cellHeight) * cellHeight;

        while (true) // Keep searching for the next available spot
        {
            bool spotTaken = Items.Any(item => item.X == x && item.Y == y);

            if (!spotTaken)
            {
                return new Point(x, y); // Return the first available spot found
            }

            // Move to the next slot
            y += cellHeight;
            if (y + cellHeight > fff.ActualHeight) // If we reach the end of the row, go to the next row
            {
                y = 0;
                x += cellWidth;
            }

            // Break out if we exceed the canvas size (optional safeguard)
            if (x + cellWidth > fff.ActualWidth)
            {
                Debug.WriteLine("No space available!");
                return new Point(-1, -1); // Fallback if no space found
            }
        }
    }

    public Point FindFreeSpot()
    {
        const int cellWidth = 82;
        const int cellHeight = 102;

        // Iterate over columns and rows to find a free spot
        for (int col = 0; col < (fff.ActualWidth - cellWidth) / cellWidth; col++)
        {
            for (int row = 0; row < (fff.ActualHeight - cellHeight) / cellHeight; row++)
            {
                // Calculate the potential spot (top-left corner of the "cell")
                double x = col * cellWidth;
                double y = row * cellHeight;

                // Check if the spot is occupied by any of the items
                bool spotTaken = false;
                foreach (var item in Items)
                {
                    // Get the item's position (assumes the item has a way to track position, e.g., a Canvas.Left/Top)
                    var itemPosition = new Point((float)item.X, (float)item.Y);

                    // Check if the item intersects with the potential spot
                    if (itemPosition.X == x && itemPosition.Y == y)
                    {
                        spotTaken = true;
                        break;
                    }
                }

                // If the spot is not taken, return it
                if (!spotTaken)
                {
                    return new Point(x, y);
                }
            }
        }

        // Return a default point (or indicate no free spot found) if needed
        return new Point(-1, -1);  // Or another way of signaling no space found
    }
    const int cellWidth = 82;
    const int cellHeight = 102;
    public bool CheckIfSpotIsFree(double x, double y)
    {


        // Calculate the "grid position" by flooring the coordinates to the nearest cell
        double snappedX = Math.Floor(x / cellWidth) * cellWidth;
        double snappedY = Math.Floor(y / cellHeight) * cellHeight;

        // Check if this snapped position is already occupied
        foreach (var item in Items)
        {
            // Get the item's position (assumes the item has a way to track position)
            var itemPosition = new Point((float)item.X, (float)item.Y);

            // If the item occupies this spot, return false (it's taken)
            if (itemPosition.X == snappedX && itemPosition.Y == snappedY)
            {
                return false;  // Spot is occupied
            }
        }

        // If no items are in the spot, return true (it's free)
        return true;
    }
    public Point? FindFreeSpotCoordinates(double x, double y)
    {
        const int cellWidth = 82;
        const int cellHeight = 102;

        // Calculate the "grid position" by flooring the coordinates to the nearest cell
        double snappedX = Math.Floor(x / cellWidth) * cellWidth;
        double snappedY = Math.Floor(y / cellHeight) * cellHeight;

        // Check if this snapped position is already occupied
        foreach (var item in Items)
        {
            // Get the item's position (assumes the item has a way to track position)
            var itemPosition = new Point((float)item.X, (float)item.Y);

            // If the item occupies this spot, return null (spot is occupied)
            if (itemPosition.X == snappedX && itemPosition.Y == snappedY)
            {
                return null;  // Spot is occupied
            }
        }

        // If no items are in the spot, return the snapped coordinates (it's free)
        return new Point(snappedX, snappedY);
    }
    public Point FindSpotCoordinates(double x, double y)
    {
        const int cellWidth = 82;
        const int cellHeight = 102;

        // Calculate the "grid position" by flooring the coordinates to the nearest cell
        double snappedX = Math.Floor(x / cellWidth) * cellWidth;
        double snappedY = Math.Floor(y / cellHeight) * cellHeight;

        // If no items are in the spot, return the snapped coordinates (it's free)
        return new Point(snappedX, snappedY);
    }

    public async Task CopyStorageFolderAsync(StorageFolder sourceFolder, StorageFolder destinationFolder)
    {
        // Create a new folder in the destination folder
        var copiedFolder = await destinationFolder.CreateFolderAsync(sourceFolder.Name, CreationCollisionOption.ReplaceExisting);

        // Copy all files from the source to the copied folder
        var files = await sourceFolder.GetFilesAsync();
        foreach (var file in files)
        {
            await file.CopyAsync(copiedFolder, file.Name, NameCollisionOption.ReplaceExisting);
        }

        // Copy all subfolders from the source to the copied folder
        var subfolders = await sourceFolder.GetFoldersAsync();
        foreach (var subfolder in subfolders)
        {
            await CopyStorageFolderAsync(subfolder, copiedFolder); // Recursively copy subfolders
        }
    }

    private ContentControl GetContentControlFromDesktopFile(DesktopItem desktopFile)
    {
        // Iterate through all children of the canvas (fff)
        foreach (UIElement child in fff.Children)
        {
            // Check if the child is a ContentControl
            if (child is ContentControl contentControl)
            {
                // Compare the Content of the ContentControl with the DesktopFile
                if (contentControl.Content is DesktopItem item && item.FilePath.Equals(desktopFile.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    // Return the matching ContentControl
                    return contentControl;
                }
            }
        }

        // Return null if no matching ContentControl is found
        return null;
    }

    private async void Grid_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        var storageItems = new Collection<IStorageItem>();

        foreach (var item in Items)
        {
            if (item.IsSelected == true)
            {
                try
                {
                    // Check if the item is a folder
                    var attributes = System.IO.File.GetAttributes(item.FilePath);
                    if (attributes.HasFlag(System.IO.FileAttributes.Directory))
                    {
                        var storageFolder = await StorageFolder.GetFolderFromPathAsync(item.FilePath);
                        storageItems.Add(storageFolder);
                    }
                    else
                    {
                        var storageFile = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        storageItems.Add(storageFile);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing {item.FilePath}: {ex.Message}");
                }
            }
        }

        args.Data.SetStorageItems(storageItems);
    }

    bool _isDragging;

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Get the Grid that was double-tapped
        var grid = sender as Grid;

        // Retrieve the DesktopFile from the DataContext
        var desktopFile = grid?.DataContext as DesktopItem;
        if (desktopFile == null) return;

        oldPoint = e.GetCurrentPoint(fff).Position;

        if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control)
            desktopFile.IsSelected = true;
        else

        {
            if ((bool)!desktopFile.IsSelected)
            {
                foreach (var item in Items)
                {
                    item.IsSelected = false;
                }

                desktopFile.IsSelected = true;
            }
        }
        _isDragging = true;
    }

    private void Grid_PointerReleased_1(object sender, PointerRoutedEventArgs e)
    {
        // Get the Grid that was double-tapped
        var grid = sender as Grid;

        // Retrieve the DesktopFile from the DataContext
        var desktopFile = grid?.DataContext as DesktopItem;
        if (desktopFile == null) return;

        _isDragging = false;
        if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control)
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }

            desktopFile.IsSelected = true;
        }
    }

    bool _isInItem = false;

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = true;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = false;
    }
}
