#include "framework.h"
#include "Rebound.About.Launcher.h"
#include <string>
#include <iostream>
#include <vector>

// Standard because yes
using namespace std;

// Package family name for the required app
string packageFamilyName = "Your_app_package_family_name_here";

// Arguments for launching the app
string args = "Your_args_here";

// PowerShell command
string command = "Start-Process 'shell:AppsFolder\\" + packageFamilyName + "!App' -ArgumentList @(' " + args + " ')";

// Convert string to wide string
static wstring StringToWString(const string& str)
{
    // Step 1: Get the length of the wide-character version of the input string, including the null terminator.
    int requiredSize = MultiByteToWideChar(CP_ACP, 0, str.c_str(), -1, NULL, 0);

    // Step 2: Create a wide string (wstring) with enough space to hold the converted string.
    wstring wideString(requiredSize, 0);

    // Step 3: Convert the narrow string to a wide-character string.
    MultiByteToWideChar(CP_ACP, 0, str.c_str(), -1, &wideString[0], requiredSize);

    // Return the resulting wide-character string.
    return wideString;
}

// Function to check if the application is running as admin
static BOOL IsRunningAsAdmin()
{
    BOOL isAdmin = FALSE;
    PSID adminGroup = NULL;

    // Define the NT Authority SID, which is the security identifier for system-level authorities.
    SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;

    // Step 1: Allocate and initialize a SID for the Administrators group.
    if (AllocateAndInitializeSid(&NtAuthority, 2,
        SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS,
        0, 0, 0, 0, 0, 0,
        &adminGroup))
    {
        // Step 2: Check if the current token is a member of the Administrators group.
        if (CheckTokenMembership(NULL, adminGroup, &isAdmin)) isAdmin = isAdmin ? TRUE : FALSE;

        // Step 3: Free the allocated SID.
        FreeSid(adminGroup);
    }

    return isAdmin;
}

// Function to run a PowerShell command
static void RunPowerShellCommand(const string& command, BOOL runAsAdmin)
{
    string powershellCommand = command;

    // If admin privileges are required, add the `-Verb RunAs` flag to the PowerShell command.
    if (runAsAdmin) powershellCommand = "powershell.exe -Command \"" + powershellCommand + "\" -Verb RunAs";
    else powershellCommand = "powershell.exe -Command \"" + powershellCommand + "\"";

    // Convert the string to a wide string for compatibility with Windows API.
    wstring wideCommand = StringToWString(powershellCommand);
    vector<wchar_t> commandBuffer(wideCommand.begin(), wideCommand.end());
    commandBuffer.push_back(L'\0'); // Ensure the string is null-terminated.

    STARTUPINFO si = { sizeof(STARTUPINFO) };
    si.dwFlags |= STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE; // Hide the PowerShell window.

    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));

    // Create the PowerShell process.
    BOOL result = CreateProcess(
        nullptr,                     // Use command line (no module name).
        commandBuffer.data(),        // Command to run.
        nullptr,                     // Process handle not inheritable.
        nullptr,                     // Thread handle not inheritable.
        FALSE,                       // Inheritance flag.
        0,                           // Creation flags.
        nullptr,                     // Parent's environment variables.
        nullptr,                     // Parent's working directory.
        &si,                         // Startup info.
        &pi                          // Process information.
    );

    if (result)
    {
        // Process created successfully.
        wcout << L"Process started successfully.\n";

        // Wait for the process to exit.
        WaitForSingleObject(pi.hProcess, INFINITE);

        // Close handles.
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
    else
    {
        // Process creation failed. Print the error.
        DWORD error = GetLastError();
        wcerr << L"CreateProcess failed with error code " << error << L".\n";

        LPVOID msgBuffer = nullptr;
        FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
            nullptr, error, 0, (LPWSTR)&msgBuffer, 0, nullptr);
        wcerr << L"Error message: " << (LPWSTR)msgBuffer << endl;
        LocalFree(msgBuffer);
    }
}

// App entry method
int APIENTRY wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE hPrevInstance, _In_ LPWSTR lpCmdLine, _In_ int nCmdShow)
{
    // Check if the application is running as admin
    BOOL isAdmin = IsRunningAsAdmin();

    // Run the PowerShell command
    RunPowerShellCommand(command, isAdmin);

    return 0;
}