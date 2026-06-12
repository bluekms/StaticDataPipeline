using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.CsvLoaderTests;

public partial class ArraySheet2CsvLoaderTests
{
    [StaticDataRecord("Excel1", "ArraySheet")]
    private sealed partial record ArraySheet(
        int Id,
        string Name,
        [ColumnName("Score")]
        [Length(3)]
        ImmutableArray<int> Scores);

    private const string ArraySheetCsv =
        """
        Id,Name,Score[0],Score[1],Score[2]
        1,Alice,95,88,92
        2,田中太郎,85,90,87
        3,김철수,90,85,88
        4,Hans Müller,82,88,85
        5,Мария Петрова,91,89,93
        """;

    [Fact]
    public void Load_ArraySheet2Csv_AllRecordsHaveThreeScores()
    {
        var records = CsvLoader.Parse(ArraySheetCsv, ArraySheet.MapFromCsvRow);

        foreach (var record in records)
        {
            Assert.Equal(3, record.Scores.Length);
        }
    }
}
