using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Resources;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvLoaderTests;

public partial class CsvLoaderTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Simple", "SimpleSheet")]
    public sealed partial record SimpleRecord(int Id, string Name, double Score);

    [Fact]
    public void Parse_WithValidCsv_ReturnsImmutableArray()
    {
        var csv = """
                  Id,Name,Score
                  1,Alice,95.5
                  2,Bob,87.3
                  3,Charlie,92.1
                  """;

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        Assert.Equal(3, result.Length);

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

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WithHeaderOnly_ReturnsEmptyList()
    {
        var csv = "Id,Name,Score";

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

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

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        Assert.Equal(2, result.Length);
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

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        Assert.IsType<System.Collections.Immutable.ImmutableArray<SimpleRecord>>(result);
    }

    [Fact]
    public async Task Load_WithValidFile_ReturnsImmutableArray()
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

            var result = await CsvLoader.LoadAsync(tempFile, SimpleRecord.MapFromCsvRow);

            Assert.Equal(2, result.Length);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadAsync_WithValidFile_ReturnsImmutableArray()
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

            var result = await CsvLoader.LoadAsync(tempFile, SimpleRecord.MapFromCsvRow);

            Assert.Equal(2, result.Length);
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

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        Assert.Equal(2, result.Length);
        Assert.Equal("Smith, John", result[0].Name);
        Assert.Equal("Doe, Jane", result[1].Name);
    }

    [Fact]
    public void Parse_WithMultiLineQuotedField_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Hello\nWorld\",95.5\n2,Bob,87.3";

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        Assert.Equal(2, result.Length);
        Assert.Equal("Hello\nWorld", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public void Parse_WithEscapedQuotes_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Say \"\"Hello\"\"\",95.5";

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        var record = Assert.Single(result);
        Assert.Equal("Say \"Hello\"", record.Name);
    }

    [Fact]
    public void Parse_WithComplexMultiLineField_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Line1\nLine2\nLine3\",95.5\n2,Simple,87.3";

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        Assert.Equal(2, result.Length);
        Assert.Equal("Line1\nLine2\nLine3", result[0].Name);
        Assert.Equal("Simple", result[1].Name);
    }

    [Fact]
    public void Parse_WithQuotedFieldContainingCommaAndNewline_ParsesCorrectly()
    {
        var csv = "Id,Name,Score\n1,\"Hello, World\nGoodbye\",95.5";

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        var record = Assert.Single(result);
        Assert.Equal("Hello, World\nGoodbye", record.Name);
    }

    [Fact]
    public void Parse_WithQuotedFieldContainingCrlf_PreservesCrlf()
    {
        // RFC 4180: 따옴표 안의 개행은 원문 그대로 보존된다 (CRLF를 LF로 정규화하지 않는다).
        var csv = "Id,Name,Score\n1,\"Line1\r\nLine2\",95.5";

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        var record = Assert.Single(result);
        Assert.Equal("Line1\r\nLine2", record.Name);
    }

    [Fact]
    public void Parse_WithUnterminatedQuote_ThrowsWithExplicitMessage()
    {
        var csv = "Id,Name,Score\n1,\"Alice,95.5";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow));

        Assert.Equal(Messages.CsvUnterminatedQuote, ex.Message);
    }

    [Fact]
    public void Parse_WithRowFieldShortage_ThrowsWithRowContext()
    {
        var csv = "Id,Name,Score\n1,Alice,95.5\n2,Bob";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow));

        var expectedShortage = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.CsvRowFieldCountShortage,
            2,
            3);
        var expectedLocation = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.CsvRowWithoutFile,
            3);

        Assert.Contains(expectedShortage, ex.Message, StringComparison.Ordinal);
        Assert.Contains(expectedLocation, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithTrailingCommaHeaders_IgnoresEmptyHeaders()
    {
        var logger = CreateLogger();

        // 헤더 행 끝에 콤마가 붙으면 빈 문자열 헤더가 여러 개 생긴다. 매핑 키가 될 수 없으므로
        // 중복 헤더 검사 대상에서 제외되어 정상 로드되어야 한다.
        var csv = """
                  Id,Name,Score,,
                  1,Alice,95.5,,
                  """;

        var result = CsvLoader.Parse(csv, SimpleRecord.MapFromCsvRow);

        var record = Assert.Single(result);
        Assert.Equal(1, record.Id);
        Assert.Equal("Alice", record.Name);
        Assert.Equal(95.5, record.Score);
        logger.LogInformation("Trailing-comma headers ignored, record loaded: Id={Id}", record.Id);
    }

    private TestOutputLogger<CsvLoaderTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<CsvLoaderTests>() is not TestOutputLogger<CsvLoaderTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
