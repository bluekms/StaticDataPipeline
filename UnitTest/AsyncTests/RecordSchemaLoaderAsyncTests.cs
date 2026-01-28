using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AsyncTests;

public class RecordSchemaLoaderAsyncTests(ITestOutputHelper testOutputHelper)
{
    private static string GetTestRecordPath()
    {
        return Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "..",
            "..",
            "..",
            "..",
            "Docs",
            "SampleRecord");
    }

    [Fact]
    public async Task LoadAsync_WithValidDirectory_ReturnsResults()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaLoaderAsyncTests>() is not TestOutputLogger<RecordSchemaLoaderAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var csPath = GetTestRecordPath();
        var results = await RecordSchemaLoader.LoadAsync(csPath, logger);

        Assert.NotEmpty(results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task LoadAsync_WithValidFile_ReturnsResults()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaLoaderAsyncTests>() is not TestOutputLogger<RecordSchemaLoaderAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var csPath = GetTestRecordPath();
        var files = Directory.GetFiles(csPath, "*.cs");
        Assert.NotEmpty(files);
        var singleFile = files[0];

        var results = await RecordSchemaLoader.LoadAsync(singleFile, logger);

        Assert.Single(results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task LoadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaLoaderAsyncTests>() is not TestOutputLogger<RecordSchemaLoaderAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var csPath = GetTestRecordPath();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RecordSchemaLoader.LoadAsync(csPath, logger, cts.Token));

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidPath_ThrowsArgumentException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaLoaderAsyncTests>() is not TestOutputLogger<RecordSchemaLoaderAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var invalidPath = Path.Combine(GetTestRecordPath(), "NonExistentPath");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            RecordSchemaLoader.LoadAsync(invalidPath, logger));

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task LoadAsync_ResultsMatchSyncVersion()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaLoaderAsyncTests>() is not TestOutputLogger<RecordSchemaLoaderAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var csPath = GetTestRecordPath();

        var syncResults = RecordSchemaLoader.Load(csPath, logger);
        var asyncResults = await RecordSchemaLoader.LoadAsync(csPath, logger);

        Assert.Equal(syncResults.Count, asyncResults.Count);
        for (var i = 0; i < syncResults.Count; i++)
        {
            Assert.Equal(
                syncResults[i].RecordDeclarationList.Count,
                asyncResults[i].RecordDeclarationList.Count);
            Assert.Equal(
                syncResults[i].EnumDeclarationList.Count,
                asyncResults[i].EnumDeclarationList.Count);
        }

        Assert.Empty(logger.Logs);
    }
}
