#include "framework.h"
#include "Rebound.Launcher.h"
#include <string>

void LaunchUWPApp(const std::wstring& packageFamilyName, const std::wstring& appId, const std::wstring& arguments)
{
    std::wstring command = L"powershell -WindowStyle Hidden -Command \"Start-Process 'shell:AppsFolder\\" +
        packageFamilyName + L"!" + appId +
        L"' -ArgumentList '" + arguments + L"' -NoNewWindow -PassThru\"";

    STARTUPINFO si = { sizeof(si) };
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;

    PROCESS_INFORMATION pi;
    if (CreateProcessW(nullptr, &command[0], nullptr, nullptr, FALSE, CREATE_NO_WINDOW, nullptr, nullptr, &si, &pi))
    {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
}

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
                     _In_opt_ HINSTANCE hPrevInstance,
                     _In_ LPWSTR    lpCmdLine,
                     _In_ int       nCmdShow)
{
    std::wstring argument(lpCmdLine);

    if (argument == L"winver")
    {
        LaunchUWPApp(L"ReboundAbout_yejd587sfa94t", L"App", L"");
    }

    return 0;
}