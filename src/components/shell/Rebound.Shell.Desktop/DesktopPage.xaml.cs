// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
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
    public ObservableCollection<DesktopItem> Items { get; set; } = [];

    private DesktopWindow? Window;

    private const int CELL_WIDTH = 82;
    private const int CELL_HEIGHT = 102;

    private bool _selectionOn;
    private double initialMarginX;
    private double initialMarginY;
    private Point oldPoint;
    private bool _isDragging;
    private bool _isInItem;

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
                    if (item.X is -1 || item.Y is -1)
                    {
                        var freeSpot = FindFreeSpot();
                        if (freeSpot.X != -1 && freeSpot.Y != -1)
                        {
                            item.X = freeSpot.X;
                            item.Y = freeSpot.Y;
                        }
                    }
                }
            });

            LoadingGrid.Visibility = Visibility.Collapsed;
        });
    }

    [RequiresUnreferencedCode("Requires unreferenced code to load thumbnails")]
    public static async Task<List<DesktopItem>> GetDesktopFilesAsync()
    {
        var items = new List<DesktopItem>();
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        if (!Directory.Exists(desktopPath))
        {
            return items;
        }

        // Combine files and folders into a single list
        var entries = Directory.GetFiles(desktopPath).Concat(Directory.GetDirectories(desktopPath)).ToList();

        // Load all entries in parallel
        var loadTasks = entries.Select(static async path =>
        {
            var desktopFile = new DesktopItem(path);
            await desktopFile.LoadThumbnailAsync().ConfigureAwait(false);
            return desktopFile;
        });

        // Wait for all to complete
        var loadedItems = await Task.WhenAll(loadTasks).ConfigureAwait(false);

        items.AddRange(loadedItems);
        return items;
    }

    private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not Grid grid || grid.DataContext is not DesktopItem desktopFile)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = desktopFile.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // Optional: log or show an error to the user
            Debug.WriteLine($"Failed to open file: {ex.Message}");
        }
    }

    private void CanvasControl_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Hide the placement preview border (used for item placement feedback)
        PlacementBorder.Visibility = Visibility.Collapsed;
        PlacementBorder.Opacity = 1;

        // Get the pointer position relative to the canvas
        var point = e.GetCurrentPoint(null);

        // If the right mouse button was released, show the context menu at that position
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
        {
            Window?.CreateContextMenuAtPosition(point.Position);
        }

        // Reset selection state and visual selector box
        _selectionOn = false;
        SelectionBorder.Margin = new(0); // Reset margin to top-left
        SelectionBorder.Width = SelectionBorder.Height = 0; // Clear selection box dimensions
    }

    private void CanvasControl_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Get pointer position relative to the canvas once to avoid repeated calls
        var position = e.GetCurrentPoint(CanvasControl).Position;

        // If Ctrl is not held and the pointer is not over an item, deselect all
        if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control && !_isInItem)
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
        }

        // Begin selection drag mode
        _selectionOn = true;

        // Store initial position for later drag tracking
        initialMarginX = position.X;
        initialMarginY = position.Y;

        // Set selection box's initial position and clear its size
        SelectionBorder.Margin = new(position.X, position.Y, 0, 0);
        SelectionBorder.Width = SelectionBorder.Height = 0;
    }

    private void CanvasControl_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var currentPos = e.GetCurrentPoint(CanvasControl).Position;

        try
        {
            // Only proceed if dragging for selection and not moving an item
            if (!e.Pointer.IsInContact || !_selectionOn || _isDragging)
            {
                return;
            }

            const double itemWidth = 80;
            const double itemHeight = 100;

            // Show placement border for feedback
            PlacementBorder.Visibility = Visibility.Visible;

            // Opacity trick to force layout/interaction visuals
            PlacementBorder.Opacity = 0.01;

            // Calculate new selection rectangle dimensions
            var left = Math.Min(currentPos.X, initialMarginX);
            var top = Math.Min(currentPos.Y, initialMarginY);
            var width = Math.Abs(currentPos.X - initialMarginX);
            var height = Math.Abs(currentPos.Y - initialMarginY);

            // Apply margin and size to selection UI
            SelectionBorder.Margin = new Thickness(left, top, -4, -4);
            SelectionBorder.Width = width;
            SelectionBorder.Height = height;

            // Define selection rectangle for hit testing
            var selectionRect = new Rect(left, top, width, height);

            // Update selection state based on intersection
            foreach (var item in Items)
            {
                var itemRect = new Rect(item.X, item.Y, itemWidth, itemHeight);
                var intersects = selectionRect.IntersectsWith(itemRect);
                if (item.IsSelected != intersects)
                {
                    item.IsSelected = intersects;
                }
            }
        }
        catch (Exception ex)
        {
            // Optional: Log or debug exception
            Debug.WriteLine("PointerMoved error: " + ex.Message);
        }
    }

    private void CanvasControl_DragOver(object sender, DragEventArgs e)
    {
        PlacementBorder.Visibility = Visibility.Visible;
        e.AcceptedOperation = DataPackageOperation.Move;
        e.DragUIOverride.Caption = "Move on Desktop";
    }

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
                            await desktopFile.LoadThumbnailAsync().ConfigureAwait(false);
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

                            await CopyStorageFolderAsync(folder, destinationFolder).ConfigureAwait(true);

                            desktopFile = new DesktopItem(destinationFolder.Path);
                            await desktopFile.LoadThumbnailAsync().ConfigureAwait(false);
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
                            // Write new position to items
                            file.X = newItemPos.X;
                            file.Y = newItemPos.Y;
                        }
                        // If it can't, find the immediate next one
                        else
                        {
                            // Get the definitive position
                            var definitivePos = FindNextFreeSpot(point.X, point.Y);

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

                    file.X = freeSpot.X;
                    file.Y = freeSpot.Y;

                    Items.Add(file);
                }
            }
        }
    }

    private static (int col, int row, double snappedX, double snappedY) SnapToGrid(double x, double y)
    {
        var col = (int)(Math.Max(0, x) / CELL_WIDTH);
        var row = (int)(Math.Max(0, y) / CELL_HEIGHT);
        return (col, row, col * CELL_WIDTH, row * CELL_HEIGHT);
    }

    public Point FindNextFreeSpot(double startX, double startY)
    {
        if (CanvasControl.ActualWidth < CELL_WIDTH || CanvasControl.ActualHeight < CELL_HEIGHT)
        {
            return new Point(-1, -1);
        }

        var (col, row, _, _) = SnapToGrid(startX, startY);

        var maxCols = (int)(CanvasControl.ActualWidth / CELL_WIDTH);
        var maxRows = (int)(CanvasControl.ActualHeight / CELL_HEIGHT);

        while (true)
        {
            double x = col * CELL_WIDTH;
            double y = row * CELL_HEIGHT;

            if (!Items.Any(item => item.X == x && item.Y == y))
            {
                return new Point(x, y);
            }

            if (++row >= maxRows)
            {
                row = 0;
                col++;
            }

            if (col >= maxCols)
            {
                Debug.WriteLine("No space available!");
                return FindFreeSpot();
            }
        }
    }

    public Point FindFreeSpot()
    {
        var maxCols = (int)(CanvasControl.ActualWidth / CELL_WIDTH);
        var maxRows = (int)(CanvasControl.ActualHeight / CELL_HEIGHT);

        for (var col = 0; col < maxCols; col++)
        {
            for (var row = 0; row < maxRows; row++)
            {
                double x = col * CELL_WIDTH;
                double y = row * CELL_HEIGHT;

                if (!Items.Any(item => item.X == x && item.Y == y))
                {
                    return new Point(x, y);
                }
            }
        }

        return new Point(-1, -1);
    }

    public bool CheckIfSpotIsFree(double x, double y)
    {
        var (_, _, snappedX, snappedY) = SnapToGrid(x, y);
        return !Items.Any(item => item.X == snappedX && item.Y == snappedY);
    }

    public Point? FindFreeSpotCoordinates(double x, double y)
    {
        var (_, _, snappedX, snappedY) = SnapToGrid(x, y);
        return Items.Any(item => item.X == snappedX && item.Y == snappedY)
            ? null
            : new Point(snappedX, snappedY);
    }

    public static Point FindSpotCoordinates(double x, double y)
    {
        var (_, _, snappedX, snappedY) = SnapToGrid(x, y);
        return new Point(snappedX, snappedY);
    }

    public static async Task CopyStorageFolderAsync(StorageFolder sourceFolder, StorageFolder destinationFolder)
    {
        if (sourceFolder == null || destinationFolder == null)
        {
            return;
        }

        try
        {
            // Create a new folder in the destination folder
            var copiedFolder = await destinationFolder.CreateFolderAsync(sourceFolder.Name, CreationCollisionOption.ReplaceExisting);

            // Copy all files from the source to the copied folder
            var files = await sourceFolder.GetFilesAsync();
            var fileCopyTasks = files.Select(file =>
                file.CopyAsync(copiedFolder, file.Name, NameCollisionOption.ReplaceExisting)
            ).ToList();

            // Await all file copy tasks concurrently
            await Task.WhenAll((IEnumerable<Task>)fileCopyTasks).ConfigureAwait(false);

            // Copy all subfolders from the source to the copied folder
            var subfolders = await sourceFolder.GetFoldersAsync();
            var subfolderCopyTasks = subfolders.Select(subfolder =>
                CopyStorageFolderAsync(subfolder, copiedFolder) // Recursively copy subfolders
            ).ToList();

            // Await all subfolder copy tasks concurrently
            await Task.WhenAll(subfolderCopyTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log or handle the error as necessary
            Debug.WriteLine($"Error copying folder: {ex.Message}");
        }
    }

    private async void Grid_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        PlacementBorder.Visibility = Visibility.Visible;

        var storageItems = new List<IStorageItem>();

        // Iterate through selected items
        foreach (var item in Items.Where(i => i.IsSelected == true))
        {
            try
            {
                IStorageItem? storageItem = null;

                // Check if the item is a folder or file
                var filePath = item.FilePath ?? string.Empty;
                if (File.Exists(filePath))
                {
                    var attributes = File.GetAttributes(filePath);
                    if (attributes.HasFlag(System.IO.FileAttributes.Directory))
                    {
                        storageItem = await StorageFolder.GetFolderFromPathAsync(filePath);
                    }
                    else
                    {
                        storageItem = await StorageFile.GetFileFromPathAsync(filePath);
                    }
                }

                if (storageItem != null)
                {
                    storageItems.Add(storageItem);
                }
            }
            catch (Exception ex)
            {
                // Log the error with more details
                Debug.WriteLine($"Error processing {item.FilePath}: {ex.Message}");
            }
        }

        // Set the storage items for the drag operation
        if (storageItems.Count != 0)
        {
            args.Data.SetStorageItems(storageItems);
        }
        else
        {
            Debug.WriteLine("No valid items selected for drag operation.");
        }
    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = true;

        var grid1 = sender as Grid;
        var desktopFile = (DesktopItem?)grid1?.DataContext;

        if (desktopFile == null)
        {
            return;
        }

        oldPoint = e.GetCurrentPoint(CanvasControl).Position;

        // Handle selection based on modifier keys
        HandleSelection(desktopFile, e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control);

        _isDragging = true;
    }

    private void Grid_PointerReleased_1(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = true;

        var grid1 = sender as Grid;
        var desktopFile = (DesktopItem?)grid1?.DataContext;

        if (desktopFile == null)
        {
            return;
        }

        _isDragging = false;

        // Deselect all items if control is not pressed and select the released item
        HandleSelection(desktopFile, e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control);
    }

    private void HandleSelection(DesktopItem desktopFile, bool isControlPressed)
    {
        // If Control is not pressed, deselect all items
        if (!isControlPressed)
        {
            foreach (var item1 in Items)
            {
                item1.IsSelected = false;
            }
        }

        desktopFile.IsSelected = true;
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e) => _isInItem = true;

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e) => _isInItem = true;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Window = (DesktopWindow?)e?.Parameter;
    }
}