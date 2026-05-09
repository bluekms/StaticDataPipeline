using System.Globalization;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.FolderUpdateCheckerTests;

public class FolderUpdateCheckerTest : IDisposable
{
    private readonly ITestOutputHelper testOutputHelper;
    private readonly CultureInfo savedCulture;

    public FolderUpdateCheckerTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
        savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = savedCulture;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Test()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("DummyPath", dummy);
        var after = new FolderState("DummyPath", dummy);

        FolderUpdateChecker.Check(before, after, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ThrowDifferencePath()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("PathA", dummy);
        var after = new FolderState("PathB", dummy);

        Assert.Throws<ArgumentException>(() => FolderUpdateChecker.Check(before, after, logger));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void AddedCheckTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime> { { "dummy", now } };
        var added = new Dictionary<string, DateTime>
        {
            { "dummy", now },
            { "Foo", now }
        };

        var before = new FolderState("DummyPath", dummy);
        var after = new FolderState("DummyPath", added);

        FolderUpdateChecker.Check(before, after, logger);

        Assert.Single(logger.Logs);
        Assert.Equal("File added: Foo", logger.Logs.First().Message);
    }

    [Fact]
    public void RemoveCheckTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime>
        {
            { "dummy", now },
            { "Foo", now }
        };
        var removed = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("DummyPath", dummy);
        var after = new FolderState("DummyPath", removed);

        FolderUpdateChecker.Check(before, after, logger);

        Assert.Single(logger.Logs);
        Assert.Equal("File removed: Foo", logger.Logs.First().Message);
    }

    [Fact]
    public void UpdateCheckTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var now = DateTime.UtcNow;
        var beforeState = new Dictionary<string, DateTime> { { "dummy", now.AddSeconds(-1) } };
        var afterState = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("DummyPath", beforeState);
        var after = new FolderState("DummyPath", afterState);

        FolderUpdateChecker.Check(before, after, logger);

        Assert.Single(logger.Logs);
        Assert.Equal("File changed since last scan: dummy", logger.Logs.First().Message);
    }

    [Fact]
    public void ThrowParadoxTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var now = DateTime.UtcNow;
        var beforeState = new Dictionary<string, DateTime> { { "dummy", now } };
        var afterState = new Dictionary<string, DateTime> { { "dummy", now.AddSeconds(-1) } };

        var before = new FolderState("DummyPath", beforeState);
        var after = new FolderState("DummyPath", afterState);

        Assert.Throws<InvalidOperationException>(() => FolderUpdateChecker.Check(before, after, logger));
        Assert.Empty(logger.Logs);
    }
}
