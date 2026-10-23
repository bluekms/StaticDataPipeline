using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using CLICommonLibrary;
using ExcelColumnExtractor.Aggregator;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.BodyColumnAggregatorTests;

public class ColumnOrderTests(ITestOutputHelper testOutputHelper)
{
    private const string RecordSource = """
        using Sdp.Attributes;

        namespace ColumnOrder;

        [StaticDataRecord("Order", "Items")]
        public sealed record ItemRecord(int Id, string Name, int Price);
        """;

    private const string BrandedIdRecordSource = """
        using Sdp.Attributes;

        namespace ColumnOrder;

        public sealed record ItemId(int Value);

        [StaticDataRecord("Order", "Items")]
        public sealed record ItemRecord(ItemId Id, string Name, int Price);
        """;

    [Fact]
    public void SheetColumnOrderSameAsRecord_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Id", "Name", "Price"],
            sheetRows:
            [
                ["1", "Potion", "100"],
                ["2", "Sword", "5000"],
            ]);

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    [Fact]
    public void SheetColumnOrderDiffersFromRecord_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Price", "Name", "Id"],
            sheetRows:
            [
                ["100", "Potion", "1"],
                ["5000", "Sword", "2"],
            ]);

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    [Fact]
    public void SheetHasExtraColumnBetween_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Name", "Memo", "Id", "Price"],
            sheetRows:
            [
                ["Potion", "회복용", "1", "100"],
                ["Sword", "기본 검", "2", "5000"],
            ]);

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    [Fact]
    public void ValueFallbackHeader_HeadersMatchValues()
    {
        var table = Extract(
            BrandedIdRecordSource,
            sheetHeaders: ["Price", "Id.Value", "Name"],
            sheetRows:
            [
                ["100", "1", "Potion"],
                ["5000", "2", "Sword"],
            ]);

        AssertTable(table, ["Id.Value", "Name", "Price"], RecordOrderRows);
    }

    [Fact]
    public void StartCellNotA1_HeadersMatchValues()
    {
        var table = Extract(
            RecordSource,
            sheetHeaders: ["Price", "Name", "Id"],
            sheetRows:
            [
                ["100", "Potion", "1"],
                ["5000", "Sword", "2"],
            ],
            startCell: "B2");

        AssertTable(table, RecordOrderHeaders, RecordOrderRows);
    }

    private static void AssertTable(
        BodyColumnAggregator.ExtractedTable table,
        string[] expectedHeaders,
        string[][] expectedRows)
    {
        Assert.Equal(expectedHeaders, table.Headers);
        Assert.Equal(expectedRows.Length, table.Rows.Count);
        for (var i = 0; i < expectedRows.Length; i++)
        {
            var values = table.Rows[i].Data.Select(cell => cell.Value);
            Assert.Equal(expectedRows[i], values);
        }
    }

    private BodyColumnAggregator.ExtractedTable Extract(
        string recordSource,
        string[] sheetHeaders,
        string[][] sheetRows,
        string startCell = "A1")
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var logger = factory.CreateLogger<ColumnOrderTests>();

        using var recordDirectory = new CsvTestDirectory();
        using var excelDirectory = new CsvTestDirectory();
        recordDirectory.Write("ItemRecord.cs", recordSource);

        var columnOffset = startCell[0] - 'A';
        var rowOffset = int.Parse(startCell[1..], CultureInfo.InvariantCulture) - 1;
        var padding = Enumerable.Repeat("설명", columnOffset).ToArray();
        var paddedRows = new[] { sheetHeaders }
            .Concat(sheetRows)
            .Select(row => padding.Concat(row).ToArray());
        var grid = Enumerable.Repeat<string[]>(["시트 설명"], rowOffset)
            .Concat(paddedRows)
            .ToList();
        WriteXlsx(Path.Combine(excelDirectory.Path, "Order.xlsx"), "Items", grid);

        var catalogs = RecordScanner.Scan(recordDirectory.Path, logger);
        var sheetNameMap = SheetNameScanner.Scan(excelDirectory.Path, logger);
        var requiredHeaderMap = RequiredHeadersChecker.Check(catalogs.RecordSchemaCatalog, sheetNameMap, startCell, logger);
        var extractedTableMap = BodyColumnAggregator.Aggregate(
            catalogs.RecordSchemaCatalog.StaticDataRecordSchemata,
            sheetNameMap,
            requiredHeaderMap,
            startCell,
            logger);

        var table = Assert.Single(extractedTableMap.SortedTables).Value;
        testOutputHelper.WriteLine(string.Join(",", table.Headers));
        foreach (var row in table.Rows)
        {
            testOutputHelper.WriteLine(string.Join(",", row.Data.Select(cell => cell.Value)));
        }

        return table;
    }

    private const string ContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
        <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
        <Default Extension="xml" ContentType="application/xml"/>
        <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
        <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
        <Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/>
        </Types>
        """;

    private const string RootRelsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string WorkbookRelsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
        <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings" Target="sharedStrings.xml"/>
        </Relationships>
        """;

    private const string XmlDeclaration = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""";
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly string[] RecordOrderHeaders = ["Id", "Name", "Price"];

    private static readonly string[][] RecordOrderRows =
    [
        ["1", "Potion", "100"],
        ["2", "Sword", "5000"],
    ];

    // 문자열 셀만 가진 최소 xlsx 를 만든다.
    private static void WriteXlsx(string filePath, string sheetName, List<string[]> rows)
    {
        var sharedStrings = rows.SelectMany(row => row).Distinct().ToList();

        var sheetData = new StringBuilder();
        for (var r = 0; r < rows.Count; r++)
        {
            sheetData.Append(CultureInfo.InvariantCulture, $"<row r=\"{r + 1}\">");
            for (var c = 0; c < rows[r].Length; c++)
            {
                var address = $"{(char)('A' + c)}{r + 1}";
                var stringIndex = sharedStrings.IndexOf(rows[r][c]);
                sheetData.Append(CultureInfo.InvariantCulture, $"<c r=\"{address}\" t=\"s\"><v>{stringIndex}</v></c>");
            }

            sheetData.Append("</row>");
        }

        var sst = new StringBuilder();
        foreach (var value in sharedStrings)
        {
            sst.Append(CultureInfo.InvariantCulture, $"<si><t>{SecurityElement.Escape(value)}</t></si>");
        }

        var workbookXml = $"{XmlDeclaration}<workbook xmlns=\"{SpreadsheetNamespace}\" xmlns:r=\"{RelationshipNamespace}\"><sheets><sheet name=\"{SecurityElement.Escape(sheetName)}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        var sheetXml = $"{XmlDeclaration}<worksheet xmlns=\"{SpreadsheetNamespace}\"><sheetData>{sheetData}</sheetData></worksheet>";
        var sharedStringsXml = FormattableString.Invariant(
            $"{XmlDeclaration}<sst xmlns=\"{SpreadsheetNamespace}\" count=\"{sharedStrings.Count}\" uniqueCount=\"{sharedStrings.Count}\">{sst}</sst>");

        using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);
        WriteEntry(zip, "[Content_Types].xml", ContentTypesXml);
        WriteEntry(zip, "_rels/.rels", RootRelsXml);
        WriteEntry(zip, "xl/workbook.xml", workbookXml);
        WriteEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);
        WriteEntry(zip, "xl/worksheets/sheet1.xml", sheetXml);
        WriteEntry(zip, "xl/sharedStrings.xml", sharedStringsXml);
    }

    private static void WriteEntry(ZipArchive zip, string name, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
