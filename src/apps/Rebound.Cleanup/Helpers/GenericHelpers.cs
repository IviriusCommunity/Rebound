namespace Rebound.Cleanup.Helpers;

internal static class GenericHelpers
{
    public static string DrivePathToLetter(this string path) => path.Remove(2, 1);
}