using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest;

public class FolderUpdateCheckerTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Test()
    {
        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("DummyPath", dummy);
        var after = new FolderState("DummyPath", dummy);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        FolderUpdateChecker.Check(before, after, logger);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ThrowDifferencePath()
    {
        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("PathA", dummy);
        var after = new FolderState("PathB", dummy);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        Assert.Throws<ArgumentException>(() => FolderUpdateChecker.Check(before, after, logger));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void AddedCheckTest()
    {
        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime> { { "dummy", now } };
        var added = new Dictionary<string, DateTime>
        {
            { "dummy", now },
            { "Foo", now }
        };

        var before = new FolderState("DummyPath", dummy);
        var after = new FolderState("DummyPath", added);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        FolderUpdateChecker.Check(before, after, logger);

        Assert.Single(logger.Logs);
        Assert.Equal("File added: Foo", logger.Logs.First().Message);
    }

    [Fact]
    public void RemoveCheckTest()
    {
        var now = DateTime.UtcNow;
        var dummy = new Dictionary<string, DateTime>
        {
            { "dummy", now },
            { "Foo", now }
        };

        var removed = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("DummyPath", dummy);
        var after = new FolderState("DummyPath", removed);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        FolderUpdateChecker.Check(before, after, logger);

        Assert.Single(logger.Logs);
        Assert.Equal("File removed: Foo", logger.Logs.First().Message);
    }

    [Fact]
    public void UpdateCheckTest()
    {
        var now = DateTime.UtcNow;
        var beforeState = new Dictionary<string, DateTime> { { "dummy", now.AddSeconds(-1) } };
        var afterState = new Dictionary<string, DateTime> { { "dummy", now } };

        var before = new FolderState("DummyPath", beforeState);
        var after = new FolderState("DummyPath", afterState);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        FolderUpdateChecker.Check(before, after, logger);

        Assert.Single(logger.Logs);
        Assert.Equal("File changed since last scan: dummy", logger.Logs.First().Message);
    }

    [Fact]
    public void ThrowParadoxTest()
    {
        var now = DateTime.UtcNow;
        var beforeState = new Dictionary<string, DateTime> { { "dummy", now } };
        var afterState = new Dictionary<string, DateTime> { { "dummy", now.AddSeconds(-1) } };

        var before = new FolderState("DummyPath", beforeState);
        var after = new FolderState("DummyPath", afterState);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<FolderUpdateCheckerTest>() is not TestOutputLogger<FolderUpdateCheckerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        Assert.Throws<InvalidOperationException>(() => FolderUpdateChecker.Check(before, after, logger));
        Assert.Empty(logger.Logs);
    }
}
