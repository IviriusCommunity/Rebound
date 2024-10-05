using Serilog;

namespace ReboundSysInfo.Common;
public static class LoggerSetup
{
    public static ILogger Logger { get; private set; }

    public static void ConfigureLogger()
    {
        if (!Directory.Exists(Constants.LogDirectoryPath))
        {
            Directory.CreateDirectory(Constants.LogDirectoryPath);
        }

        Logger = new LoggerConfiguration()
            .Enrich.WithProperty("Version", App.Current.AppVersion)
            .WriteTo.File(Constants.LogFilePath, rollingInterval: RollingInterval.Day)
            .WriteTo.Debug()
            .CreateLogger();
    }
}

