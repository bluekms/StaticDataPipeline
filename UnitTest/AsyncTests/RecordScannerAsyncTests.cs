using CLICommonLibrary;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AsyncTests;

public class RecordScannerAsyncTests(ITestOutputHelper testOutputHelper)
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
    public async Task ScanAsync_WithValidPath_ReturnsCatalogs()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordScannerAsyncTests>() is not TestOutputLogger<RecordScannerAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var csPath = GetTestRecordPath();
        var catalogs = await RecordScanner.ScanAsync(csPath, logger);

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

        var csPath = GetTestRecordPath();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RecordScanner.ScanAsync(csPath, logger, cts.Token));

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

        var csPath = GetTestRecordPath();
        var syncCatalogs = RecordScanner.Scan(csPath, logger);
        var asyncCatalogs = await RecordScanner.ScanAsync(csPath, logger);

        Assert.Equal(
            syncCatalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count,
            asyncCatalogs.RecordSchemaCatalog.StaticDataRecordSchemata.Count);

        Assert.Empty(logger.Logs);
    }
}
