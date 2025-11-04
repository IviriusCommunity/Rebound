using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rebound.Forge;

public enum ModCategory
{
    General,
    Productivity,
    SystemAdministration,
    Customization,
    Extras,
    Sideloaded
}

public partial class Mod : ObservableObject
{
    [ObservableProperty] public partial bool IsInstalled { get; set; } = false;
    [ObservableProperty] public partial bool IsIntact { get; set; } = true;

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial int Progress { get; set; }
    [ObservableProperty] public partial int TaskCount { get; set; }

    public string Name { get; }
    public string Description { get; }
    public string EntryExecutable { get; set; } = string.Empty;
    public string Icon { get; }
    public string InstallationSteps { get; }
    public ModCategory Category { get; }
    public InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;
    public ObservableCollection<ICog> Cogs { get; }
    public ObservableCollection<IModItem>? Settings { get; set; } = new();
    public string ProcessName { get; }

    private readonly object integrityLock = new();

    public Mod(
        string name,
        string description,
        string icon,
        string installationSteps,
        ObservableCollection<ICog> cogs,
        string processName,
        ModCategory category,
        ObservableCollection<IModItem>? settings = null)
    {
        Name = name;
        Description = description;
        Icon = icon;
        InstallationSteps = installationSteps;
        Cogs = cogs;
        ProcessName = processName;
        Settings = settings;
        Category = category;
    }

    [RelayCommand]
    public async Task InstallAsync()
    {
        try
        {
            IsLoading = true;
            ReboundLogger.Log($"[Mod] Installing {Name}...");

            TaskCount = Cogs.Count(c => !c.Ignorable);
            Progress = 0;

            // Apply all non-ignorable cogs
            foreach (var cog in Cogs.Where(c => !c.Ignorable))
            {
                await cog.ApplyAsync();
                Progress++;
            }

            await UpdateIntegrityAsync();

            ReboundLogger.Log($"[Mod] Installation finished for {Name}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] InstallAsync failed for {Name}", ex);
        }
        finally
        {
            IsLoading = false;
            Progress = 0;
            TaskCount = 0;
        }
    }

    [RelayCommand]
    public async Task UninstallAsync()
    {
        try
        {
            IsLoading = true;
            ReboundLogger.Log($"[Mod] Uninstalling {Name}...");

            TaskCount = Cogs.Count(c => !c.Ignorable);
            Progress = 0;

            // Remove all non-ignorable cogs
            foreach (var cog in Cogs.Where(c => !c.Ignorable))
            {
                await cog.RemoveAsync();
                Progress++;
            }

            await UpdateIntegrityAsync();

            ReboundLogger.Log($"[Mod] Uninstallation finished for {Name}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] UninstallAsync failed for {Name}", ex);
        }
        finally
        {
            IsLoading = false;
            Progress = 0;
            TaskCount = 0;
        }
    }

    [RelayCommand]
    public async Task OpenAsync()
    {
        
    }

    [RelayCommand]
    public async Task RepairAsync() => await InstallAsync();

    [RelayCommand]
    private async Task UpdateIntegrityAsync()
    {
        int intactItems = 0;
        int totalItems = 0;

        lock (integrityLock)
        {
            if (Cogs != null)
            {
                var nonIgnorableCogs = Cogs.Where(c => !c.Ignorable).ToList();
                totalItems = nonIgnorableCogs.Count;

                // We must await outside the lock, so capture the list first
                var tasks = nonIgnorableCogs.Select(c => c.IsAppliedAsync());
                // Move out of lock before awaiting
                Task.Run(async () =>
                {
                    var results = await Task.WhenAll(tasks);
                    intactItems = results.Count(applied => applied);

                    // Update observable properties thread-safely on UI thread
                    IsInstalled = intactItems != 0;
                    IsIntact = intactItems == 0 || intactItems == totalItems;

                    ReboundLogger.Log($"[Mod] Updated integrity for {Name}: Installed={IsInstalled}, Intact={IsIntact}, intactItems={intactItems}, totalItems={totalItems}");
                }).Wait();
            }
        }
    }

    public async Task<ModIntegrity> GetIntegrityAsync()
    {
        if (Cogs == null)
            return ModIntegrity.NotInstalled;

        var nonIgnorableCogs = Cogs.Where(c => !c.Ignorable).ToList();
        int totalItems = nonIgnorableCogs.Count;

        var results = await Task.WhenAll(nonIgnorableCogs.Select(c => c.IsAppliedAsync()));
        int intactItems = results.Count(applied => applied);

        return intactItems == totalItems
            ? ModIntegrity.Installed
            : intactItems == 0
                ? ModIntegrity.NotInstalled
                : ModIntegrity.Corrupt;
    }
}
