// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.UI.UWP.Storage;

/// <summary>
/// Describes a single file type filter entry shown in the dialog dropdown.
/// </summary>
/// <param name="DisplayName">Human-readable label, e.g. "PNG Images".</param>
/// <param name="Extension">
/// Semicolon-separated pattern(s), e.g. "*.png" or "*.png;*.jpg".
/// Omit leading *. — the wrapper normalises bare extensions automatically.
/// </param>
public readonly record struct FileDialogFilter(string DisplayName, string Extension)
{
    /// <summary>
    /// Returns the extension spec normalised to a Win32 pattern.
    /// </summary>
    internal string NormalizedPattern
    {
        get
        {
            // Accept both "png" and "*.png"; split on commas/spaces too.
            var parts = Extension.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries);
            var normalised = new string[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i].Trim();
                normalised[i] = p == "*" ? "*"
                              : p.StartsWith("*.") ? p
                              : p.StartsWith('*') ? "*." + p[1..]
                              : p.StartsWith('.') ? "*" + p
                                                  : "*." + p;
            }
            return string.Join(';', normalised);
        }
    }
}

/// <summary>
/// The outcome of any file/folder picker call.
/// </summary>
public readonly struct DialogResult
{
    /// <summary>
    /// <see langword="true"/> when the user dismissed the dialog without
    /// confirming a selection.
    /// </summary>
    public bool IsCancelled { get; init; }

    /// <summary>
    /// The single selected path, or <see langword="null"/> when cancelled or
    /// when <see cref="Paths"/> contains multiple items.
    /// For multi-select results use <see cref="Paths"/> instead.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// All selected paths. Always non-null; empty when cancelled.
    /// Single-selection dialogs populate index 0 only.
    /// </summary>
    public string[] Paths { get; init; }

    // ── Convenience factories ─────────────────────────────────────────────────

    internal static DialogResult Cancelled() =>
        new() { IsCancelled = true, Paths = [] };

    internal static DialogResult Single(string path) =>
        new() { IsCancelled = false, Path = path, Paths = [path] };

    internal static DialogResult Multi(string[] paths) =>
        new()
        {
            IsCancelled = paths.Length == 0,
            Path = paths.Length == 1 ? paths[0] : null,
            Paths = paths,
        };
}

internal static partial class Shell
{
    internal unsafe static ComPtr<IFileOpenDialog> CreateOpenDialog()
    {
        using var clsid = new ManagedPtr<Guid>(CLSID.CLSID_FileOpenDialog);
        using var iid = new ManagedPtr<Guid>(IID.IID_IUnknown);
        ComPtr<IFileOpenDialog> obj = default;

        var hr = CoCreateInstance(
            clsid,
            null,
            (uint)CLSCTX.CLSCTX_INPROC_SERVER,
            iid,
            (void**)obj.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);
        return obj;
    }

    internal unsafe static ComPtr<IFileSaveDialog> CreateSaveDialog()
    {
        using var clsid = new ManagedPtr<Guid>(CLSID.CLSID_FileSaveDialog);
        using var iid = new ManagedPtr<Guid>(IID.IID_IUnknown);
        ComPtr<IFileSaveDialog> obj = default;

        var hr = CoCreateInstance(
            clsid,
            null,
            (uint)CLSCTX.CLSCTX_INPROC_SERVER,
            iid,
            (void**)obj.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);
        return obj;
    }
}

internal static class FOS
{
    internal const uint OverwritePrompt = 0x00000002;
    internal const uint StrictFileTypes = 0x00000004;
    internal const uint NoChangeDir = 0x00000008;
    internal const uint PickFolders = 0x00000020;
    internal const uint ForceFilesystem = 0x00000040;
    internal const uint AllNonStorageItems = 0x00000080;
    internal const uint NoValidate = 0x00000100;
    internal const uint AllowMultiSelect = 0x00000200;
    internal const uint PathMustExist = 0x00000800;
    internal const uint FileMustExist = 0x00001000;
    internal const uint CreatePrompt = 0x00002000;
    internal const uint NoReadOnlyReturn = 0x00008000;
    internal const uint HideMRUPlaces = 0x00020000;
    internal const uint HidePinnedPlaces = 0x00040000;
    internal const uint DontAddToRecent = 0x02000000;
    internal const uint ForceShowHidden = 0x10000000;
    internal const uint DefaultNoMiniMode = 0x20000000;
}

file static class DialogHelpers
{
    internal const int HRESULT_Cancelled = unchecked((int)0x800704C7);

    internal static unsafe string? GetPath(IShellItem* item)
    {
        char* path;
        item->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &path);
        //Marshal.ReleaseComObject(item);
        return new string(path);
    }

    internal static unsafe string[] GetPaths(IShellItemArray* array)
    {
        uint count;
        array->GetCount(&count);
        var paths = new string[count];
        for (uint i = 0; i < count; i++)
        {
            ComPtr<IShellItem> psi = default;
            array->GetItemAt(i, psi.GetAddressOf());
            paths[i] = GetPath(psi.Get()) ?? string.Empty;
        }
        //Marshal.ReleaseComObject(array);
        return paths;
    }
}

/// <summary>
/// Win32 file and folder picker wrappers, owner-aware for XAML Islands windows.
/// All methods are synchronous and must be called on the UI thread.
/// </summary>
public static class FilePickers
{
    /// <inheritdoc cref="PickOpenFileAsync(nint,string?,IReadOnlyList{FileDialogFilter}?,bool)"/>
    public static DialogResult PickOpenFile(
        IslandsWindow owner,
        string? title = null,
        IReadOnlyList<FileDialogFilter>? filters = null,
        bool multiSelect = false)
        => PickOpenFile((HWND)owner?.Handle!, title, filters, multiSelect);

    /// <summary>
    /// Shows the system Open File dialog.
    /// </summary>
    /// <param name="owner">Raw owner HWND.</param>
    /// <param name="title">Optional dialog title. Pass <see langword="null"/> to use the OS default.</param>
    /// <param name="filters">Optional list of <see cref="FileDialogFilter"/> entries.</param>
    /// <param name="multiSelect">
    /// When <see langword="true"/>, the user can select multiple files;
    /// results are in <see cref="DialogResult.Paths"/>.
    /// </param>
    public static unsafe DialogResult PickOpenFile(
        nint owner,
        string? title = null,
        IReadOnlyList<FileDialogFilter>? filters = null,
        bool multiSelect = false)
        => PickOpenFileCore(new HWND((void*)owner), title, filters, multiSelect);

    private static unsafe DialogResult PickOpenFileCore(
        HWND hwnd,
        string? title,
        IReadOnlyList<FileDialogFilter>? filters,
        bool multiSelect)
    {
        using var dialog = Shell.CreateOpenDialog();
        try
        {
            // Options
            uint options;
            dialog.Get()->GetOptions(&options);
            options |= FOS.ForceFilesystem | FOS.PathMustExist | FOS.FileMustExist;
            if (multiSelect) options |= FOS.AllowMultiSelect;
            dialog.Get()->SetOptions(options);

            using var titlePtr = new ManagedPtr<char>(title ?? string.Empty);

            if (title is not null)
                dialog.Get()->SetTitle(titlePtr);

            ApplyFilters(dialog.Get(), filters);

            var hr = dialog.Get()->Show(hwnd);
            if (hr == DialogHelpers.HRESULT_Cancelled)
                return DialogResult.Cancelled();
            Marshal.ThrowExceptionForHR(hr);

            if (multiSelect)
            {
                using ComPtr<IShellItemArray> psia = default;
                dialog.Get()->GetResults(psia.GetAddressOf());
                return DialogResult.Multi(DialogHelpers.GetPaths(psia.Get()));
            }
            else
            {
                using ComPtr<IShellItem> psi = default;
                dialog.Get()->GetResult(psi.GetAddressOf());
                var path = DialogHelpers.GetPath(psi.Get());
                return path is null ? DialogResult.Cancelled() : DialogResult.Single(path);
            }
        }
        finally
        {
            //Marshal.ReleaseComObject(dialog);
        }
    }

    /// <inheritdoc cref="PickSaveFile(nint,string?,string?,IReadOnlyList{FileDialogFilter}?)"/>
    public static DialogResult PickSaveFile(
        IslandsWindow owner,
        string? title = null,
        string? defaultFileName = null,
        IReadOnlyList<FileDialogFilter>? filters = null)
        => PickSaveFile((HWND)owner?.Handle!, title, defaultFileName, filters);

    /// <summary>
    /// Shows the system Save File dialog.
    /// </summary>
    /// <param name="owner">Raw owner HWND.</param>
    /// <param name="title">Optional dialog title.</param>
    /// <param name="defaultFileName">Pre-filled filename in the text box.</param>
    /// <param name="filters">Optional filter list.</param>
    public static unsafe DialogResult PickSaveFile(
        nint owner,
        string? title = null,
        string? defaultFileName = null,
        IReadOnlyList<FileDialogFilter>? filters = null)
        => PickSaveFileCore(new HWND((void*)owner), title, defaultFileName, filters);

    private static unsafe DialogResult PickSaveFileCore(
        HWND hwnd,
        string? title,
        string? defaultFileName,
        IReadOnlyList<FileDialogFilter>? filters)
    {
        using var dialog = Shell.CreateSaveDialog();
        try
        {
            uint options;
            dialog.Get()->GetOptions(&options);
            options |= FOS.ForceFilesystem | FOS.OverwritePrompt;
            dialog.Get()->SetOptions(options);

            using var titlePtr = new ManagedPtr<char>(title ?? string.Empty);

            if (title is not null)
                dialog.Get()->SetTitle(titlePtr);

            using var defaultFileNamePtr = new ManagedPtr<char>(defaultFileName ?? string.Empty);

            if (defaultFileName is not null)
                dialog.Get()->SetFileName(defaultFileNamePtr);

            ApplyFilters(dialog.Get(), filters);

            var hr = dialog.Get()->Show(hwnd);
            if (hr == DialogHelpers.HRESULT_Cancelled)
                return DialogResult.Cancelled();
            Marshal.ThrowExceptionForHR(hr);

            using ComPtr<IShellItem> psi = default;
            dialog.Get()->GetResult(psi.GetAddressOf());
            var path = DialogHelpers.GetPath(psi.Get());
            return path is null ? DialogResult.Cancelled() : DialogResult.Single(path);
        }
        finally
        {
            //Marshal.ReleaseComObject(dialog);
        }
    }

    /// <inheritdoc cref="PickFolder(nint,string?)"/>
    public static DialogResult PickFolder(
        IslandsWindow owner,
        string? title = null)
        => PickFolder((HWND)owner?.Handle!, title);

    /// <summary>
    /// Shows the system folder picker (IFileOpenDialog in PickFolders mode).
    /// </summary>
    /// <param name="owner">Raw owner HWND.</param>
    /// <param name="title">Optional dialog title.</param>
    public static unsafe DialogResult PickFolder(
        nint owner,
        string? title = null)
        => PickFolderCore(new HWND((void*)owner), title);

    private static unsafe DialogResult PickFolderCore(HWND hwnd, string? title)
    {
        using var dialog = Shell.CreateOpenDialog();
        try
        {
            uint options;
            dialog.Get()->GetOptions(&options);
            options |= FOS.PickFolders | FOS.ForceFilesystem | FOS.PathMustExist;
            dialog.Get()->SetOptions(options);

            using var titlePtr = new ManagedPtr<char>(title ?? string.Empty);

            if (title is not null)
                dialog.Get()->SetTitle(titlePtr);

            var hr = dialog.Get()->Show(hwnd);
            if (hr == DialogHelpers.HRESULT_Cancelled)
                return DialogResult.Cancelled();
            Marshal.ThrowExceptionForHR(hr);

            using ComPtr<IShellItem> psi = default;
            dialog.Get()->GetResult(psi.GetAddressOf());
            var path = DialogHelpers.GetPath(psi.Get());
            return path is null ? DialogResult.Cancelled() : DialogResult.Single(path);
        }
        finally
        {
            //Marshal.ReleaseComObject(dialog);
        }
    }

    private static unsafe void ApplyFilters(IFileOpenDialog* dialog, IReadOnlyList<FileDialogFilter>? filters)
    {
        var specs = BuildFilterSpecs(filters);
        if (specs.Length == 0) return;
        using var specsPtr = new ManagedPtr<COMDLG_FILTERSPEC>(specs);
        dialog->SetFileTypes((uint)specs.Length, specsPtr);
    }

    private static unsafe void ApplyFilters(IFileSaveDialog* dialog, IReadOnlyList<FileDialogFilter>? filters)
    {
        var specs = BuildFilterSpecs(filters);
        using var specsPtr = new ManagedPtr<COMDLG_FILTERSPEC>(specs);
        if (specs.Length == 0) return;
        dialog->SetFileTypes((uint)specs.Length, specsPtr);
        foreach (ref var spec in specs.AsSpan())
        {
            NativeMemory.Free(spec.pszName);
            NativeMemory.Free(spec.pszSpec);
        }
    }

    internal static unsafe COMDLG_FILTERSPEC[] BuildFilterSpecs(IReadOnlyList<FileDialogFilter>? filters)
    {
        if (filters is null || filters.Count == 0)
            return [];

        var specs = new COMDLG_FILTERSPEC[filters.Count];
        for (var i = 0; i < filters.Count; i++)
        {
            var name = filters[i].DisplayName;
            var spec = filters[i].NormalizedPattern;

            var namePtr = (char*)NativeMemory.Alloc((nuint)(name.Length + 1), sizeof(char));
            var specPtr = (char*)NativeMemory.Alloc((nuint)(spec.Length + 1), sizeof(char));

            name.AsSpan().CopyTo(new Span<char>(namePtr, name.Length));
            spec.AsSpan().CopyTo(new Span<char>(specPtr, spec.Length));

            namePtr[name.Length] = '\0';
            specPtr[spec.Length] = '\0';

            specs[i] = new COMDLG_FILTERSPEC { pszName = namePtr, pszSpec = specPtr };
        }
        return specs;
    }
}