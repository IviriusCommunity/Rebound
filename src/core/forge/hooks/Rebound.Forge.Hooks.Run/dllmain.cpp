// hook_helpers.cpp
#include <windows.h>
#include <stdint.h>
#include <stdio.h>
#include "pch.h"
#include <stdint.h>  // C header (works in C and C++)
#include <process.h>

static HANDLE g_workerEvent = NULL;
static SLIST_HEADER g_messageList; // for lock-free message queue

// prototype
unsigned __stdcall InjectorInitThread(void* param);
void QueueMessage(const wchar_t* msg); // lightweight enqueue (caller must copy message or store pointer)

// helper to queue message from hook: allocate a small node and push to SList then signal worker
void QueueMessage(const wchar_t* msg) {
    struct MsgNode { SLIST_ENTRY link; wchar_t msg[256]; };
    MsgNode* node = (MsgNode*)HeapAlloc(GetProcessHeap(), 0, sizeof(MsgNode));
    if (!node) return;
    node->link.Next = NULL;
    wcsncpy_s(node->msg, msg, _TRUNCATE);
    InterlockedPushEntrySList(&g_messageList, (PSLIST_ENTRY)node);
    if (g_workerEvent) SetEvent(g_workerEvent);
}

static void FlushInsCache(void* addr, SIZE_T size) {
    FlushInstructionCache(GetCurrentProcess(), addr, size);
}

#ifdef _M_X64
const SIZE_T OVERWRITE_SIZE = 12; // mov rax, imm64 (10) + jmp rax (2) = 12 bytes
#else
const SIZE_T OVERWRITE_SIZE = 5;  // x86 relative jmp = 5 bytes
#endif

// A small struct that holds hook metadata
struct InlineHook {
    void* target;            // address of original function
    SIZE_T overwriteSize;    // bytes we overwrote
    BYTE originalBytes[64];  // saved original bytes (enough)
    void* trampoline;        // pointer to trampoline callable
};

// Helper to change page protection and write
static bool WriteMemory(void* dst, const void* src, SIZE_T len) {
    DWORD old;
    if (!VirtualProtect(dst, len, PAGE_EXECUTE_READWRITE, &old)) return false;
    memcpy(dst, src, len);
    FlushInsCache(dst, len);
    VirtualProtect(dst, len, old, &old);
    return true;
}

// Allocate trampoline, copy original bytes and append jmp back
static void* CreateTrampoline(void* target, SIZE_T overwriteSize) {
    // allocate executable memory
    SIZE_T trampSize = overwriteSize + 32; // extra for jump back
    void* tramp = VirtualAlloc(nullptr, trampSize, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!tramp) return nullptr;

    // copy original bytes
    memcpy(tramp, target, overwriteSize);

    // append jump back to original + overwriteSize
    BYTE* p = (BYTE*)tramp + overwriteSize;

#ifdef _M_X64
    // mov rax, imm64
    p[0] = 0x48; p[1] = 0xB8; // mov rax, imm64
    uint64_t retAddr = (uint64_t)((BYTE*)target + overwriteSize);
    memcpy(p + 2, &retAddr, sizeof(retAddr)); // imm64
    p[10] = 0xFF; p[11] = 0xE0; // jmp rax
    // total appended 12 bytes
#else
    // x86: relative jmp (E9 rel32)
    uint32_t src = (uint32_t)((BYTE*)target + overwriteSize);
    uint32_t dst = (uint32_t)(p + 5); // just placeholder; we'll overwrite with proper rel
    // compute rel = src - (paddr + 5) ??? Actually we want jump from p to src, so:
    uint32_t rel = src - (uint32_t)((uintptr_t)p + 5);
    p[0] = 0xE9;
    memcpy(p + 1, &rel, 4);
#endif

    FlushInsCache(tramp, trampSize);
    return tramp;
}

// Install inline hook
static bool InstallInlineHook(InlineHook* hook, void* target, void* detour) {
    if (!hook || !target || !detour) return false;
    hook->target = target;
    hook->overwriteSize = OVERWRITE_SIZE;
    if (hook->overwriteSize > sizeof(hook->originalBytes)) return false;

    // save original bytes
    memcpy(hook->originalBytes, target, hook->overwriteSize);

    // create trampoline
    hook->trampoline = CreateTrampoline(target, hook->overwriteSize);
    if (!hook->trampoline) return false;

    // craft jump to detour at target
#ifdef _M_X64
    // mov rax, imm64 ; jmp rax
    BYTE patch[OVERWRITE_SIZE] = { 0 };
    patch[0] = 0x48; patch[1] = 0xB8; // mov rax, imm64
    uint64_t det = (uint64_t)detour;
    memcpy(patch + 2, &det, sizeof(det));
    patch[10] = 0xFF; patch[11] = 0xE0; // jmp rax
    // if OVERWRITE_SIZE > 12, pad with NOPs
    for (SIZE_T i = 12; i < hook->overwriteSize; ++i) patch[i] = 0x90;
    if (!WriteMemory(target, patch, hook->overwriteSize)) return false;
#else
    // x86: E9 rel32
    BYTE patch[OVERWRITE_SIZE];
    uint32_t rel = (uint32_t)((uintptr_t)detour - ((uintptr_t)target + 5));
    patch[0] = 0xE9;
    memcpy(patch + 1, &rel, 4);
    // if overwriteSize > 5, pad with NOPs
    for (SIZE_T i = 5; i < hook->overwriteSize; ++i) patch[i] = 0x90;
    if (!WriteMemory(target, patch, hook->overwriteSize)) return false;
#endif

    return true;
}

// Uninstall inline hook (restore original bytes, free trampoline)
static bool UninstallInlineHook(InlineHook* hook) {
    if (!hook || !hook->target) return false;

    if (!WriteMemory(hook->target, hook->originalBytes, hook->overwriteSize)) return false;

    if (hook->trampoline) {
        VirtualFree(hook->trampoline, 0, MEM_RELEASE);
        hook->trampoline = nullptr;
    }
    return true;
}

//////////////////////////////////////////
// Example: hooking RunFileDlg ordinal #61
//////////////////////////////////////////

// typedef for the function signature (match your target; here we use generic)
#ifdef _M_X64
typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);
#else
typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);
#endif

static InlineHook g_runfile_hook = { 0 };
static RunFileDlg_t g_original_runfile = nullptr;

// Example pipe-send helper (simple, blocking) - adjust encoding/length as needed
static void SendToHostSimple(const wchar_t* message) {
    HANDLE h = CreateFileW(L"\\\\.\\pipe\\REBOUND_SERVICE_HOST", GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
    if (h == INVALID_HANDLE_VALUE) return;
    DWORD written = 0;
    QueueMessage(message);
    CloseHandle(h);
}

// Hook function
int WINAPI RunFileDlg_Hook(HWND hwnd, HICON icon, LPCWSTR path, LPCWSTR title, LPCWSTR prompt, UINT flags) {
    QueueMessage(L"Shell::SpawnRunWindow"); // async, safe
    if (g_runfile_hook.trampoline) {
        auto trampoline = (RunFileDlg_t)g_runfile_hook.trampoline;
        return trampoline(hwnd, icon, path, title, prompt, flags);
    }
    return 0; // block if trampoline failed
}

// Install function to call from your injector thread
bool InstallRunFileDlgHook() {
    // Load shell32 and get ordinal #61
    HMODULE shell32 = LoadLibraryW(L"shell32.dll");
    if (!shell32) return false;

    // Use MAKEINTRESOURCE-style ordinal
    FARPROC fp = GetProcAddress(shell32, MAKEINTRESOURCEA(61));
    if (!fp) return false;

    void* target = (void*)fp;

    // If you want to keep a direct original pointer as fallback, set it (not used if trampoline exists)
    g_original_runfile = (RunFileDlg_t)fp;

    // Install inline hook
    if (!InstallInlineHook(&g_runfile_hook, target, (void*)&RunFileDlg_Hook)) {
        return false;
    }

    return true;
}

bool RemoveRunFileDlgHook() {
    return UninstallInlineHook(&g_runfile_hook);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID) {
    if (fdwReason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hinstDLL);
        uintptr_t t = _beginthreadex(nullptr, 0, InjectorInitThread, nullptr, 0, nullptr);
        if (t) CloseHandle((HANDLE)t);
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        RemoveRunFileDlgHook();
    }
    return TRUE;
}

unsigned __stdcall InjectorInitThread(void*) {
    if (!InstallRunFileDlgHook())
        OutputDebugStringW(L"[Injector] Hook install failed");
    return 0;
}
