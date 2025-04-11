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
            ReboundWorkingEnvironment.EnsureFolderIntegrity();

            // Copy the file to the directory
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