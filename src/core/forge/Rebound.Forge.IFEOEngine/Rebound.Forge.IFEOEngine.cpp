#include <windows.h>
#include <string>
#include <vector>
#include <iostream>

bool PauseIFEOEntry(const std::wstring& exeName) {
    const std::wstring basePath = L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\";
    const std::wstring originalKeyPath = basePath + exeName;
    const std::wstring invalidKeyPath = basePath + L"INVALID" + exeName;

    HKEY hOriginal = nullptr;
    HKEY hInvalid = nullptr;

    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, originalKeyPath.c_str(), 0, KEY_READ | KEY_WRITE, &hOriginal) != ERROR_SUCCESS)
        return false;

    if (RegCreateKeyExW(HKEY_LOCAL_MACHINE, invalidKeyPath.c_str(), 0, nullptr, 0, KEY_WRITE, nullptr, &hInvalid, nullptr) != ERROR_SUCCESS) {
        RegCloseKey(hOriginal);
        return false;
    }

    DWORD index = 0;
    WCHAR valueName[256];
    BYTE data[1024];
    DWORD valueNameSize = 256;
    DWORD dataSize = sizeof(data);
    DWORD type;

    while (RegEnumValueW(hOriginal, index++, valueName, &valueNameSize, nullptr, &type, data, &dataSize) == ERROR_SUCCESS) {
        RegSetValueExW(hInvalid, valueName, 0, type, data, dataSize);

        valueNameSize = 256;
        dataSize = sizeof(data);
    }

    RegCloseKey(hOriginal);
    RegCloseKey(hInvalid);

    RegDeleteKeyW(HKEY_LOCAL_MACHINE, originalKeyPath.c_str()); // Delete original

    return true;
}

bool ResumeIFEOEntry(const std::wstring& exeName) {
    const std::wstring basePath = L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\";
    const std::wstring originalKeyPath = basePath + exeName;
    const std::wstring invalidKeyPath = basePath + L"INVALID" + exeName;

    HKEY hInvalid = nullptr;
    HKEY hOriginal = nullptr;

    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, invalidKeyPath.c_str(), 0, KEY_READ | KEY_WRITE, &hInvalid) != ERROR_SUCCESS)
        return false;

    if (RegCreateKeyExW(HKEY_LOCAL_MACHINE, originalKeyPath.c_str(), 0, nullptr, 0, KEY_WRITE, nullptr, &hOriginal, nullptr) != ERROR_SUCCESS) {
        RegCloseKey(hInvalid);
        return false;
    }

    DWORD index = 0;
    WCHAR valueName[256];
    BYTE data[1024];
    DWORD valueNameSize = 256;
    DWORD dataSize = sizeof(data);
    DWORD type;

    while (RegEnumValueW(hInvalid, index++, valueName, &valueNameSize, nullptr, &type, data, &dataSize) == ERROR_SUCCESS) {
        RegSetValueExW(hOriginal, valueName, 0, type, data, dataSize);

        valueNameSize = 256;
        dataSize = sizeof(data);
    }

    RegCloseKey(hInvalid);
    RegCloseKey(hOriginal);

    RegDeleteKeyW(HKEY_LOCAL_MACHINE, invalidKeyPath.c_str()); // Delete the invalid

    return true;
}

int main()
{
    std::wstring exe = L"winver.exe";

    if (PauseIFEOEntry(exe)) {
        std::wcout << L"IFEO entry paused.\n";
    }
    else {
        std::wcout << L"Failed to pause IFEO entry.\n";
    }

	ShellExecuteA(nullptr, "open", "winver.exe", nullptr, nullptr, SW_SHOWNORMAL);

    if (ResumeIFEOEntry(exe)) {
        std::wcout << L"IFEO entry resumed.\n";
    }
    else {
        std::wcout << L"Failed to resume IFEO entry.\n";
    }

    return 0;
}