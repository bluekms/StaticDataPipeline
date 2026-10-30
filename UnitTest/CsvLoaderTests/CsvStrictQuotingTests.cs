using Sdp.Csv;

namespace UnitTest.CsvLoaderTests;

public class CsvStrictQuotingTests
{
    private sealed record Row(string A, string B);

    [Theory]
    [InlineData("A,B\nab\"c,d\"")]
    [InlineData("A,B\n\"ab\"c,d")]
    public void MalformedQuotes_AreRejectedWithRowNumber(string csv)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, Map, "items.csv"));

        Assert.StartsWith("[items.csv] ", ex.Message, StringComparison.Ordinal);
        Assert.Contains("2", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WellFormedQuotes_StillParse()
    {
        var rows = CsvLoader.Parse("A,B\n\"a,b\",\"say \"\"hi\"\"\"\n\"\",x", Map);

        Assert.Equal(2, rows.Length);
        Assert.Equal(new Row("a,b", "say \"hi\""), rows[0]);
        Assert.Equal(new Row(string.Empty, "x"), rows[1]);
    }

    private static Row Map(CsvHeaderIndex headers, string[] values)
        => new(values[headers["A"]], values[headers["B"]]);
}
