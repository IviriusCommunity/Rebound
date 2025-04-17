// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

#include <windows.h>
#include <shobjidl.h>
#include <shlobj_core.h>
#include <combaseapi.h>
#include <iostream>

#pragma comment(lib, "Ole32.lib")

bool IsRunningAsAdmin()
{
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

bool LaunchPackagedApp(const std::wstring& appUserModelID, const std::wstring& arguments)
{
    HRESULT hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (FAILED(hr)) {
        std::wcerr << L"CoInitializeEx failed: 0x" << std::hex << hr << std::endl;
        return false;
    }

    IApplicationActivationManager* pActivator = nullptr;
    hr = CoCreateInstance(
        CLSID_ApplicationActivationManager,
        nullptr,
        CLSCTX_LOCAL_SERVER,
        IID_PPV_ARGS(&pActivator)
    );

    if (FAILED(hr)) {
        std::wcerr << L"CoCreateInstance failed: 0x" << std::hex << hr << std::endl;
        CoUninitialize();
        return false;
    }

    DWORD pid = 0;
    hr = pActivator->ActivateApplication(
        appUserModelID.c_str(),
        arguments.c_str(),
        IsRunningAsAdmin() ? (ACTIVATEOPTIONS)0x20000000 : AO_NONE,
        &pid
    );

    pActivator->Release();
    CoUninitialize();

    if (FAILED(hr)) {
        std::wcerr << L"ActivateApplication failed: 0x" << std::hex << hr << std::endl;
        return false;
    }

    std::wcout << L"Launched packaged app with PID: " << pid << std::endl;
    return true;
}

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
    _In_opt_ HINSTANCE hPrevInstance,
    _In_ LPWSTR lpCmdLine,
    _In_ int nCmdShow)
{
    std::wstring appId = L"ReboundAbout_yejd587sfa94t!App";
    std::wstring args = L"";

    LaunchPackagedApp(appId, args);

    return 0;
}