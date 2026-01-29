using Docs.SampleRecord.Excel1;
using Eds.Csv;

namespace UnitTest.CsvLoaderTests;

public class SingleColumnCollectionSheetCsvLoaderTests
{
    [Fact]
    public void Load_SingleColumnCollectionSheetCsv_ReturnsValidRecords()
    {
        var csvPath = GetCsvPath("Excel1.SingleColumnCollectionSheet.csv");

        var records = CsvLoader.Load<SingleColumnCollectionSheet>(csvPath);

        Assert.NotEmpty(records);
        Assert.Equal(9, records.Count);

        // Id=2: "1.234, 5.678"
        var record2 = records.First(r => r.Id == 2);
        Assert.Equal(2, record2.Values.Length);
        Assert.Equal(1.234f, record2.Values[0]);
        Assert.Equal(5.678f, record2.Values[1]);

        // Id=3: "123.456, 78.9, 0.123"
        var record3 = records.First(r => r.Id == 3);
        Assert.Equal(3, record3.Values.Length);
        Assert.Equal(123.456f, record3.Values[0]);
        Assert.Equal(78.9f, record3.Values[1]);
        Assert.Equal(0.123f, record3.Values[2]);

        // Id=8: 567.89 (단일 값, 따옴표 없음)
        var record8 = records.First(r => r.Id == 8);
        Assert.Single(record8.Values);
        Assert.Equal(567.89f, record8.Values[0]);
    }

    [Fact]
    public void Load_SingleColumnCollectionSheetCsv_ParsesAllFloatValues()
    {
        var csvPath = GetCsvPath("Excel1.SingleColumnCollectionSheet.csv");

        var records = CsvLoader.Load<SingleColumnCollectionSheet>(csvPath);

        foreach (var record in records)
        {
            Assert.NotEmpty(record.Values);
            foreach (var value in record.Values)
            {
                Assert.True(float.IsFinite(value));
            }
        }
    }

    private static string GetCsvPath(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        return Path.Combine(solutionDir, "Docs", "SampleCsv", fileName);
    }
}
