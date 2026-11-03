using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class BclEnumExtractionTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string ClubSource = """
        using System;
        using Sdp.Attributes;

        namespace BclEnum;

        [StaticDataRecord("Club", "Clubs")]
        public sealed record ClubRecord([Key] int Id, string Name, DayOfWeek MeetingDay);
        """;

    [Fact]
    public void BclEnumMemberName_IsExtracted()
    {
        var table = Extract(["Id", "Name", "MeetingDay"], [["1", "Chess", "Monday"], ["2", "Choir", "Friday"]]);

        Assert.Equal(["Id", "Name", "MeetingDay"], table.Headers);
        Assert.Equal(["Monday", "Friday"], table.Rows.Select(row => row.Data[2].Value));
    }

    [Fact]
    public void UndefinedBclEnumMember_FailsExtraction()
    {
        var ex = Record.Exception(() => Extract(["Id", "Name", "MeetingDay"], [["1", "Chess", "Funday"]]));

        Assert.IsType<ExtractionException>(ex);
    }

    private BodyColumnAggregator.ExtractedTable Extract(string[] sheetHeaders, string[][] sheetRows)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<BclEnumExtractionTests>() is not TestOutputLogger<BclEnumExtractionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        if (factory.CreateLogger<ExcelColumnExtractor.Program>() is not TestOutputLogger<ExcelColumnExtractor.Program> programLogger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("ClubRecord.cs", ClubSource);

        var grid = new[] { sheetHeaders }.Concat(sheetRows).ToList();
        MinimalXlsxWriter.Write(Path.Combine(excelDirectory.Path, "Club.xlsx"), "Clubs", grid);

        var catalogs = RecordScanner.Scan(recordDirectory.Path, logger);
        var sheetNameMap = SheetNameScanner.Scan(excelDirectory.Path, logger);
        var requiredHeaderMap = RequiredHeadersChecker.Check(catalogs.RecordSchemaCatalog, sheetNameMap, "A1", logger);
        var extractedTableMap = BodyColumnAggregator.Aggregate(
            catalogs.RecordSchemaCatalog.StaticDataRecordSchemata,
            sheetNameMap,
            requiredHeaderMap,
            logger);
        DataBodyChecker.Check(catalogs, extractedTableMap, programLogger);
        Assert.Empty(logger.Logs);
        Assert.Empty(programLogger.Logs);

        return Assert.Single(extractedTableMap.SortedTables).Value;
    }
}
