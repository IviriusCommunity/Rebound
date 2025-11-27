// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.TaskScheduler;

namespace Rebound.Forge.Cogs
{
    /// <summary>
    /// Cog responsible for managing a task folder within the Windows Task Scheduler.
    /// </summary>
    public class TaskFolderCog : ICog
    {
        /// <summary>
        /// Gets or sets the name of the task folder to be managed.
        /// </summary>
        public required string Name { get; set; }

        /// <inheritdoc/>
        public bool Ignorable { get; }

        /// <inheritdoc/>
        public string TaskDescription { get => $"Create a Task Scheduler folder at \\{Name}"; }

        /// <inheritdoc/>
        public async unsafe Task ApplyAsync()
        {
            try
            {
                HRESULT hr;
                using ComPtr<ITaskService> taskService = default;

                using var pszRoot = PInvoke.SysAllocString("\\");
                using var pszFolder = PInvoke.SysAllocString(Name);

                ReboundLogger.Log("[TaskFolderCog] Apply started.");

                fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
                {
                    hr = PInvoke.CoCreateInstance(
                        CLSID.CLSID_TaskScheduler,
                        null,
                        CLSCTX.CLSCTX_INPROC_SERVER,
                        iidITaskService,
                        (void**)taskService.GetAddressOf());
                }

                if (hr.Failed || taskService.Get() is null)
                {
                    ReboundLogger.Log($"[TaskFolderCog] Failed to create ITaskService. HRESULT=0x{hr.Value:X}");
                    return;
                }

                taskService.Get()->Connect(new(), new(), new(), new());
                ReboundLogger.Log("[TaskFolderCog] Connected to Task Scheduler.");

                using ComPtr<ITaskFolder> rootFolder = null;
                hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
                if (hr.Failed)
                {
                    ReboundLogger.Log($"[TaskFolderCog] Failed to get root folder. HRESULT=0x{hr.Value:X}");
                    return;
                }

                using ComPtr<ITaskFolder> reboundFolder = null;
                hr = taskService.Get()->GetFolder(pszFolder, reboundFolder.GetAddressOf());
                if (hr.Failed || reboundFolder.Get() is null)
                {
                    ReboundLogger.Log("[TaskFolderCog] Task folder not found, attempting to create it.");
                    hr = rootFolder.Get()->CreateFolder(pszFolder, new(), reboundFolder.GetAddressOf());
                    if (hr.Failed)
                    {
                        ReboundLogger.Log($"[TaskFolderCog] Failed to create task folder. HRESULT=0x{hr.Value:X}");
                        return;
                    }
                    ReboundLogger.Log("[TaskFolderCog] Successfully created task folder.");
                }
            }
            catch (Exception ex)
            {
                ReboundLogger.Log("[TaskFolderCog] ApplyAsync failed with exception.", ex);
            }
        }

        /// <inheritdoc/>
        public async unsafe Task RemoveAsync()
        {
            try
            {
                HRESULT hr;
                using ComPtr<ITaskService> taskService = default;

                using var pszRoot = PInvoke.SysAllocString("\\");
                using var pszFolder = PInvoke.SysAllocString(Name);

                ReboundLogger.Log("[TaskFolderCog] Remove started.");

                fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
                {
                    hr = PInvoke.CoCreateInstance(
                        CLSID.CLSID_TaskScheduler,
                        null,
                        CLSCTX.CLSCTX_INPROC_SERVER,
                        iidITaskService,
                        (void**)taskService.GetAddressOf());
                }

                if (hr.Failed || taskService.Get() is null)
                {
                    ReboundLogger.Log($"[TaskFolderCog] Failed to create ITaskService. HRESULT=0x{hr.Value:X}");
                    return;
                }

                taskService.Get()->Connect(new(), new(), new(), new());
                ReboundLogger.Log("[TaskFolderCog] Connected to Task Scheduler.");

                using ComPtr<ITaskFolder> rootFolder = null;
                hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
                if (hr.Failed)
                {
                    ReboundLogger.Log($"[TaskFolderCog] Failed to get root folder. HRESULT=0x{hr.Value:X}");
                    return;
                }

                using ComPtr<ITaskFolder> reboundFolder = null;
                hr = taskService.Get()->GetFolder(pszFolder, reboundFolder.GetAddressOf());
                if (hr.Failed || reboundFolder.Get() is null)
                {
                    ReboundLogger.Log("[TaskFolderCog] Task folder not found, nothing to remove.");
                    return;
                }
                else
                {
                    rootFolder.Get()->DeleteFolder(pszFolder, 0);
                    ReboundLogger.Log("[TaskFolderCog] Task folder removed.");
                }
            }
            catch (Exception ex)
            {
                ReboundLogger.Log("[TaskFolderCog] RemoveAsync failed with exception.", ex);
            }
        }

        /// <inheritdoc/>
        public async unsafe Task<bool> IsAppliedAsync()
        {
            try
            {
                HRESULT hr;
                using ComPtr<ITaskService> taskService = default;

                using var pszRoot = PInvoke.SysAllocString("\\");
                using var pszFolder = PInvoke.SysAllocString(Name);

                fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
                {
                    hr = PInvoke.CoCreateInstance(
                        CLSID.CLSID_TaskScheduler,
                        null,
                        CLSCTX.CLSCTX_INPROC_SERVER,
                        iidITaskService,
                        (void**)taskService.GetAddressOf());
                }

                if (hr.Failed || taskService.Get() is null)
                {
                    ReboundLogger.Log($"[TaskFolderCog] Failed to create ITaskService. HRESULT=0x{hr.Value:X}");
                    return false;
                }

                taskService.Get()->Connect(new(), new(), new(), new());

                using ComPtr<ITaskFolder> reboundFolder = null;
                hr = taskService.Get()->GetFolder(pszFolder, reboundFolder.GetAddressOf());
                if (hr.Failed || reboundFolder.Get() is null)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                ReboundLogger.Log("[TaskFolderCog] IsAppliedAsync failed with exception.", ex);
                return false;
            }
        }
    }
}
