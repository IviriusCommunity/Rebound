// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Storage;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Rebound.Core.Helpers;
using Rebound.Shell.ExperienceHost;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Diagnostics;
using Windows.UI;
using Windows.Win32;
using Windows.UI.Xaml;
using Colors = Windows.UI.Colors;
using Thickness = Windows.UI.Xaml.Thickness;

namespace Rebound.Shell.Desktop;

/*public partial class WallpaperGlassBackdrop : CompositionBrushBackdrop
{
    protected override Windows.UI.Composition.CompositionBrush CreateBrush(Windows.UI.Composition.Compositor compositor)
    {
        var LuminosityOpacity = SettingsHelper.GetValue("UseMicaMenus", "rshell.desktop", false) ? 1F : 0.7F; // Opacity for luminosity overlay
        var TintOpacity = SettingsHelper.GetValue("UseMicaMenus", "rshell.desktop", false) ? 0.8F : 0F; // Opacity for luminosity overlay
        var TintColor = (App.BackgroundWindow.Content as FrameworkElement).ActualTheme == ElementTheme.Light ? Color.FromArgb(255, 223, 223, 223) : Color.FromArgb(255, 32, 32, 32);

        var baseBrush = SettingsHelper.GetValue("UseMicaMenus", "rshell.desktop", false) ? compositor.TryCreateBlurredWallpaperBackdropBrush() : compositor.CreateHostBackdropBrush();

        // --------- Luminosity Overlay Effect ---------
        var luminosityEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Color,
            Background = new Windows.UI.Composition.CompositionEffectSourceParameter("Wallpaper"),
            Foreground = new Windows.UI.Composition.CompositionEffectSourceParameter("LuminosityOverlay")
        };

        var luminosityEffectComposite = new ArithmeticCompositeEffect
        {
            Source1 = new Windows.UI.Composition.CompositionEffectSourceParameter("Wallpaper"),
            Source2 = luminosityEffect,
            MultiplyAmount = 0,
            Source1Amount = 1 - LuminosityOpacity,
            Source2Amount = LuminosityOpacity,
            Offset = 0
        };

        var luminosityEffectFactory = compositor.CreateEffectFactory(luminosityEffectComposite);
        var luminosityEffectBrush = luminosityEffectFactory.CreateBrush();

        var luminosityTint = compositor.CreateColorBrush(TintColor);
        luminosityEffectBrush.SetSourceParameter("Wallpaper", baseBrush);
        luminosityEffectBrush.SetSourceParameter("LuminosityOverlay", luminosityTint);

        // --------- Color Overlay Effect ---------
        var colorEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Luminosity,
            Background = new Windows.UI.Composition.CompositionEffectSourceParameter("LuminosityEffectOutput"), // Use output of luminosityEffect
            Foreground = new Windows.UI.Composition.CompositionEffectSourceParameter("ColorOverlay")
        };

        var colorEffectComposite = new ArithmeticCompositeEffect
        {
            Source1 = new Windows.UI.Composition.CompositionEffectSourceParameter("LuminosityEffectOutput"), // Use output of luminosityEffect
            Source2 = colorEffect,
            MultiplyAmount = 0,
            Source1Amount = 1 - TintOpacity,
            Source2Amount = TintOpacity,
            Offset = 0
        };

        var colorEffectFactory = compositor.CreateEffectFactory(colorEffectComposite);
        var colorEffectBrush = colorEffectFactory.CreateBrush();

        var colorTint = compositor.CreateColorBrush(TintColor);
        colorEffectBrush.SetSourceParameter("LuminosityEffectOutput", luminosityEffectBrush); // Set luminosityEffectBrush as input
        colorEffectBrush.SetSourceParameter("ColorOverlay", colorTint);

        // Return the final brush with both effects applied
        return colorEffectBrush;
    }
}*/

public sealed partial class DesktopPage : Page
{
    public ObservableCollection<DesktopItem> Items { get; set; } = [];

    //private DesktopWindow? Window;

    private const int CELL_WIDTH = 82;
    private const int CELL_HEIGHT = 102;

    private double initialMarginX;
    private double initialMarginY;

    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _timer2;

    public DesktopViewModel ViewModel { get; } = new();

    public DesktopPage()
    {
        InitializeComponent();

        /*unsafe
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaper();

            PWSTR rawPath;
            desktopWallpaper.GetWallpaper(null, &rawPath);

            var path = new string(rawPath.Value);
            if (!string.IsNullOrWhiteSpace(path))
            {
                Wallpaper.Source = new BitmapImage(new Uri(path));
            }
            DESKTOP_WALLPAPER_POSITION pos;
            desktopWallpaper.GetPosition(&pos);
            Wallpaper.Stretch = pos switch
            {
                DESKTOP_WALLPAPER_POSITION.DWPOS_FILL => Stretch.Fill,
                DESKTOP_WALLPAPER_POSITION.DWPOS_FIT => Stretch.Uniform,
                DESKTOP_WALLPAPER_POSITION.DWPOS_STRETCH => Stretch.UniformToFill,
                DESKTOP_WALLPAPER_POSITION.DWPOS_TILE => Stretch.None,
                DESKTOP_WALLPAPER_POSITION.DWPOS_CENTER => Stretch.None,
                _ => Stretch.Uniform
            };

            //Marshal.ReleaseComObject(desktopWallpaper);
        }*/
        this.PointerMoved += DesktopPage_PointerMoved;
        this.PointerReleased += DesktopPage_PointerReleased;
        Refresh();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        _timer2 = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _timer2.Tick += _timer2_Tick;
        _timer2.Start();

        UpdateClock(); // initial immediate update

        QueryLiveWallpaper();
    }

    private async void _timer2_Tick(object? sender, object e)
    {
        var usage = await GetSystemUsageAsync();
        ViewModel.CPUUsage = $"{usage.cpuPercent:0}%";
        ViewModel.RAMUsage = $"{usage.memoryPercent:0}%";
    }

    public async Task<(double cpuPercent, double memoryPercent)> GetSystemUsageAsync()
    {
        PerformanceCounter cpu = new("Processor", "% Processor Time", "_Total");
        var usageReport = SystemDiagnosticInfo.GetForCurrentSystem().CpuUsage;
        var memoryReport = SystemDiagnosticInfo.GetForCurrentSystem().MemoryUsage;

        cpu.NextValue();
        await Task.Delay(500);
        var cpuUsed = cpu.NextValue();

        var totalMem = memoryReport.GetReport().TotalPhysicalSizeInBytes;
        var usedMem = totalMem - memoryReport.GetReport().AvailableSizeInBytes;
        var memPercent = (usedMem / (double)totalMem) * 100;

        return (cpuUsed, memPercent);
    }

    private static readonly Windows.Win32.Foundation.HWND HWND_BOTTOM = new(1);

    // Constants
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;

    public async void QueryLiveWallpaper()
    {
        /*while (true)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                Wallpaper.Source = null;

                Windows.Win32.PInvoke.GetWindowRect(new(Window.GetWindowHandle()), out var rect);

                var screenArea = new System.Drawing.Rectangle(
                    rect.left,
                    rect.top,
                    rect.right - rect.left,
                    rect.bottom - rect.top
                );

                var bmp = ProgManHook.CaptureBehindWindow(new(Window.GetWindowHandle()));

                using var stream = new MemoryStream();
                bmp.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                var image = new BitmapImage();
                await image.SetSourceAsync(stream.AsRandomAccessStream());

                Wallpaper.Source = image;
            });
            await Task.Delay(1000);
        }*/
    }

    [RelayCommand]
    private void ExitDesktop()
    {
        /*((DesktopWindow)App.DesktopWindow).canClose = true;
        ((DesktopWindow)App.DesktopWindow).Close();
        ((DesktopWindow)App.DesktopWindow).RestoreExplorerDesktop();*/
    }

    [RelayCommand]
    private async Task ExitShellAsync()
    {
        /*((DesktopWindow)App.DesktopWindow).canClose = true;
        ((DesktopWindow)App.DesktopWindow).Close();
        ((DesktopWindow)App.DesktopWindow).RestoreExplorerDesktop();*/
        await Task.Delay(250).ConfigureAwait(true);
        Process.GetCurrentProcess().Kill();
    }

    [RelayCommand]
    public void ShowOptions()
    {
        OptionsGrid.Visibility = Visibility.Visible;
    }

    [RelayCommand]
    public void HideOptions()
    {
        OptionsGrid.Visibility = Visibility.Collapsed;
    }

    private void Timer_Tick(object sender, object e)
    {
        UpdateClock();
    }

    private DateTime _lastDate = DateTime.MinValue;

    private void UpdateClock()
    {
        var now = DateTime.Now;

        // Example: 12:03
        ViewModel.CurrentTime = now.ToString("hh:mm:ss tt");

        // Example: Friday, 02.12.2025
        ViewModel.CurrentDate = now.ToString("dddd, dd.MM.yyyy");

        ViewModel.CurrentDay = now.ToString("dddd");
        ViewModel.CurrentDayOfMonth = now.ToString("dd");
        ViewModel.CurrentMonthAndYear = now.ToString("MMMM yyyy");

        if (_lastDate.Date != now.Date)
        {
            CalendarControl.SetDisplayDate(now);
            _lastDate = now.Date;
        }
    }

    [RelayCommand]
    public void Refresh()
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
        {
            // Clear items before loading to avoid adding duplicates
            Items.Clear();

            // Fetch files first, then update UI safely
            var newItems = await GetDesktopFilesAsync().ConfigureAwait(true);

            // Ensure UI updates happen on UI thread
            DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
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
                    item.PropertyChanged += Item_PropertyChanged;
                }
            });

            LoadingGrid.Visibility = Visibility.Collapsed;
        });
    }

    private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not DesktopItem desktopItem)
            return;

        // Get the index of the changed item
        int index = Items.IndexOf(desktopItem);
        if (index < 0) return;

        // Get the container (UIElement) for that item
        var container = CanvasControl.GetOrCreateElement(index) as ListViewItem;
        if (container != null)
        {
            container.Background = new SolidColorBrush(Colors.Transparent);
        }
    }

    // BEGIN

    bool _isSelectingWithSelectionBox;
    bool _isRightClickHeld;

    private async void DesktopPage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isRightClickHeld)
        {
            _isRightClickHeld = false;
            ContextMenu.ShowAt(this, new FlyoutShowOptions()
            {
                Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
                ShowMode = FlyoutShowMode.Transient,
                Position = e.GetCurrentPoint(null).Position
            });
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
        var currentPos = e.GetCurrentPoint(null).Position;

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
            SelectionBorder.Margin = new Thickness(left, top, 0, 0);
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
                var position = e.GetCurrentPoint(null).Position;

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

                // Store initial position for later drag tracking
                initialMarginX = position.X;
                initialMarginY = position.Y;

                // Set selection box's initial position and clear its size
                SelectionBorder.Margin = new(initialMarginX, initialMarginY, 0, 0);
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

    private async Task<Windows.Win32.UI.Shell.HDROP?> TryGetHDROPAsync(DragEventArgs e)
    {
        if (!e.DataView.Contains("WindowsShell.HDROP"))
            return null;

        try
        {
            var data = await e.DataView.GetDataAsync("WindowsShell.HDROP");
            if (data is Windows.Win32.System.Com.IDataObject dataObject)
            {
                var format = new Windows.Win32.System.Com.FORMATETC
                {
                    cfFormat = 15, // CF_HDROP
                    dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    tymed = (uint)TYMED.TYMED_HGLOBAL
                };

                dataObject.GetData(ref format, out var medium);
                return new Windows.Win32.UI.Shell.HDROP((nint)medium.u.hGlobal);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HDROP failed: {ex.Message}");
        }

        return null;
    }

    private Dictionary<string, Point> dragOffsets = new(); // FilePath → offset from pointer
    private Point dragStartPointerPos;
    private async void CanvasControl_Drop(object sender, DragEventArgs e)
    {
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
                            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            FileStream stream;

                            if (!File.Exists(Path.Combine(desktopPath, file.Name)))
                            {
                                stream = File.Create(Path.Combine(desktopPath, file.Name));
                            }
                            else
                            {
                                stream = File.OpenWrite(Path.Combine(desktopPath, file.Name));
                            }
                            var str = await file.OpenReadAsync();
                            using (var input = str.AsStreamForRead()) // Convert to .NET Stream
                            {
                                await input.CopyToAsync(stream);
                                await stream.FlushAsync(); // Ensure everything is written
                            }

                            desktopFile = new DesktopItem(Path.Combine(desktopPath, file.Name));
                            await desktopFile.LoadThumbnailAsync().ConfigureAwait(false);
                            newFiles.Add(desktopFile);
                        }
                        else if (storageFile is StorageFolder folder)
                        {
                            await CopyFolderToDesktopAsync(folder).ConfigureAwait(false);

                            async Task CopyFolderToDesktopAsync(StorageFolder sourceFolder)
                            {
                                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                var targetRoot = Path.Combine(desktopPath, sourceFolder.Name);

                                CopyDirectoryRecursive(sourceFolder.Path, targetRoot);

                                // Create a DesktopItem for the copied folder
                                var desktopFolder = new DesktopItem(targetRoot);
                                await desktopFolder.LoadThumbnailAsync().ConfigureAwait(false);
                                newFiles.Add(desktopFolder);
                            }

                            void CopyDirectoryRecursive(string sourceDir, string destDir)
                            {
                                // Create destination directory if it doesn't exist
                                Directory.CreateDirectory(destDir);

                                // Copy all files
                                foreach (var file in Directory.GetFiles(sourceDir))
                                {
                                    var destFile = Path.Combine(destDir, Path.GetFileName(file));
                                    File.Copy(file, destFile, overwrite: true);
                                }

                                // Recursively copy subdirectories
                                foreach (var dir in Directory.GetDirectories(sourceDir))
                                {
                                    var dirName = Path.GetFileName(dir);
                                    var destSubDir = Path.Combine(destDir, dirName);
                                    CopyDirectoryRecursive(dir, destSubDir);
                                }
                            }
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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //Window = (DesktopWindow?)e?.Parameter;
    }
}