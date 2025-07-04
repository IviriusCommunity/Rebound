using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.ControlPanel.Views;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;
using Windows.System;

namespace Rebound.ControlPanel;

[ReboundApp("Rebound.Control", "Legacy Control Panel*legacy*ms-appx:///Assets/ControlPanelLegacy.ico")]
public partial class App : Application
{
    internal struct CplEntry()
    {
        public object? Type { get; set; } = null;
        public List<string> Args { get; set; } = [];
    }

    public static bool ArgsMatchKnownEntries(IEnumerable<string> matches, string args)
    {
        var controlPanelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "control.exe");
        List<string> items = [];
        foreach (var match in matches)
        {
            items.Add(match);
            items.Add($"{controlPanelPath} {match}");
        }
        return items.Contains(args, StringComparer.InvariantCultureIgnoreCase);
    }

    private async void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        Debug.WriteLine(e.Arguments);

        var args = e.Arguments?.Trim() ?? string.Empty;
        var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var controlPanelPath = Path.Combine(systemFolder, "control.exe");
        var intlCplPath = Path.Combine(systemFolder, "intl.cpl");
        var appWizCplPath = Path.Combine(systemFolder, "appwiz.cpl");

        var knownArgMappings = new List<CplEntry>()
        {
            // Rebound Settings
            new()
            {
                Type = typeof(ReboundSettingsPage),
                Args = [ "reboundsettings", "/name Rebound.Settings" ]
            },

            // Home page
            new()
            {
                Type = typeof(HomePage),
                Args = [ controlPanelPath, "control", "" ]
            },

            // Windows Tools
            new()
            {
                Type = typeof(WindowsToolsPage),
                Args = [ "admintools", "/name Microsoft.AdministrativeTools" ]
            },

            // Date and Time
            new()
            {
                Type = typeof(WindowsToolsPage),
                Args = [ intlCplPath, $"{intlCplPath},,/p:date", $"{intlCplPath}," ]
            },

            // Apps and Features
            new()
            {
                Type = "ms-settings:appsfeatures",
                Args = [ appWizCplPath, $"{appWizCplPath}," ]
            }
        };

        object? pageToLaunch = null;
        
        foreach (var mapping in knownArgMappings)
        {
            if (ArgsMatchKnownEntries(mapping.Args, args))
            {
                pageToLaunch = mapping.Type;
                break;
            }
        }

        // Launch the legacy cpl if no known arguments match
        if (pageToLaunch == null || e.Arguments == "legacy")
        {
            if (!this.IsRunningAsAdmin())
            {
                var finalArgs = StripControlExePrefix(args.Trim() ?? string.Empty);
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = e.Arguments == "legacy" ? "legacy" : finalArgs
                });
                Process.GetCurrentProcess().Kill();
                return;
            }
            await IFEOEngine.PauseIFEOEntryAsync("control.exe").ConfigureAwait(true);
            var cleanedArgs = StripControlExePrefix(args);
            Process.Start(new ProcessStartInfo
            {
                FileName = "control.exe",
                UseShellExecute = true,
                Arguments = e.Arguments == "legacy" ? string.Empty : cleanedArgs
            });
            await IFEOEngine.ResumeIFEOEntryAsync("control.exe").ConfigureAwait(true);
            Process.GetCurrentProcess().Kill();
            return;
        }
        else
        {
            if (e.IsFirstLaunch)
            {
                if (string.IsNullOrEmpty(args) || pageToLaunch is Type)
                {
                    MainAppWindow ??= new MainWindow();
                    MainAppWindow.Activate();

                    if (pageToLaunch is Type)
                        LaunchPageOrUri(pageToLaunch, args);
                }
                else if (pageToLaunch is string uri)
                {
                    await Launcher.LaunchUriAsync(new Uri(uri));
                }
            }
            else
            {
                MainAppWindow?.BringToFront();

                if (string.IsNullOrEmpty(args) || pageToLaunch is Type or string)
                    LaunchPageOrUri(pageToLaunch, args);
            }

            void LaunchPageOrUri(object? target, string arguments)
            {
                MainAppWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    if (target is Type pageType)
                    {
                        var frame = (MainAppWindow as MainWindow)?.RootFrame.Content as RootPage;
                        if (frame?.RootFrame?.Content?.GetType() != pageType)
                        {
                            _ = frame?.RootFrame?.Navigate(pageType);
                        }
                    }
                    else if (target is string uri)
                    {
                        await Launcher.LaunchUriAsync(new Uri(uri));
                    }
                });
            }
        }
    }

    private static string StripControlExePrefix(string args)
    {
        var trimmed = args.Trim();
        while (true)
        {
            if (string.IsNullOrWhiteSpace(trimmed))
                return string.Empty;

            // Get the first token
            var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return trimmed;

            var firstToken = parts[0].Trim('"');

            // Match against any case of "control.exe" with or without full path
            if (firstToken.EndsWith("control.exe", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = parts.Length > 1 ? parts[1] : string.Empty;
                continue;
            }

            break;
        }

        return trimmed;
    }
}