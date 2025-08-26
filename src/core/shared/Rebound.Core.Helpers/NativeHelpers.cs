using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Win32.Foundation;

namespace Rebound.Core.Helpers;

public static class NativeHelpers
{
    public static bool ArgsMatchKnownEntries(this string appName, IEnumerable<string> matches, string args)
    {
        List<string> items = [];
        foreach (var match in matches)
        {
            items.Add(match);
            items.Add($"{appName} {match}");
        }
        return items.Contains(args, StringComparer.InvariantCultureIgnoreCase);
    }

    public static unsafe HWND ToCsWin32HWND(this TerraFX.Interop.Windows.HWND hwnd)
    {
        return *(HWND*)hwnd.Value;
    }

    public static unsafe TerraFX.Interop.Windows.HWND ToTerraFXHWND(this HWND hwnd)
    {
        return *(TerraFX.Interop.Windows.HWND*)hwnd.Value;
    }

    public static unsafe PCWSTR ToPCWSTR(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return new PCWSTR(valueCharPtr);
        }
    }

    public static unsafe PWSTR ToPWSTR(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return new PWSTR(valueCharPtr);
        }
    }
}