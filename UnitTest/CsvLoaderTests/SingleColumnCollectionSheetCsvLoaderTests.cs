using Docs.SampleRecords.Excel1;
using Sdp.Csv;

namespace UnitTest.CsvLoaderTests;

public class SingleColumnCollectionSheetCsvLoaderTests
{
    private const string SingleColumnCollectionSheetCsv =
        """
        Id,Values
        1,99.9
        2,"1.234, 5.678"
        3,"123.456, 78.9, 0.123"
        4,"10.0, 20.0, 30.0, 40.0"
        5,"1.1, 2.2, 3.3, 4.4, 5.5"
        6,100
        7,"50.5, 60.6"
        8,567.89
        9,"0.1, 0.2, 0.3"
        """;

    [Fact]
    public void Load_SingleColumnCollectionSheetCsv_ReturnsValidRecords()
    {
        var records = CsvLoader.Parse<SingleColumnCollectionSheet>(SingleColumnCollectionSheetCsv);

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
        var records = CsvLoader.Parse<SingleColumnCollectionSheet>(SingleColumnCollectionSheetCsv);

        foreach (var record in records)
        {
            Assert.NotEmpty(record.Values);
            foreach (var value in record.Values)
            {
                Assert.True(float.IsFinite(value));
            }
        }
    }
}
