using System;
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
            var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            var directoryPath = System.IO.Path.Combine(programFilesPath ?? "", "Rebound");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Set attributes for the directory
            var currentAttributes = File.GetAttributes(directoryPath);

            // Ensure directory attributes are set (optional but ensures directory is recognized)
            if (!currentAttributes.HasFlag(FileAttributes.Directory))
            {
                File.SetAttributes(directoryPath, FileAttributes.Directory);
            }

            File.SetAttributes(directoryPath, currentAttributes | FileAttributes.System | FileAttributes.Hidden);

            // Copy the file to the directory
            File.Copy(Path, TargetPath, true);
        }
        catch (Exception ex)
        {
            // Log the error or handle it
            Console.WriteLine($"Error: {ex.Message}");
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