using System.Text;

#nullable enable

namespace Rebound.Defrag.Helpers;

public static class GenericHelpers
{
    // Used to obtain the key name for IsChecked in app settings for each drive
    public static string ConvertStringToNumericRepresentation(string? input)
    {
        input ??= "";

        // Create a StringBuilder to store the numeric representation
        StringBuilder numericRepresentation = new();

        // Iterate over each character in the string
        foreach (var c in input)
        {
            // Convert the character to its ASCII value and append it
            _ = numericRepresentation.Append((int)c);
        }

        // Return the numeric representation as a string
        return numericRepresentation.ToString();
    }

    public static string DrivePathToLetter(this string path) => path.Remove(2, 1);

    public static string DrivePathToSingleLetter(this string path) => path.Remove(1, 2);
}