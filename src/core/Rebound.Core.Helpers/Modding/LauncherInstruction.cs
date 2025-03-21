using System.IO;

namespace Rebound.Helpers.Modding;

public class LauncherInstruction : IReboundAppInstruction
{
    public required string Path { get; set; }

    public required string TargetPath { get; set; }

    public LauncherInstruction()
    {

    }

    public void Apply()
    {
        try
        {
            if (!Directory.Exists("%PROGRAMFILES%\\Rebound")) Directory.CreateDirectory("%PROGRAMFILES%\\Rebound");
            File.Copy(Path, TargetPath, true);
        }
        catch
        {

        }
    }

    public void Remove()
    {
        try
        {
            if (File.Exists(TargetPath))
            {
                File.Delete(TargetPath);
            }
        }
        catch
        {

        }
    }

    public bool IsApplied()
    {
        try
        {
            return File.Exists(TargetPath);
        }
        catch
        {
            return false;
        }
    }
}