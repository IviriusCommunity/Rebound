using System.Text;

namespace Rebound.Defrag.Helpers;

internal static class GenericHelpers
{
    public static string ConvertStringToSafeKey(string? input)
    {
        input ??= "";
        StringBuilder numericRepresentation = new();

        foreach (var c in input)
        {
            _ = numericRepresentation.Append(((int)c).ToString("X2")); // Hex is cleaner
        }

        return "_key_" + numericRepresentation;
    }

    public static string DrivePathToLetter(this string path) => path.Remove(2, 1);

    public static string DrivePathToSingleLetter(this string path) => path.Remove(1, 2);
}