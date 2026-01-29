using Docs.SampleRecord.Excel1;
using Eds.Csv;

namespace UnitTest.CsvLoaderTests;

public class ArraySheet2CsvLoaderTests
{
    [Fact]
    public void Load_ArraySheet2Csv_AllRecordsHaveThreeScores()
    {
        var csvPath = GetCsvPath("Excel1.ArraySheet.csv");

        var records = CsvLoader.Load<ArraySheet>(csvPath);

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
