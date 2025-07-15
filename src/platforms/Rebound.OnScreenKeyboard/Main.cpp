#include <windows.h>
#include <shlobj.h>   // For SHGetKnownFolderPath
#include <strsafe.h>

#pragma comment(lib, "shell32.lib")

int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int)
{
    PWSTR commonPath = nullptr;
    HRESULT hr = SHGetKnownFolderPath(FOLDERID_ProgramFilesCommon, 0, NULL, &commonPath);

    if (SUCCEEDED(hr))
    {
        wchar_t fullPath[MAX_PATH];
        StringCchPrintfW(fullPath, MAX_PATH, L"%s\\Microsoft Shared\\ink\\TabTip.exe", commonPath);

        ShellExecuteW(NULL, L"open", fullPath, NULL, NULL, SW_SHOWNORMAL);
        CoTaskMemFree(commonPath);
    }

    return 0;
}