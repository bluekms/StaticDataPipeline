using ClosedXML.Excel;
using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Mappings;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class SheetStateWarningTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string RecordSource = """
        using Sdp.Attributes;

        namespace SheetState;

        public enum Kind { Potion, Sword }

        [StaticDataRecord("State", "Items")]
        public sealed record ItemRecord([Key] int Id, string Name, Kind Kind, long Price);
        """;

    // language=C#
    private const string BrandedRecordSource = """
        using Sdp.Attributes;

        namespace SheetState;

        public sealed record ItemId(int Value);

        [StaticDataRecord("State", "Items")]
        public sealed record ItemRecord([Key] ItemId Id, string Name);
        """;

    [Fact]
    public void HiddenSheet_FailsWithSingleErrorLog()
    {
        var (logger, exception) = ExtractExpectingFailure(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], [["1", "Potion", "Potion", "100"]]);
            sheet.Hide();
        });

        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
        Assert.Single(logger.Logs, log => log.LogLevel == LogLevel.Error && log.Message.Contains("State.Items", StringComparison.Ordinal));
        Assert.Contains("State.Items", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormulaCellInDataArea_FailsWithSingleErrorLog()
    {
        var (logger, exception) = ExtractExpectingFailure(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], [["1", "Potion", "Potion", "100"]]);
            sheet.Cell("D2").FormulaA1 = "=50*2";
        });

        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
        Assert.Single(logger.Logs, log => log.LogLevel == LogLevel.Error && log.Message.Contains("D2", StringComparison.Ordinal));
        Assert.Contains("D2", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormulaCellOutsideDataArea_IsIgnored()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            sheet.Cell("A1").Value = "아이템 표";
            sheet.Cell("B1").FormulaA1 = "=TODAY()";
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], [["1", "Potion", "Potion", "100"]], startRow: 3);
        }, startCell: "A3");

        Assert.Single(table.Rows);
        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public void FormulaCellInNonExtractedColumn_IsIgnored()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price", "Memo"], [["1", "Potion", "Potion", "100", string.Empty]]);
            sheet.Cell("E2").FormulaA1 = "=D2*2";
        });

        Assert.Single(table.Rows);
        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public void SurroundingWhitespace_WarnsWithQuotedValue()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], [["1", "Potion", " Potion ", "100"]]);
        });

        Assert.Equal(" Potion ", table.Rows[0].Data[2].Value);
        var warning = Assert.Single(logger.Logs, log => log.LogLevel == LogLevel.Warning);
        Assert.Contains("C2", warning.Message, StringComparison.Ordinal);
        Assert.Contains("' Potion '", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NumberFormatOnNumericCells_WarnsOncePerColumnAndKeepsStoredValue()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], [["1", "Potion", "Potion", "0"], ["2", "Sword", "Sword", "0"]]);
            sheet.Cell("D2").Value = 9999999999;
            sheet.Cell("D2").Style.NumberFormat.Format = "#,##0";
            sheet.Cell("D3").Value = 5000;
            sheet.Cell("D3").Style.NumberFormat.Format = "#,##0";
        });

        Assert.Equal("9999999999", table.Rows[0].Data[3].Value);
        Assert.Equal("5000", table.Rows[1].Data[3].Value);
        var warning = Assert.Single(logger.Logs, log => log.LogLevel == LogLevel.Warning);
        Assert.Contains("#,##0", warning.Message, StringComparison.Ordinal);
        Assert.Contains("D2", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WhitespaceAndNumberFormatInNonExtractedColumn_AreNotReported()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price", "Memo"], [["1", "Potion", "Potion", "100", " 회복용 "]]);
            sheet.Cell("F1").Value = "Rate";
            sheet.Cell("F2").Value = 0.02;
            sheet.Cell("F2").Style.NumberFormat.Format = "0%";
        });

        Assert.Single(table.Rows);
        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public void DateCell_IsNotReportedAsFormatted()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], [["1", "Potion", "Potion", "100"]]);
            sheet.Cell("B2").Value = new DateTime(2026, 3, 2);
            sheet.Cell("B2").Style.DateFormat.Format = "yyyy-mm-dd";
        });

        Assert.Equal("2026-03-02", table.Rows[0].Data[1].Value);
        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public void HeaderOnlySheet_LogsInformationAndYieldsNoRows()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], []);
        });

        Assert.Empty(table.Rows);
        Assert.Contains(logger.Logs, log => log.LogLevel == LogLevel.Information && log.Message.Contains("State.Items", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public void FormattedBlankRowsOnly_LogsInformationAndYieldsNoRows()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price"], []);
            sheet.Range("A2:D3").Style.Fill.BackgroundColor = XLColor.Yellow;
        });

        Assert.Empty(table.Rows);
        Assert.Contains(logger.Logs, log => log.LogLevel == LogLevel.Information && log.Message.Contains("State.Items", StringComparison.Ordinal));
    }

    [Fact]
    public void ValueSuffixColumnBesidePrimitiveHeader_IsIgnored()
    {
        var (logger, table) = Extract(RecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Name", "Kind", "Price", "Price.Value"], [["1", "Potion", "Potion", "100", "999"]]);
        });

        Assert.Equal("100", table.Rows[0].Data[3].Value);
        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public void HeaderCaseMismatch_FailsWithBothHeaders()
    {
        var (_, exception) = ExtractExpectingFailure(RecordSource, sheet =>
        {
            WriteRows(sheet, ["id", "Name", "Kind", "Price"], [["1", "Potion", "Potion", "100"]]);
        });

        Assert.Contains("'Id'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'id'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BrandedHeaderInBothForms_FailsWithSingleErrorLog()
    {
        var (logger, exception) = ExtractExpectingFailure(BrandedRecordSource, sheet =>
        {
            WriteRows(sheet, ["Id", "Id.Value", "Name"], [["1", "1", "Potion"]]);
        });

        Assert.DoesNotContain(logger.Logs, log => log.LogLevel == LogLevel.Warning);
        Assert.Single(logger.Logs, log => log.LogLevel == LogLevel.Error && log.Message.Contains("Id.Value", StringComparison.Ordinal));
        Assert.Contains("Id.Value", exception.Message, StringComparison.Ordinal);
    }

    private static void WriteRows(IXLWorksheet sheet, string[] headers, string[][] rows, int startRow = 1)
    {
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(startRow, c + 1).Value = headers[c];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                sheet.Cell(startRow + r + 1, c + 1).Value = rows[r][c];
            }
        }
    }

    private sealed record ExtractResult(
        TestOutputLogger<SheetStateWarningTests> Logger,
        BodyColumnAggregator.ExtractedTable Table);

    private sealed record FailureResult(TestOutputLogger<SheetStateWarningTests> Logger, ExtractionException Exception);

    private ExtractResult Extract(
        string recordSource,
        Action<IXLWorksheet> buildSheet,
        string startCell = "A1")
    {
        var logger = CreateLogger();
        var extractedTableMap = Run(recordSource, buildSheet, startCell, logger);

        return new(logger, Assert.Single(extractedTableMap.SortedTables).Value);
    }

    private FailureResult ExtractExpectingFailure(
        string recordSource,
        Action<IXLWorksheet> buildSheet)
    {
        var logger = CreateLogger();
        var exception = Assert.Throws<ExtractionException>(() => Run(recordSource, buildSheet, "A1", logger));
        testOutputHelper.WriteLine(exception.Message);

        return new(logger, exception);
    }

    private static ExtractedTableMap Run(
        string recordSource,
        Action<IXLWorksheet> buildSheet,
        string startCell,
        ILogger logger)
    {
        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("ItemRecord.cs", recordSource);

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Items");
            buildSheet(sheet);
            workbook.SaveAs(Path.Combine(excelDirectory.Path, "State.xlsx"));
        }

        var catalogs = RecordScanner.Scan(recordDirectory.Path, logger);
        var sheetNameMap = SheetNameScanner.Scan(excelDirectory.Path, logger);
        var requiredHeaderMap = RequiredHeadersChecker.Check(catalogs.RecordSchemaCatalog, sheetNameMap, startCell, logger);

        return BodyColumnAggregator.Aggregate(
            catalogs.RecordSchemaCatalog.StaticDataRecordSchemata,
            sheetNameMap,
            requiredHeaderMap,
            logger);
    }

    private TestOutputLogger<SheetStateWarningTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<SheetStateWarningTests>() is not TestOutputLogger<SheetStateWarningTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
