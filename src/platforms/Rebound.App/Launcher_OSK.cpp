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

void RunShellCommand() {
    // Define the path with %PROGRAMFILES% environment variable
    std::string command = "%PROGRAMFILES%\\Common Files\\Microsoft Shared\\ink\\TabTip.exe";

    // Expand the environment variables (e.g., %PROGRAMFILES% -> actual path)
    char expandedPath[MAX_PATH];
    ExpandEnvironmentStringsA(command.c_str(), expandedPath, sizeof(expandedPath));

    // Launch the program using ShellExecute
    HINSTANCE hInst = ShellExecuteA(
        nullptr,        // Parent window handle (nullptr for no parent)
        "open",         // Action: open the program
        expandedPath,   // Path to the executable
        nullptr,        // Command line arguments (none in this case)
        nullptr,        // Default directory (nullptr to use the current directory)
        SW_SHOWNORMAL   // Show the window normally
    );

    // Check for errors
    if ((intptr_t)hInst <= 32) {
        DWORD error = GetLastError();
        std::cerr << "ShellExecute failed with error code " << error << ".\n";

        // Convert error code to a message
        LPVOID msgBuffer;
        FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
            nullptr, error, 0, (LPSTR)&msgBuffer, 0, nullptr);
        std::cerr << "Error message: " << (LPSTR)msgBuffer << std::endl;
        LocalFree(msgBuffer);
    }
    else {
        std::cout << "Process started successfully.\n";
    }
}

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
    _In_opt_ HINSTANCE hPrevInstance,
    _In_ LPWSTR lpCmdLine,
    _In_ int nCmdShow)
{
    RunShellCommand();

    return 0;
}
