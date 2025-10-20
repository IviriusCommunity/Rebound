using Rebound.Core.Helpers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Rebound.Forge;

internal class DLLInjector
{
    public static bool CanOpenProcess(int pid)
    {
        var hProcess = TerraFX.Interop.Windows.Windows.OpenProcess(PROCESS_ALL_ACCESS, bInheritHandle: false, dwProcessId: (uint)pid);
        if (hProcess == IntPtr.Zero) return false;
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
        return true;
    }

    private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint MEM_RELEASE = 0x8000;

    public static unsafe bool Inject(uint pid, string dllPath, uint waitTimeoutMs = 10_000)
    {
        // Obtain a handle to the target process
        var hProcess = TerraFX.Interop.Windows.Windows.OpenProcess(
            dwDesiredAccess: PROCESS_ALL_ACCESS,
            bInheritHandle: false,
            dwProcessId: pid);

        // Skip if the handle is invalid, most likely due to insufficient permissions
        if (hProcess == IntPtr.Zero)
        {
            return false;
        }

        // Obtain raw bytes of the DLL path and allocate memory in the target process
        var dllPathBytes = System.Text.Encoding.Unicode.GetBytes(dllPath + "\0");
        var allocMem = TerraFX.Interop.Windows.Windows.VirtualAllocEx(
            hProcess,
            lpAddress: null,
            dwSize: (uint)dllPathBytes.Length,
            flAllocationType: MEM_COMMIT | MEM_RESERVE,
            flProtect: PAGE_READWRITE);

        // Return false if memory allocation failed, likely due to insufficient permissions or memory issues
        if ((nint)allocMem == IntPtr.Zero)
        {
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        fixed (byte* pBytes = dllPathBytes)
        {
            // Write the DLL path into the allocated memory of the target process
            if (!TerraFX.Interop.Windows.Windows.WriteProcessMemory(
                hProcess,
                lpBaseAddress: allocMem,
                lpBuffer: pBytes,
                nSize: (uint)dllPathBytes.Length,
                lpNumberOfBytesWritten: null))
            {
                // Writing memory failed, clean up and return false
                _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }
        }

        // Get the address of LoadLibraryW in kernel32.dll
        var hKernel32 = TerraFX.Interop.Windows.Windows.GetModuleHandleW("kernel32.dll".ToPCWSTR().Value);
        var loadLibraryAddr = TerraFX.Interop.Windows.Windows.GetProcAddress(
            hKernel32,
            (sbyte*)Marshal.StringToHGlobalAnsi("LoadLibraryW"));

        // Return false if the program couldn't get the address of LoadLibraryW
        if (loadLibraryAddr == null)
        {
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Transform the address to a callable function pointer
        var loadLibraryFunc = (delegate* unmanaged<void*, uint>)loadLibraryAddr;

        // Create a remote thread in the target process to execute LoadLibraryW with the DLL path
        var hThread = TerraFX.Interop.Windows.Windows.CreateRemoteThread(
            hProcess,
            null,
            0,
            loadLibraryFunc,
            allocMem,
            0,
            null
        );

        if (hThread == IntPtr.Zero)
        {
            // Thread creation failed, clean up and return false
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Wait with timeout
        uint waitResult = TerraFX.Interop.Windows.Windows.WaitForSingleObject(hThread, waitTimeoutMs);

        uint exitCode = 0;

        // Check the result of the wait operation
        if (waitResult == TerraFX.Interop.Windows.WAIT.WAIT_OBJECT_0)
        {
            _ = TerraFX.Interop.Windows.Windows.GetExitCodeThread(hThread, &exitCode);
        }

        // Handle timeout or failure
        else if (waitResult == TerraFX.Interop.Windows.WAIT.WAIT_TIMEOUT)
        {
            // Clean up handles and memory, but the remote thread may still be running inside LoadLibraryW.
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Other wait failures
        else
        {
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Normal cleanup
        _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);

        return true;
    }
}