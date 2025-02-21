using System.Text;

#nullable enable

namespace Rebound.Shell.ExperiencePack;

public static class StringHelper
{
    public static string ConvertStringToNumericString(this string input)
    {
        var numericString = new StringBuilder();
        foreach (var c in input)
        {
            numericString.Append((int)c);
        }
        return numericString.ToString();
    }
}