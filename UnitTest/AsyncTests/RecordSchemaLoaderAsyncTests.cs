using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AsyncTests;

public partial class RecordSchemaLoaderAsyncTests(ITestOutputHelper testOutputHelper)
{
    private static readonly string[] RecordResourceFileNames =
    [
        "Excel1Records.cs",
        "Excel2Records.cs",
        "Excel3Records.cs",
    ];

    [Fact]
    public async Task LoadAsync_WithValidDirectory_ReturnsResults()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaLoaderAsyncTests>() is not TestOutputLogger<RecordSchemaLoaderAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var testData = new TestDataDirectory(RecordResourceFileNames);
        var results = await RecordSchemaLoader.LoadAsync(testData.Path, logger);

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

        using var testData = new TestDataDirectory(RecordResourceFileNames);
        var files = Directory.GetFiles(testData.Path, "*.cs");
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

        using var testData = new TestDataDirectory(RecordResourceFileNames);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RecordSchemaLoader.LoadAsync(testData.Path, logger, cts.Token));

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

        using var testData = new TestDataDirectory(RecordResourceFileNames);
        var invalidPath = Path.Combine(testData.Path, "NonExistentPath");

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

        using var testData = new TestDataDirectory(RecordResourceFileNames);

        var syncResults = RecordSchemaLoader.Load(testData.Path, logger);
        var asyncResults = await RecordSchemaLoader.LoadAsync(testData.Path, logger);

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
