using System.Collections.Immutable;
using Eds.Attributes;
using Eds.Csv;

namespace UnitTest.CsvLoaderTests;

public class ArraySheet2CsvLoaderTests
{
    [StaticDataRecord("Excel1", "ArraySheet")]
    public sealed record ArraySheet2(
        string Name,

        [ColumnName("Score")]
        [Length(3)]
        ImmutableArray<int> Scores);

    [Fact]
    public void Load_ArraySheet2Csv_AllRecordsHaveThreeScores()
    {
        var csvPath = GetCsvPath("Excel1.ArraySheet.csv");

        var records = CsvLoader.Load<ArraySheet2>(csvPath);

        foreach (var record in records)
        {
            Assert.Equal(3, record.Scores.Length);
        }
    }

    private static string GetCsvPath(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        return Path.Combine(solutionDir, "Docs", "SampleCsv", fileName);
    }
}
