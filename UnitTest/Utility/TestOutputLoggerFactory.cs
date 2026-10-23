using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace UnitTest.Utility;

public partial class TestOutputLoggerFactory(
    ITestOutputHelper output,
    LogLevel minLogLevel)
    : ILoggerFactory
{
    private bool disposed;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
            }

            disposed = true;
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger<object>(output, minLogLevel);
    }

    public ILogger<T> CreateLogger<T>()
    {
        return new TestOutputLogger<T>(output, minLogLevel);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }
}
