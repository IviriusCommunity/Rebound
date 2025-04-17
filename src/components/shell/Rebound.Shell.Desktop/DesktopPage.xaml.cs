using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;

namespace Rebound.Shell.Desktop;

public sealed partial class DesktopPage : Page
{
    public ObservableCollection<DesktopItem> Items { get; set; } = new();

    public DesktopPage()
    {
        InitializeComponent();

        DispatcherQueue.TryEnqueue(async () =>
        {
            // Clear items before loading to avoid adding duplicates
            Items.Clear();

            // Fetch files first, then update UI safely
            var newItems = await GetDesktopFilesAsync().ConfigureAwait(true);

            // Ensure UI updates happen on UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in newItems)
                {
                    Items.Add(item);
                }

                // Now safely process items
                foreach (var item in Items)
                {
                    /*var contentControl = new ContentControl()
                    {
                        ContentTemplate = (DataTemplate)Resources["DesktopItemTemplate"],
                        Content = item
                    };

                    contentControl.PointerPressed += ContentControl_PointerPressed;
                    contentControl.PointerMoved += ContentControl_PointerMoved;
                    contentControl.PointerReleased += ContentControl_PointerReleased;*/

                    if (item.X is -1 || item.Y is -1)
                    {
                        var freeSpot = FindFreeSpot();
                        if (freeSpot.X != -1 && freeSpot.Y != -1)
                        {
                            /*Canvas.SetLeft(contentControl, freeSpot.X);
                            Canvas.SetTop(contentControl, freeSpot.Y);*/
                            item.X = freeSpot.X;
                            item.Y = freeSpot.Y;
                        }
                    }
                    else
                    {
                        /*Canvas.SetLeft(contentControl, item.X);
                        Canvas.SetTop(contentControl, item.Y);*/
                    }

                    /*CanvasControl.Children.Add(contentControl);*/
                }
            });
        });
    }

    public async Task<List<DesktopItem>> GetDesktopFilesAsync()
    {
        List<DesktopItem> items = [];
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        if (Directory.Exists(desktopPath))
        {
            // Get both files and folders
            var files = Directory.GetFiles(desktopPath);
            var folders = Directory.GetDirectories(desktopPath);

            foreach ( var file in files)
            {
                await LoadFileAsync(items, file).ConfigureAwait(true);
            }
            foreach ( var folder in folders)
            {
                await LoadFileAsync(items, folder).ConfigureAwait(true);
            }
        }

        return items;
    }

    private async Task LoadFileAsync(List<DesktopItem> items, string filePath)
    {
        var desktopFile = new DesktopItem(filePath);
        await desktopFile.LoadThumbnailAsync().ConfigureAwait(true);

        // Add directly to ObservableCollection for real-time UI update
        items.Add(desktopFile);
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
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

    // P/Invoke to set the parent of the window
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    public static string GetWallpaperPath()
    {
        var wallpaperPath = new StringBuilder(MAX_PATH);
        var result = SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaperPath, 0);

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

    private async void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
    {
        // Clear items before loading to avoid adding duplicates
        Items.Clear();

        // Run GetDesktopFilesAsync to add items one-by-one
        await GetDesktopFilesAsync();
    }

    private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // Get the Grid that was double-tapped
        var grid = sender as Grid;

        // Retrieve the DesktopFile from the DataContext
        var desktopFile = (DesktopItem?)grid?.DataContext;
        if (desktopFile == null)
        {
            return;
        }

        // Open the file
        Process.Start(new ProcessStartInfo
        {
            FileName = desktopFile.FilePath,
            UseShellExecute = true // Ensure the shell is used to launch the file
        });
    }

    private void CanvasControl_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        SelectionBorder.Margin = new(0);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
        _selectionOn = false;
    }

    private bool _selectionOn = false;
    private double initialMarginX;
    private double initialMarginY;

    private void CanvasControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control && !_isInItem)
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
        }
        _selectionOn = true;
        initialMarginX = e.GetCurrentPoint(CanvasControl).Position.X;
        initialMarginY = e.GetCurrentPoint(CanvasControl).Position.Y;
        SelectionBorder.Margin = new(e.GetCurrentPoint(CanvasControl).Position.X, e.GetCurrentPoint(CanvasControl).Position.Y, 0, 0);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
    }

    private Point lastPos;

    private double left;
    private double top;

    private void CanvasControl_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var Pos = e.GetCurrentPoint(CanvasControl).Position;
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

    private void CanvasControl_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
        e.DragUIOverride.Caption = "Move on Desktop";
    }

    private Point oldPoint;

    private async void CanvasControl_Drop(object sender, DragEventArgs e)
    {
        PlacementBorder.Visibility = Visibility.Collapsed;
        _isDragging = false;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                var point = e.GetPosition(CanvasControl); // Get drop position
                var freeSpot = FindFreeSpotCoordinates(point.X, point.Y) ?? FindFreeSpot(); // Find the first available spot

                List<DesktopItem> existingFiles = [];
                List<DesktopItem> newFiles = [];

                foreach (var storageFile in items)
                {
                    var desktopFile = Items.FirstOrDefault(item => (item.FilePath ?? "").Equals(storageFile.Path, StringComparison.OrdinalIgnoreCase));

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

                        Items.Add(desktopFile ?? new DesktopItem(""));
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
                        //var contentControl = GetContentControlFromDesktopFile(file);

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
                            /*Canvas.SetLeft(contentControl, newItemPos.X);
                            Canvas.SetTop(contentControl, newItemPos.Y);*/

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
                            /*Canvas.SetLeft(contentControl, definitivePos.X);
                            Canvas.SetTop(contentControl, definitivePos.Y);*/

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
                    //CanvasControl.Children.Add(contentControl);
                }
            }
        }
    }

    public Point FindNextFreeSpot(double startX, double startY)
    {
        if (CanvasControl.ActualWidth < CELL_WIDTH || CanvasControl.ActualHeight < CELL_HEIGHT)
            return new Point(-1, -1); // No room for even one item

        int col = (int)(Math.Max(0, startX) / CELL_WIDTH);
        int row = (int)(Math.Max(0, startY) / CELL_HEIGHT);

        while (true)
        {
            double x = col * CELL_WIDTH;
            double y = row * CELL_HEIGHT;

            bool spotTaken = Items.Any(item => item.X == x && item.Y == y);
            if (!spotTaken)
                return new Point(x, y);

            row++;
            if ((row + 1) * CELL_HEIGHT > CanvasControl.ActualHeight)
            {
                row = 0;
                col++;
            }

            if ((col + 1) * CELL_WIDTH > CanvasControl.ActualWidth)
            {
                Debug.WriteLine("No space available!");
                return FindFreeSpot();
            }
        }
    }

    public Point FindFreeSpot()
    {
        int maxCols = (int)(CanvasControl.ActualWidth / CELL_WIDTH);
        int maxRows = (int)(CanvasControl.ActualHeight / CELL_HEIGHT);

        for (int col = 0; col < maxCols; col++)
        {
            for (int row = 0; row < maxRows; row++)
            {
                double x = col * CELL_WIDTH;
                double y = row * CELL_HEIGHT;

                bool spotTaken = Items.Any(item => item.X == x && item.Y == y);
                if (!spotTaken)
                    return new Point(x, y);
            }
        }

        return new Point(-1, -1);
    }

    private const int CELL_WIDTH = 82;
    private const int CELL_HEIGHT = 102;

    public bool CheckIfSpotIsFree(double x, double y)
    {
        int col = (int)(Math.Max(0, x) / CELL_WIDTH);
        int row = (int)(Math.Max(0, y) / CELL_HEIGHT);

        double snappedX = col * CELL_WIDTH;
        double snappedY = row * CELL_HEIGHT;

        return !Items.Any(item => item.X == snappedX && item.Y == snappedY);
    }

    public Point? FindFreeSpotCoordinates(double x, double y)
    {
        int col = (int)(Math.Max(0, x) / CELL_WIDTH);
        int row = (int)(Math.Max(0, y) / CELL_HEIGHT);

        double snappedX = col * CELL_WIDTH;
        double snappedY = row * CELL_HEIGHT;

        if (Items.Any(item => item.X == snappedX && item.Y == snappedY))
            return null;

        return new Point(snappedX, snappedY);
    }

    public static Point FindSpotCoordinates(double x, double y)
    {
        int col = (int)(Math.Max(0, x) / CELL_WIDTH);
        int row = (int)(Math.Max(0, y) / CELL_HEIGHT);

        return new Point(col * CELL_WIDTH, row * CELL_HEIGHT);
    }

    public static async Task CopyStorageFolderAsync(StorageFolder sourceFolder, StorageFolder destinationFolder)
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

    /*private ContentControl? GetContentControlFromDesktopFile(DesktopItem desktopFile)
    {
        // Iterate through all children of the canvas (fff)
        foreach (var child in CanvasControl.Children)
        {
            // Check if the child is a ContentControl
            if (child is ContentControl contentControl)
            {
                // Compare the Content of the ContentControl with the DesktopFile
                if (contentControl.Content is DesktopItem item && (item.FilePath ?? "").Equals(desktopFile.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    // Return the matching ContentControl
                    return contentControl;
                }
            }
        }

        // Return null if no matching ContentControl is found
        return null;
    }*/

    private async void Grid_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        PlacementBorder.Visibility = Visibility.Visible;
        var storageItems = new Collection<IStorageItem>();

        foreach (var item in Items)
        {
            if (item.IsSelected == true)
            {
                try
                {
                    // Check if the item is a folder
                    var attributes = File.GetAttributes(item.FilePath ?? "");
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

    private bool _isDragging;

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Get the Grid that was double-tapped
        var grid = sender as Grid;

        // Retrieve the DesktopFile from the DataContext
        var desktopFile = (DesktopItem?)grid?.DataContext;
        if (desktopFile == null)
        {
            return;
        }

        oldPoint = e.GetCurrentPoint(CanvasControl).Position;

        if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control)
        {
            desktopFile.IsSelected = true;
        }
        else

        {
            if (desktopFile.IsSelected != true)
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
        var desktopFile = (DesktopItem?)grid?.DataContext;
        if (desktopFile == null)
        {
            return;
        }

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

    private bool _isInItem = false;

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = true;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = false;
    }

    DesktopWindow win;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        win = (DesktopWindow)e.Parameter;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        win.RestoreExplorerDesktop();
    }
}