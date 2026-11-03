using CLICommonLibrary;
using ClosedXML.Excel;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class DateCellFormatTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void MidnightDateCell_WithTimeFormat_KeepsZeroTime()
    {
        var table = Extract("yyyy-MM-dd HH:mm:ss", new DateTime(2026, 3, 1));

        Assert.Equal("2026-03-01 00:00:00", table.Rows[0].Data[1].Value);
    }

    [Fact]
    public void DateCell_WithCustomFormat_UsesColumnFormat()
    {
        var table = Extract("yyyy/MM/dd", new DateTime(2026, 3, 1));

        Assert.Equal("2026/03/01", table.Rows[0].Data[1].Value);
    }

    [Fact]
    public void DateCellWithTime_WithDateOnlyFormat_Fails()
    {
        var exception = Assert.Throws<ExtractionException>(() => Extract("yyyy-MM-dd", new DateTime(2026, 3, 1, 10, 30, 0)));

        Assert.Contains("2026-03-01 10:30:00", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TextCell_IsExtractedAsWritten()
    {
        var table = Extract("yyyy-MM-dd HH:mm:ss", "2026-03-01 00:00:00");

        Assert.Equal("2026-03-01 00:00:00", table.Rows[0].Data[1].Value);
    }

    [Fact]
    public void TimeCell_WithTimeFormat_UsesColumnFormat()
    {
        var table = Extract("HH:mm", ExcelTimeSerial(13, 45, 0), "TimeOnly", "HH:mm");

        Assert.Equal("13:45", table.Rows[0].Data[1].Value);
    }

    [Fact]
    public void TimeCellWithSeconds_WithMinuteFormat_Fails()
    {
        var exception = Assert.Throws<ExtractionException>(() => Extract("HH:mm", ExcelTimeSerial(13, 45, 30), "TimeOnly", "HH:mm"));

        Assert.Contains("1899-12-31 13:45:30", exception.Message, StringComparison.Ordinal);
    }

    private static double ExcelTimeSerial(int hour, int minute, int second)
    {
        return new TimeSpan(hour, minute, second).TotalDays;
    }

    private BodyColumnAggregator.ExtractedTable Extract(
        string format,
        XLCellValue cellValue,
        string type = "DateTime",
        string? cellNumberFormat = null)
    {
        // language=C#
        var recordSource = $$"""
            using Sdp.Attributes;

            namespace DateCell;

            [StaticDataRecord("Dates", "Items")]
            public sealed record ItemRecord([Key] int Id, [DateTimeFormat("{{format}}")] {{type}} SaleStart);
            """;

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<DateCellFormatTests>();

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("ItemRecord.cs", recordSource);

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Items");
            sheet.Cell("A1").Value = "Id";
            sheet.Cell("B1").Value = "SaleStart";
            sheet.Cell("A2").Value = 1;
            sheet.Cell("B2").Value = cellValue;
            if (cellNumberFormat is not null)
            {
                sheet.Cell("B2").Style.NumberFormat.Format = cellNumberFormat;
            }

            workbook.SaveAs(Path.Combine(excelDirectory.Path, "Dates.xlsx"));
        }

        var catalogs = RecordScanner.Scan(recordDirectory.Path, logger);
        var sheetNameMap = SheetNameScanner.Scan(excelDirectory.Path, logger);
        var requiredHeaderMap = RequiredHeadersChecker.Check(catalogs.RecordSchemaCatalog, sheetNameMap, "A1", logger);
        var extractedTableMap = BodyColumnAggregator.Aggregate(
            catalogs.RecordSchemaCatalog.StaticDataRecordSchemata,
            sheetNameMap,
            requiredHeaderMap,
            logger);
        DataBodyChecker.Check(catalogs, extractedTableMap, factory.CreateLogger<ExcelColumnExtractor.Program>());

        return Assert.Single(extractedTableMap.SortedTables).Value;
    }
}
