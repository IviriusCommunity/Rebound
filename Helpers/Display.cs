using Microsoft.Graphics.Display;
using WinUIEx;

namespace Rebound.Helpers;

public static class Display
{
    public static double Scale(WindowEx windowEx)
    {
        // Get the DisplayInformation object for the current view
        var displayInformation = DisplayInformation.CreateForWindowId(windowEx.AppWindow.Id);
        // Get the RawPixelsPerViewPixel which gives the scale factor
        var scaleFactor = displayInformation.RawPixelsPerViewPixel;
        return scaleFactor;
    }
}
