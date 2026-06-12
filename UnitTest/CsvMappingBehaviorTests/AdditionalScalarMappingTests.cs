using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class AdditionalScalarMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Scalar", "Bool")]
    public sealed partial record BoolRecord(int Id, bool Enabled);

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    public void Bool_Parses(string cell, bool expected)
    {
        var logger = CreateLogger();

        var csv = $"Id,Enabled\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, BoolRecord.MapFromCsvRow));

        Assert.Equal(expected, record.Enabled);
        logger.LogInformation("Bool cell '{Cell}' mapped to {Enabled}", cell, record.Enabled);
    }

    [StaticDataRecord("Scalar", "Guid")]
    public sealed partial record GuidRecord(global::System.Guid Id, string Name);

    [Fact]
    public void Guid_Parses()
    {
        var logger = CreateLogger();

        var csv = "Id,Name\n12345678-1234-1234-1234-1234567890ab,Alpha";

        var record = Assert.Single(CsvLoader.Parse(csv, GuidRecord.MapFromCsvRow));

        Assert.Equal(global::System.Guid.Parse("12345678-1234-1234-1234-1234567890ab"), record.Id);
        Assert.Equal("Alpha", record.Name);
        logger.LogInformation("Guid record mapped: Id={Id}, Name={Name}", record.Id, record.Name);
    }

    [StaticDataRecord("Scalar", "Char")]
    public sealed partial record CharRecord(int Id, char Grade);

    [Theory]
    [InlineData("A", 'A')]
    [InlineData("Z", 'Z')]
    [InlineData("0", '0')]
    public void Char_Parses(string cell, char expected)
    {
        var logger = CreateLogger();

        var csv = $"Id,Grade\n1,{cell}";

        var record = Assert.Single(CsvLoader.Parse(csv, CharRecord.MapFromCsvRow));

        Assert.Equal(expected, record.Grade);
        logger.LogInformation("Char cell '{Cell}' mapped to {Grade}", cell, record.Grade);
    }

    [Fact]
    public void Char_MultipleCharacters_Throws()
    {
        var logger = CreateLogger();

        var csv = "Id,Grade\n1,AB";

        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, CharRecord.MapFromCsvRow));

        Assert.IsType<FormatException>(ex.InnerException);
        logger.LogInformation("Multi-character char cell threw: {Message}", ex.Message);
    }

    [StaticDataRecord("Scalar", "Integers")]
    public sealed partial record IntegerRecord(
        byte B,
        sbyte Sb,
        short S,
        ushort Us,
        uint Ui,
        ulong Ul);

    [Fact]
    public void UnsignedAndSmallIntegers_Parse()
    {
        var logger = CreateLogger();

        var csv = "B,Sb,S,Us,Ui,Ul\n255,-128,-32768,65535,4000000000,18000000000000000000";

        var record = Assert.Single(CsvLoader.Parse(csv, IntegerRecord.MapFromCsvRow));

        Assert.Equal((byte)255, record.B);
        Assert.Equal((sbyte)-128, record.Sb);
        Assert.Equal((short)-32768, record.S);
        Assert.Equal((ushort)65535, record.Us);
        Assert.Equal(4000000000u, record.Ui);
        Assert.Equal(18000000000000000000ul, record.Ul);
        logger.LogInformation(
            "Integer record mapped: B={B}, Sb={Sb}, S={S}, Us={Us}, Ui={Ui}, Ul={Ul}",
            record.B,
            record.Sb,
            record.S,
            record.Us,
            record.Ui,
            record.Ul);
    }

    [StaticDataRecord("Scalar", "Offset")]
    public sealed partial record DateTimeOffsetRecord(
        int Id,
        [DateTimeFormat("yyyy-MM-ddTHH:mm:sszzz")] global::System.DateTimeOffset Timestamp);

    [Fact]
    public void DateTimeOffset_ParsesWithFormat()
    {
        var logger = CreateLogger();

        var csv = "Id,Timestamp\n1,2026-05-19T13:30:00+09:00";

        var record = Assert.Single(CsvLoader.Parse(csv, DateTimeOffsetRecord.MapFromCsvRow));

        Assert.Equal(
            new global::System.DateTimeOffset(2026, 5, 19, 13, 30, 0, global::System.TimeSpan.FromHours(9)),
            record.Timestamp);
        logger.LogInformation("DateTimeOffset mapped to {Timestamp}", record.Timestamp);
    }

    private TestOutputLogger<AdditionalScalarMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<AdditionalScalarMappingTests>()
            is not TestOutputLogger<AdditionalScalarMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
