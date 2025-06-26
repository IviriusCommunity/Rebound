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
using Windows.System;

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

    public DesktopPage()
    {
        InitializeComponent();
        this.PointerMoved += DesktopPage_PointerMoved;
        this.PointerReleased += DesktopPage_PointerReleased;
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

    // BEGIN

    bool _isSelectingWithSelectionBox;
    bool _isRightClickHeld;

    private void DesktopPage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isRightClickHeld)
        {
            _isRightClickHeld = false;
            Window.CreateContextMenuAtPosition(e.GetCurrentPoint(null).Position);
            return;
        }

        _isSelectingWithSelectionBox = false;
        SelectionBorder.Margin = new(0);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
    }

    private void DesktopPage_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isSelectingWithSelectionBox)
        {
            InvokeDragBorder(e);
        }
    }

    // Declare this at class level:
    private Dictionary<string, bool> pointerOverStates = new();

    private void InvokeDragBorder(PointerRoutedEventArgs e)
    {
        var currentPos = e.GetCurrentPoint(CanvasControl).Position;

        try
        {
            const double itemWidth = 80;
            const double itemHeight = 100;

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

            foreach (var item in Items)
            {
                var itemRect = new Rect(item.X, item.Y, itemWidth, itemHeight);
                var intersects = selectionRect.IntersectsWith(itemRect);

                if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control)
                {
                    // Get previous pointer-over state, default to false
                    pointerOverStates.TryGetValue(item.FilePath, out bool wasOver);

                    if (intersects && !wasOver)
                    {
                        // Pointer just entered the item's rect → toggle selection
                        item.IsSelected = !item.IsSelected;
                        pointerOverStates[item.FilePath] = true;
                    }
                    else if (!intersects && wasOver)
                    {
                        // Pointer left the item's rect → update state
                        pointerOverStates[item.FilePath] = false;
                    }
                    // else: no change
                }
                else
                {
                    // Default behavior: intersecting = selected
                    item.IsSelected = intersects;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("PointerMoved error: " + ex.Message);
        }
    }

    private void BackgroundGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(null);
        if (point.Properties.IsLeftButtonPressed)
        {
            if (CheckIfSpotIsFree(point.Position.X, point.Position.Y))
            {
                _isSelectingWithSelectionBox = true;

                // Get pointer position relative to the canvas once to avoid repeated calls
                var position = e.GetCurrentPoint(CanvasControl).Position;

                // If Ctrl is not held and the pointer is not over an item, deselect all
                if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed && !e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
                {
                    if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control)
                    {
                        foreach (var item in Items)
                        {
                            item.IsSelected = false;
                        }
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
            else
            {
                _isSelectingWithSelectionBox = false;
            }
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            _isRightClickHeld = true;
        }
    }

    private async void Item_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        dragOffsets.Clear();
        dragStartPointerPos = args.GetPosition(null); // or use e.GetPosition(CanvasControl) depending on where drag was initiated

        foreach (var item in Items.Where(i => (bool)i.IsSelected))
        {
            var offset = new Point(item.X - dragStartPointerPos.X, item.Y - dragStartPointerPos.Y);
            dragOffsets[item.FilePath] = offset;
        }

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

    private void CanvasControl_DragOver(object sender, DragEventArgs e)
    {
        //PlacementBorder.Visibility = Visibility.Visible;
        e.AcceptedOperation = DataPackageOperation.Move;
        e.DragUIOverride.Caption = "Move on Desktop";
    }

    private Dictionary<string, Point> dragOffsets = new(); // FilePath → offset from pointer
    private Point dragStartPointerPos;
    private async void CanvasControl_Drop(object sender, DragEventArgs e)
    {
        _isDragging = false;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                var dropPoint = e.GetPosition(CanvasControl);

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

                // Move existing files (group move)
                if (existingFiles.Count > 0)
                {
                    // Calculate the bounding rectangle of the group
                    var minX = existingFiles.Min(i => i.X);
                    var minY = existingFiles.Min(i => i.Y);

                    // Snap the drop point to grid
                    var (dropCol, dropRow, snappedDropX, snappedDropY) = SnapToGrid(dropPoint.X, dropPoint.Y);

                    // Calculate the offset between the pointer and the top-left of the group
                    var pointerToGroupOffsetX = dropPoint.X - minX;
                    var pointerToGroupOffsetY = dropPoint.Y - minY;

                    // For each item, calculate its relative offset in the group, then snap to grid
                    foreach (var item in existingFiles)
                    {
                        var relX = item.X - minX;
                        var relY = item.Y - minY;

                        var targetX = snappedDropX + relX;
                        var targetY = snappedDropY + relY;

                        // Snap each item's target position to grid
                        var snapped = FindSpotCoordinates(targetX, targetY);

                        // If spot is occupied, find next free spot
                        if (!CheckIfSpotIsFree(snapped.X, snapped.Y))
                        {
                            snapped = FindNextFreeSpot(snapped.X, snapped.Y);
                        }

                        item.X = snapped.X;
                        item.Y = snapped.Y;
                    }
                }

                // Stack new files in next available spots
                var freeSpot = FindFreeSpotCoordinates(dropPoint.X, dropPoint.Y) ?? FindFreeSpot();
                foreach (var file in newFiles)
                {
                    freeSpot = FindNextFreeSpot(freeSpot.X, freeSpot.Y);
                    file.X = freeSpot.X;
                    file.Y = freeSpot.Y;
                    Items.Add(file);
                }
            }
        }
    }

    private (DesktopItem, DateTime) lastClicked;

    private async void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var control = (FrameworkElement)sender;
        var item2 = control.DataContext as DesktopItem;

        // If Ctrl is not held and the pointer is not over an item, deselect all
        if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed && !e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
        {
            if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control)
            {
                foreach (var item in Items)
                {
                    item.IsSelected = false;
                }
                item2.IsSelected = true;
            }
            else
            {
                item2.IsSelected = !item2.IsSelected;
            }
        }

        if (lastClicked.Item1 == item2 && (DateTime.Now - lastClicked.Item2).TotalMilliseconds < 800)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = item2.FilePath,
                    UseShellExecute = true
                });
            }
            catch
            {

            }
        }

        lastClicked = new(item2, DateTime.Now);
    }

    // END

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
        //PlacementBorder.Visibility = Visibility.Collapsed;
        //PlacementBorder.Opacity = 1;
        toggledPaths.Clear();

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
        if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed && !e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
        {
            if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control)
            {
                foreach (var item in Items)
                {
                    item.IsSelected = false;
                }
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

    List<string> toggledPaths = new();

    private void CanvasControl_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        InvokeDragBorder(e);
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

                if (!Items.Any(item => item.X == x && item.Y == y && !item.IsDragging))
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
        return !Items.Any(item => item.X == snappedX && item.Y == snappedY && !item.IsDragging);
    }

    public Point? FindFreeSpotCoordinates(double x, double y)
    {
        var (_, _, snappedX, snappedY) = SnapToGrid(x, y);
        return Items.Any(item => item.X == snappedX && item.Y == snappedY && !item.IsDragging)
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
        //PlacementBorder.Visibility = Visibility.Visible;

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

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {

    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Window = (DesktopWindow?)e?.Parameter;
    }

    private async void CanvasControl_PointerMoved_1(object sender, PointerRoutedEventArgs e)
    {
        //PlacementBorder.Visibility = Visibility.Visible;

        if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed && SelectionBorder.Width == 0 && SelectionBorder.Height == 0 && SelectionBorder.Margin == new Thickness(0))
        {
            // If the left button is pressed and the selection border is visible, set the drag state
            _isDragging = true;

            _ = await (sender as FrameworkElement).StartDragAsync(e.GetCurrentPoint(null));
        }
    }

    FrameworkElement? _triggeredItem;

    Point previousPoint;

    bool _isSelecting;

    private void PlacementBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isSelecting)
        {
            statusBlock.Text = _isInItem.ToString();
            if (!_isInItem)
            {
                InvokeDragBorder(e);
            }
            else
            {
                var point = e.GetCurrentPoint(null);

                if (point.Properties.IsLeftButtonPressed)
                {
                    // If the left button is pressed and the selection border is visible, set the drag state
                    _isDragging = true;

                    foreach (var item in Items)
                    {
                        if (item.IsSelected == true)
                        {
                            item.IsDragging = true;
                            item.Opacity = 0.25;
                            item.X += e.GetCurrentPoint(null).Position.X - previousPoint.X;
                            item.Y += e.GetCurrentPoint(null).Position.Y - previousPoint.Y;
                        }
                    }
                }
            }
            previousPoint = e.GetCurrentPoint(null).Position;
        }
    }

    private void PlacementBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isSelecting = true;
    }

    bool rightClicked;

    private void PlacementBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isSelecting = false;

        if (!_isInItem && rightClicked)
        {
            Window?.CreateContextMenuAtPosition(e.GetCurrentPoint(null).Position);
        }

        if (_isDragging)
        {
            foreach (var item in Items)
            {
                if (item.IsSelected == true)
                {
                    item.Opacity = 0.25;
                    var pos = FindFreeSpotCoordinates(item.X, item.Y);
                    item.X = pos.Value.X;
                    item.Y = pos.Value.Y;
                    item.IsDragging = false;
                }
            }
        }
        //PlacementBorder.Visibility = Visibility.Collapsed;
        SelectionBorder.Margin = new(0);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;

        foreach (var item in Items)
        {
            item.Opacity = 1;
        }
    }

    private void CanvasControl_PointerPressed_1(object sender, PointerRoutedEventArgs e)
    {
        _isSelecting = true;
    }

    private void PointerMoved1(object sender, PointerRoutedEventArgs e)
    {
        if (_isSelecting)
        {
            _isInItem = false;
            _triggeredItem = null;
            var point = e.GetCurrentPoint(null);
            initialMarginX = point.Position.X - 4;
            initialMarginY = point.Position.Y - 4;
            //PlacementBorder.Visibility = Visibility.Visible;
            rightClicked = e.GetCurrentPoint(null).Properties.IsRightButtonPressed;
        }
    }

    bool _isInItem;

    private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = false;
    }

    private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isInItem = true;
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isInItem && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            previousPoint = e.GetCurrentPoint(null).Position;
            //PlacementBorder.Visibility = Visibility.Visible;
        }
    }
}