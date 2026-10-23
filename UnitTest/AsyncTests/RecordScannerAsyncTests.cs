using CLICommonLibrary;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AsyncTests;

public partial class RecordScannerAsyncTests(ITestOutputHelper testOutputHelper)
{
    private static readonly string[] RecordResourceFileNames =
    [
        "Excel1Records.cs",
        "Excel2Records.cs",
        "Excel3Records.cs",
    ];

    [Fact]
    public async Task ScanAsync_WithValidPath_ReturnsCatalogs()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordScannerAsyncTests>() is not TestOutputLogger<RecordScannerAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var testData = new TestDataDirectory(RecordResourceFileNames);
        var catalogs = await RecordScanner.ScanAsync(testData.Path, logger);

        Assert.NotNull(catalogs);
        Assert.NotNull(catalogs.RecordSchemaCatalog);
        Assert.NotNull(catalogs.EnumMemberCatalog);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task ScanAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordScannerAsyncTests>() is not TestOutputLogger<RecordScannerAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var testData = new TestDataDirectory(RecordResourceFileNames);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RecordScanner.ScanAsync(testData.Path, logger, cts.Token));

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task ScanAsync_ResultsMatchSyncVersion()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordScannerAsyncTests>() is not TestOutputLogger<RecordScannerAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var testData = new TestDataDirectory(RecordResourceFileNames);
        var syncCatalogs = RecordScanner.Scan(testData.Path, logger);
        var asyncCatalogs = await RecordScanner.ScanAsync(testData.Path, logger);

        Assert.Equal(
            syncCatalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count,
            asyncCatalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count);

        Assert.Empty(logger.Logs);
    }
}
