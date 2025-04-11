#include <windows.h>
#include <string>
#include <iostream>
#include <vector>

// Function to convert std::string to std::wstring
std::wstring StringToWString(const std::string& str) {
    int len;
    int slength = (int)str.length() + 1;
    len = MultiByteToWideChar(CP_ACP, 0, str.c_str(), slength, NULL, 0);
    std::wstring wstr(len, 0);
    MultiByteToWideChar(CP_ACP, 0, str.c_str(), slength, &wstr[0], len);
    return wstr;
}

// Function to convert std::wstring to std::string
std::string WStringToString(const std::wstring& wstr) {
    int len;
    int wlength = (int)wstr.length() + 1;
    len = WideCharToMultiByte(CP_ACP, 0, wstr.c_str(), wlength, NULL, 0, NULL, NULL);
    std::string str(len, 0);
    WideCharToMultiByte(CP_ACP, 0, wstr.c_str(), wlength, &str[0], len, NULL, NULL);
    return str;
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

    std::string ansiCommand = powershellCommand;
    std::vector<char> commandBuffer(ansiCommand.begin(), ansiCommand.end());
    commandBuffer.push_back('\0'); // Null-terminate the string

    STARTUPINFOA si = { sizeof(STARTUPINFOA) };
    si.dwFlags |= STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE; // Hide the PowerShell window

    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));

    BOOL result = CreateProcessA(
        nullptr,                     // No module name (use command line)
        commandBuffer.data(),        // Command line (ANSI char array)
        nullptr,                     // Process handle not inheritable
        nullptr,                     // Thread handle not inheritable
        FALSE,                       // Set handle inheritance to FALSE
        0,                           // No creation flags
        nullptr,                     // Use parent's environment block
        nullptr,                     // Use parent's starting directory 
        &si,                         // Pointer to STARTUPINFOA structure
        &pi);                        // Pointer to PROCESS_INFORMATION structure

    if (result) {
        // Successfully created the process
        std::cout << "Process started successfully.\n";

        // Wait until the PowerShell process exits
        WaitForSingleObject(pi.hProcess, INFINITE);

        // Close process and thread handles
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
    else {
        // Error occurred
        DWORD error = GetLastError();
        std::cerr << "CreateProcess failed with error code " << error << ".\n";

        // Convert error code to a message
        LPVOID msgBuffer;
        FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
            nullptr, error, 0, (LPSTR)&msgBuffer, 0, nullptr);
        std::cerr << "Error message: " << (LPSTR)msgBuffer << std::endl;
        LocalFree(msgBuffer);
    }
}

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
    _In_opt_ HINSTANCE hPrevInstance,
    _In_ LPWSTR lpCmdLine,
    _In_ int nCmdShow)
{
    // Check if the application is running as admin
    BOOL isAdmin = IsRunningAsAdmin();

    std::wstring argument(lpCmdLine);
    // Define the PowerShell command to execute
    std::string command = "Start-Process 'shell:AppsFolder\\Rebound.Shell.ExperienceHost_34rd76tfyvk3e!App' -ArgumentList @(' ')";

    // Run the PowerShell command
    RunPowerShellCommand(command, isAdmin);
    return 0;
}