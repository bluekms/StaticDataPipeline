using CLICommonLibrary.Resources;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace CLICommonLibrary;

public static class Logger
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}";

    public static ILogger<T> CreateLogger<T>(LogEventLevel minLogLevel, string logPath)
    {
        if (string.IsNullOrEmpty(logPath))
        {
            throw new InvalidOperationException(Messages.LogPathRequired);
        }

        var formatter = new MessageTemplateTextFormatter(OutputTemplate, null);
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(minLogLevel)
            .WriteTo.Console(formatter);

        var logFile = Path.Combine(logPath, "log.txt");
        Log.Logger = loggerConfiguration.WriteTo
            .File(formatter, logFile, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var loggerFactory = new LoggerFactory().AddSerilog();
        return loggerFactory.CreateLogger<T>();
    }

    public static ILogger<T> CreateLoggerWithoutFile<T>(LogEventLevel minLogLevel)
    {
        var formatter = new MessageTemplateTextFormatter(OutputTemplate, null);
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(minLogLevel)
            .WriteTo.Console(formatter);

        Log.Logger = loggerConfiguration.CreateLogger();

        var loggerFactory = new LoggerFactory().AddSerilog();
        return loggerFactory.CreateLogger<T>();
    }
}
