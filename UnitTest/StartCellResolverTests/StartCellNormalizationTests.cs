using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StartCellResolverTests;

public class StartCellNormalizationTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("B2")]
    [InlineData("b2")]
    [InlineData(" b2 ")]
    public void StartCell_IsCaseInsensitive(string startCell)
    {
        var logger = CreateLogger();
        using var excelDirectory = new CsvTestDirectory();
        var path = Path.Combine(excelDirectory.Path, "Book.xlsx");
        MinimalXlsxWriter.Write(path, "Items", [["memo"], [string.Empty, "Id", "Name"], [string.Empty, "1", "Potion"]]);

        var headers = SheetHeaderScanner.Scan(new ExcelSheetName(path, "Items"), startCell, logger);

        Assert.Equal(["Id", "Name"], headers);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("A0")]
    [InlineData("1A")]
    [InlineData("")]
    public void InvalidStartCell_IsRejected(string startCell)
    {
        var logger = CreateLogger();
        using var excelDirectory = new CsvTestDirectory();
        var path = Path.Combine(excelDirectory.Path, "Book.xlsx");
        MinimalXlsxWriter.Write(path, "Items", [["Id"], ["1"]]);

        Assert.Throws<ArgumentException>(() => SheetHeaderScanner.Scan(new ExcelSheetName(path, "Items"), startCell, logger));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MissingDirectory_IsNotMaskedAsLockedFile()
    {
        var logger = CreateLogger();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "Book.xlsx");

        Assert.Throws<DirectoryNotFoundException>(() => new LockedFileStreamOpener(path));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MissingScanPath_IsNamedInMessage()
    {
        var logger = CreateLogger();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var ex = Assert.Throws<ArgumentException>(() => SheetNameScanner.Scan(path, logger));

        Assert.Contains(path, ex.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<StartCellNormalizationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StartCellNormalizationTests>() is not TestOutputLogger<StartCellNormalizationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
