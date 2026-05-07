using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace UnitTest.Utility;

public class TestOutputLogger<T>(
    ITestOutputHelper output,
    LogLevel minLogLevel)
    : ILogger<T>
{
    public sealed record LogMessage(LogLevel LogLevel, string Message)
    {
        public override string ToString()
        {
            return $"[{LogLevel}]:\t{Message}";
        }
    }

    public List<LogMessage> Logs { get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (!string.IsNullOrEmpty(message))
        {
            var logMessage = new LogMessage(logLevel, message);
            output.WriteLine(logMessage.ToString());
            Logs.Add(logMessage);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= minLogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }
}
