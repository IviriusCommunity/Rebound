#include <windows.h>
#include <string>
#include <iostream>
#include <vector>

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