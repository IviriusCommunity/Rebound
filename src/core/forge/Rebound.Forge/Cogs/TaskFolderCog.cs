// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using Rebound.Core.TaskScheduler.Native;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Cog responsible for managing a task folder within the Windows Task Scheduler.
/// </summary>
public class TaskFolderCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Manage task folder '{TaskFolderName}'."; }

    /// <summary>
    /// Task Scheduler APIs always require elevation.
    /// </summary>
    public bool RequiresElevation => true;

    /// <summary>
    /// Gets or sets the name of the task folder to be managed.
    /// </summary>
    public required string TaskFolderName { get; set; }

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            HRESULT hr;
            using ComPtr<ITaskService> taskService = default;

            using ManagedPtr<ushort> pszRoot = "\\";
            using ManagedPtr<ushort> pszFolder = TaskFolderName;

            ReboundLogger.WriteToLog(
                "TaskFolderCog Apply",
                "Apply started for TaskFolderCog with folder name: " + TaskFolderName);

            using ManagedPtr<Guid> iid = *ITaskService.IID;
            using ManagedPtr<Guid> clsid = new("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");

            hr = CoCreateInstance(
                clsid,
                null,
                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                iid,
                (void**)taskService.GetAddressOf());

            if (hr.FAILED || taskService.Get() is null)
            {
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Apply",
                    $"Failed to create ITaskService. HRESULT=0x{hr.Value:X}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "TASK_SERVICE_NOT_CREATED", false, false));
            }

            taskService.Get()->Connect(new(), new(), new(), new());

            ReboundLogger.WriteToLog(
                "TaskFolderCog Apply",
                "Connected to Task Scheduler successfully.");

            using ComPtr<ITaskFolder> rootFolder = default;
            hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hr.FAILED)
            {
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Apply",
                    $"Failed to get root folder. HRESULT=0x{hr.Value:X}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "NO_ROOT_FOLDER", false, false));
            }

            using ComPtr<ITaskFolder> reboundFolder = default;
            hr = taskService.Get()->GetFolder(pszFolder, reboundFolder.GetAddressOf());
            if (hr.FAILED || reboundFolder.Get() is null)
            {
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Apply",
                    "Task folder not found, attempting to create it.");
                hr = rootFolder.Get()->CreateFolder(pszFolder, new(), reboundFolder.GetAddressOf());
                if (hr.FAILED)
                {
                    ReboundLogger.WriteToLog(
                        "TaskFolderCog Apply",
                        $"Failed to create task folder. HRESULT=0x{hr.Value:X}",
                        LogMessageSeverity.Error);
                    return Task.FromResult(new CogOperationResult(false, "FOLDER_NOT_CREATED", false, false));
                }
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Apply",
                    "Successfully created task folder.");
            }

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "TaskFolderCog Apply",
                "ApplyAsync failed with exception.",
                LogMessageSeverity.Error,
                ex);

            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            HRESULT hr;
            using ComPtr<ITaskService> taskService = default;

            using ManagedPtr<ushort> pszRoot = "\\";
            using ManagedPtr<ushort> pszFolder = TaskFolderName;

            ReboundLogger.WriteToLog(
                "TaskFolderCog Remove",
                "Remove started for TaskFolderCog with folder name: " + TaskFolderName);

            using ManagedPtr<Guid> iid = *ITaskService.IID;
            using ManagedPtr<Guid> clsid = new("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");

            hr = CoCreateInstance(
                clsid,
                null,
                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                iid,
                (void**)taskService.GetAddressOf());

            if (hr.FAILED || taskService.Get() is null)
            {
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Remove",
                    $"Failed to create ITaskService. HRESULT=0x{hr.Value:X}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "TASK_SERVICE_NOT_CREATED", false, false));
            }

            taskService.Get()->Connect(new(), new(), new(), new());
            
            ReboundLogger.WriteToLog(
                "TaskFolderCog Remove",
                "Connected to Task Scheduler.");

            using ComPtr<ITaskFolder> rootFolder = default;
            hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hr.FAILED)
            {
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Remove",
                    $"Failed to get root folder. HRESULT=0x{hr.Value:X}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "ROOT_FOLDER_NOT_FOUND", false, false));
            }

            using ComPtr<ITaskFolder> reboundFolder = default;
            hr = taskService.Get()->GetFolder(pszFolder, reboundFolder.GetAddressOf());
            if (hr.FAILED || reboundFolder.Get() is null)
            {
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Remove",
                    "Task folder not found, nothing to remove.",
                    LogMessageSeverity.Warning);
            }
            else
            {
                rootFolder.Get()->DeleteFolder(pszFolder, new());
                ReboundLogger.WriteToLog(
                    "TaskFolderCog Remove",
                    "Task folder removed.");
            }

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "TaskFolderCog Remove",
                "RemoveAsync failed with exception.",
                LogMessageSeverity.Error,
                ex);

            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public unsafe Task<CogStatus> GetStatusAsync()
    {
        try
        {
            HRESULT hr;
            using ComPtr<ITaskService> taskService = default;

            using ManagedPtr<ushort> pszRoot = "\\";
            using ManagedPtr<ushort> pszFolder = TaskFolderName;

            ReboundLogger.WriteToLog(
                "TaskFolderCog GetStatus",
                "GetStatus started for TaskFolderCog with folder name: " + TaskFolderName);

            using ManagedPtr<Guid> iid = *ITaskService.IID;
            using ManagedPtr<Guid> clsid = new("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");

            hr = CoCreateInstance(
                clsid,
                null,
                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                iid,
                (void**)taskService.GetAddressOf());

            if (hr.FAILED || taskService.Get() is null)
            {
                ReboundLogger.WriteToLog(
                    "TaskFolderCog GetStatus",
                    $"Failed to create ITaskService. HRESULT=0x{hr.Value:X}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogStatus(CogState.Unknown, "Could not create Task Service instance."));
            }

            taskService.Get()->Connect(new(), new(), new(), new());

            ReboundLogger.WriteToLog(
                "TaskFolderCog GetStatus",
                "Connected to Task Scheduler.");

            using ComPtr<ITaskFolder> reboundFolder = default;
            hr = taskService.Get()->GetFolder(pszFolder, reboundFolder.GetAddressOf());
            if (hr.FAILED || reboundFolder.Get() is null)
                return Task.FromResult(new CogStatus(CogState.Unknown, "Task folder not found."));

            return Task.FromResult(new CogStatus(CogState.Installed, "Task folder found."));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "TaskFolderCog GetStatus",
                "GetStatusAsync failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogStatus(CogState.Unknown, "An error occurred while getting task folder status."));
        }
    }
}