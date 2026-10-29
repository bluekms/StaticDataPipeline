using Microsoft.Extensions.Logging;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvLoaderTests;

public class CsvEmptyRowSkipTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Parse_RowEmptyForRecord_IsSkipped()
    {
        var logger = CreateLogger();
        const string csv = "A,B\n1,x\n,y\n2,\n";

        var records = CsvLoader.Parse(csv, (headers, values) => values[headers["A"]], null, (headers, values) => values[headers["A"]].Length == 0);

        Assert.Equal(["1", "2"], records.ToArray());
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void IsEmptyRow_ChecksColumnsAndPrefixes()
    {
        var logger = CreateLogger();
        var headers = new CsvHeaderIndex(["A", "Items[0]", "Items[1]", "Other"]);
        string[] columns = ["A", "Missing"];
        string[] prefixes = ["Items["];

        Assert.True(CsvLoader.IsEmptyRow(headers, [string.Empty, string.Empty, string.Empty, "z"], columns, prefixes));
        Assert.False(CsvLoader.IsEmptyRow(headers, ["1", string.Empty, string.Empty, string.Empty], columns, prefixes));
        Assert.False(CsvLoader.IsEmptyRow(headers, [string.Empty, string.Empty, "x", string.Empty], columns, prefixes));
        Assert.False(CsvLoader.IsEmptyRow(headers, [string.Empty, string.Empty, string.Empty, string.Empty], [], []));
        Assert.False(CsvLoader.IsEmptyRow(headers, [string.Empty, string.Empty, string.Empty, string.Empty], ["Missing"], ["None["]));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Parse_NoRecordColumnInCsv_IsNotSkippedAndMapperReportsMissingHeader()
    {
        var logger = CreateLogger();
        const string csv = "Other\n1\n";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(
            csv,
            (headers, values) => values[headers["Grade"]],
            null,
            (headers, values) => CsvLoader.IsEmptyRow(headers, values, ["Grade"], [])));

        var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("'Grade'", inner.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<CsvEmptyRowSkipTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CsvEmptyRowSkipTests>() is not TestOutputLogger<CsvEmptyRowSkipTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
