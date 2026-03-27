using Sdp.Csv;

namespace UnitTest.CsvLoaderTests;

public class CsvLoaderTests
{
    public sealed record SimpleRecord(int Id, string Name, double Score);

    [Fact]
    public void Parse_WithValidCsv_ReturnsImmutableList()
    {
        var csv = """
            Id,Name,Score
            1,Alice,95.5
            2,Bob,87.3
            3,Charlie,92.1
            """;

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.Equal(3, result.Count);

        Assert.Equal(1, result[0].Id);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(95.5, result[0].Score);

        Assert.Equal(2, result[1].Id);
        Assert.Equal("Bob", result[1].Name);
        Assert.Equal(87.3, result[1].Score);

        Assert.Equal(3, result[2].Id);
        Assert.Equal("Charlie", result[2].Name);
        Assert.Equal(92.1, result[2].Score);
    }

    [Fact]
    public void Parse_WithEmptyContent_ReturnsEmptyList()
    {
        var csv = string.Empty;

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WithHeaderOnly_ReturnsEmptyList()
    {
        var csv = "Id,Name,Score";

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WithBlankLines_SkipsBlankLines()
    {
        var csv = """
            Id,Name,Score
            1,Alice,95.5

            2,Bob,87.3
            """;

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void Parse_ResultIsImmutable()
    {
        var csv = """
            Id,Name,Score
            1,Alice,95.5
            """;

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.IsType<System.Collections.Immutable.ImmutableList<SimpleRecord>>(result);
    }

    [Fact]
    public void Load_WithValidFile_ReturnsImmutableList()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var csv = """
                Id,Name,Score
                1,Alice,95.5
                2,Bob,87.3
                """;
            File.WriteAllText(tempFile, csv);

            var result = CsvLoader.Load<SimpleRecord>(tempFile);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_WithValidFile_ReturnsImmutableList()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var csv = """
                Id,Name,Score
                1,Alice,95.5
                2,Bob,87.3
                """;
            await File.WriteAllTextAsync(tempFile, csv);

            var result = await CsvLoader.LoadAsync<SimpleRecord>(tempFile);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_WithQuotedFieldContainingComma_ParsesCorrectly()
    {
        var csv = """
            Id,Name,Score
            1,"Smith, John",95.5
            2,"Doe, Jane",87.3
            """;

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.Equal(2, result.Count);
        Assert.Equal("Smith, John", result[0].Name);
        Assert.Equal("Doe, Jane", result[1].Name);
    }

    [Fact]
    public void Parse_WithMultiLineQuotedField_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Hello\nWorld\",95.5\n2,Bob,87.3";

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.Equal(2, result.Count);
        Assert.Equal("Hello\nWorld", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public void Parse_WithEscapedQuotes_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Say \"\"Hello\"\"\",95.5";

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        var record = Assert.Single(result);
        Assert.Equal("Say \"Hello\"", record.Name);
    }

    [Fact]
    public void Parse_WithComplexMultiLineField_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Line1\nLine2\nLine3\",95.5\n2,Simple,87.3";

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        Assert.Equal(2, result.Count);
        Assert.Equal("Line1\nLine2\nLine3", result[0].Name);
        Assert.Equal("Simple", result[1].Name);
    }

    [Fact]
    public void Parse_WithQuotedFieldContainingCommaAndNewline_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Hello, World\nGoodbye\",95.5";

        var result = CsvLoader.Parse<SimpleRecord>(csv);

        var record = Assert.Single(result);
        Assert.Equal("Hello, World\nGoodbye", record.Name);
    }
}
