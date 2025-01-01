#include "framework.h"
#include "Rebound.About.Launcher.h"
#include <string>
#include <iostream>
#include <vector>

using namespace std;

// Package family name for the required app
std::string packageFamilyName = "ReboundAbout_yejd587sfa94t";

// Arguments for launching the app
std::string args = "";

// PowerShell command
std::string command = "Start-Process 'shell:AppsFolder\\" + packageFamilyName + "!App' -ArgumentList @(' " + args + " ')";

// Function to convert std::string to std::wstring
std::wstring StringToWString(const std::string& str) {
    int len;
    int slength = (int)str.length() + 1;
    len = MultiByteToWideChar(CP_ACP, 0, str.c_str(), slength, NULL, 0);
    std::wstring wstr(len, 0);
    MultiByteToWideChar(CP_ACP, 0, str.c_str(), slength, &wstr[0], len);
    return wstr;
}

// Function to check if the application is running as admin
BOOL IsRunningAsAdmin() {
    BOOL isAdmin = FALSE;
    PSID adminGroup = NULL;
    SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;

    if (AllocateAndInitializeSid(&NtAuthority, 2,
        SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS,
        0, 0, 0, 0, 0, 0,
        &adminGroup)) {
        if (CheckTokenMembership(NULL, adminGroup, &isAdmin)) {
            isAdmin = isAdmin ? TRUE : FALSE;
        }
        FreeSid(adminGroup);
    }

    return isAdmin;
}

// Function to run a PowerShell command
void RunPowerShellCommand(const std::string& command, BOOL runAsAdmin) {
    std::string powershellCommand = command;

    if (runAsAdmin) {
        powershellCommand = "powershell.exe -Command \"" + powershellCommand + "\" -Verb RunAs";
    }
    else {
        powershellCommand = "powershell.exe -Command \"" + powershellCommand + "\"";
    }

    std::wstring wideCommand = StringToWString(powershellCommand);
    std::vector<wchar_t> commandBuffer(wideCommand.begin(), wideCommand.end());
    commandBuffer.push_back(L'\0'); // Null-terminate the string

    STARTUPINFO si = { sizeof(STARTUPINFO) };
    si.dwFlags |= STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE; // Hide the PowerShell window

    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));

    BOOL result = CreateProcess(
        nullptr,                     // No module name (use command line)
        commandBuffer.data(),        // Command line (wide char array)
        nullptr,                     // Process handle not inheritable
        nullptr,                     // Thread handle not inheritable
        FALSE,                       // Set handle inheritance to FALSE
        0,                           // No creation flags
        nullptr,                     // Use parent's environment block
        nullptr,                     // Use parent's starting directory 
        &si,                         // Pointer to STARTUPINFO structure
        &pi);                        // Pointer to PROCESS_INFORMATION structure

    if (result) {
        // Successfully created the process
        std::wcout << L"Process started successfully.\n";

        // Wait until the PowerShell process exits
        WaitForSingleObject(pi.hProcess, INFINITE);

        // Close process and thread handles
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
    else {
        // Error occurred
        DWORD error = GetLastError();
        std::wcerr << L"CreateProcess failed with error code " << error << L".\n";

        // Convert error code to a message
        LPVOID msgBuffer;
        FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
            nullptr, error, 0, (LPWSTR)&msgBuffer, 0, nullptr);
        std::wcerr << L"Error message: " << (LPWSTR)msgBuffer << std::endl;
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