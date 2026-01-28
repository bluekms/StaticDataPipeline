using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AsyncTests;

[Collection("ExcelFileTests")]
public class ExcelSheetProcessorAsyncTests(ITestOutputHelper testOutputHelper)
{
    private static string GetTestExcelPath()
    {
        return Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "..",
            "..",
            "..",
            "..",
            "Docs",
            "SampleExcel",
            "Excel1.xlsx");
    }

    [Fact]
    public async Task ProcessAsync_WithValidSheet_ProcessesHeader()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<ExcelSheetProcessorAsyncTests>() is not TestOutputLogger<ExcelSheetProcessorAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var excelPath = GetTestExcelPath();
        Assert.True(File.Exists(excelPath), $"Test file not found: {excelPath}");

        SheetHeader? capturedHeader = null;
        var processor = new ExcelSheetProcessor(header => capturedHeader = header);

        string sheetName;
        using (var opener = new LockedFileStreamOpener(excelPath))
        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(opener.Stream))
        {
            sheetName = reader.Name;
        }

        var excelSheetName = new ExcelSheetName(excelPath, sheetName);
        await processor.ProcessAsync(excelSheetName, logger);

        Assert.NotNull(capturedHeader);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<ExcelSheetProcessorAsyncTests>() is not TestOutputLogger<ExcelSheetProcessorAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var excelPath = GetTestExcelPath();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var processor = new ExcelSheetProcessor((ExcelSheetProcessor.ProcessHeader)(_ => { }));

        string sheetName;
        using (var opener = new LockedFileStreamOpener(excelPath))
        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(opener.Stream))
        {
            sheetName = reader.Name;
        }

        var excelSheetName = new ExcelSheetName(excelPath, sheetName);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            processor.ProcessAsync(excelSheetName, logger, cts.Token));

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task ProcessAsync_ResultsMatchSyncVersion()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
        if (factory.CreateLogger<ExcelSheetProcessorAsyncTests>() is not TestOutputLogger<ExcelSheetProcessorAsyncTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var excelPath = GetTestExcelPath();

        SheetHeader? syncHeader = null;
        SheetHeader? asyncHeader = null;
        var syncRows = new List<SheetBodyRow>();
        var asyncRows = new List<SheetBodyRow>();

        var syncProcessor = new ExcelSheetProcessor(
            header => syncHeader = header,
            row => syncRows.Add(row));

        var asyncProcessor = new ExcelSheetProcessor(
            header => asyncHeader = header,
            row => asyncRows.Add(row));

        string sheetName;
        using (var opener = new LockedFileStreamOpener(excelPath))
        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(opener.Stream))
        {
            sheetName = reader.Name;
        }

        var excelSheetName = new ExcelSheetName(excelPath, sheetName);

        syncProcessor.Process(excelSheetName, logger);
        await asyncProcessor.ProcessAsync(excelSheetName, logger);

        Assert.NotNull(syncHeader);
        Assert.NotNull(asyncHeader);
        Assert.Equal(syncHeader.Cells.Count, asyncHeader.Cells.Count);
        Assert.Equal(syncRows.Count, asyncRows.Count);

        for (var i = 0; i < syncHeader.Cells.Count; i++)
        {
            Assert.Equal(syncHeader.Cells[i], asyncHeader.Cells[i]);
        }

        Assert.Empty(logger.Logs);
    }
}
