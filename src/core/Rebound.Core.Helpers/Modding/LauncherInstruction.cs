using System;
using System.IO;
using Windows.Storage;

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
        if (!Directory.Exists("%PROGRAMFILES%\\Rebound")) Directory.CreateDirectory("%PROGRAMFILES%\\Rebound");

        File.Copy(Path, TargetPath, true);
    }

    public void Remove()
    {
        if (File.Exists(TargetPath))
        {
            File.Delete(TargetPath);
        }
    }

    public bool IsApplied()
    {
        return File.Exists(TargetPath);
    }
}