using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;
using SdpMessages = Sdp.Resources.Messages;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class CellErrorColumnMappingTests(ITestOutputHelper testOutputHelper)
{
    public enum Grade
    {
        A,
        B,
    }

    public sealed record Position(int X, int Y);

    [StaticDataRecord("Cell", "Scalar")]
    public sealed partial record ScalarRecord(int Id, int Level);

    [StaticDataRecord("Cell", "Nested")]
    public sealed partial record NestedRecord(int Id, Position Pos);

    [StaticDataRecord("Cell", "Fixed")]
    public sealed partial record FixedArrayRecord(int Id, [Length(2)] ImmutableArray<int> Ids);

    [StaticDataRecord("Cell", "Single")]
    public sealed partial record SingleColumnRecord(int Id, [SingleColumnCollection(",")] ImmutableArray<int> Tags);

    [StaticDataRecord("Cell", "Enum")]
    public sealed partial record EnumRecord(int Id, Grade Rank);

    [StaticDataRecord("Cell", "Nullable")]
    public sealed partial record NullableRecord(int Id, [NullString("-")] int? Bonus);

    [StaticDataRecord("Cell", "Date")]
    public sealed partial record DateRecord(int Id, [DateTimeFormat("yyyy-MM-dd")] DateTime Start);

    [StaticDataRecord("Cell", "Range")]
    public sealed partial record RangeRecord(int Id, [Range(1, 9)] int Level);

    [StaticDataRecord("Cell", "Set")]
    public sealed partial record SetRecord(int Id, [Length(2)] FrozenSet<int> Slots);

    [Fact]
    public void Scalar_InvalidInteger_ReportsColumnAndType()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<FormatException, ScalarRecord>("Id,Level\n1,abc", ScalarRecord.MapFromCsvRow);

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "abc", "int", "Level"), inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NestedRecordMember_InvalidInteger_ReportsDottedColumn()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<FormatException, NestedRecord>("Id,Pos.X,Pos.Y\n1,a,2", NestedRecord.MapFromCsvRow);

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "a", "int", "Pos.X"), inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void FixedLengthArrayElement_InvalidInteger_ReportsIndexedColumn()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<FormatException, FixedArrayRecord>("Id,Ids[0],Ids[1]\n1,5,x", FixedArrayRecord.MapFromCsvRow);

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "x", "int", "Ids[1]"), inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SingleColumnPart_InvalidInteger_ReportsColumn()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<FormatException, SingleColumnRecord>("Id,Tags\n1,\"1, x\"", SingleColumnRecord.MapFromCsvRow);

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "x", "int", "Tags"), inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Enum_UndefinedMember_ReportsColumnAndEnumName()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<ArgumentException, EnumRecord>("Id,Rank\n1,C", EnumRecord.MapFromCsvRow);

        var enumName = typeof(CellErrorColumnMappingTests).FullName + ".Grade";
        Assert.StartsWith(Format(SdpMessages.Composite.CellEnumMemberUndefined, "C", enumName, "Rank"), inner.Message, StringComparison.Ordinal);
        Assert.Equal("value", inner.ParamName);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NullableScalar_InvalidInteger_ReportsColumn()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<FormatException, NullableRecord>("Id,Bonus\n1,abc", NullableRecord.MapFromCsvRow);

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "abc", "int", "Bonus"), inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DateTime_WrongFormat_ReportsColumnAndFormat()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<FormatException, DateRecord>("Id,Start\n1,2024/01/02", DateRecord.MapFromCsvRow);

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalidFormat, "2024/01/02", "DateTime", "yyyy-MM-dd", "Start"), inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Range_OutOfBounds_ReportsNameBoundsAndValue()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<ArgumentOutOfRangeException, RangeRecord>("Id,Level\n1,10", RangeRecord.MapFromCsvRow);

        Assert.StartsWith(Format(SdpMessages.Composite.MappedValueOutOfRange, "Level", "1", "9"), inner.Message, StringComparison.Ordinal);
        Assert.Equal(10, inner.ActualValue);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void FixedLengthSet_DuplicateValue_ReportsValueAndName()
    {
        var logger = CreateLogger();

        var inner = ParseFailure<ArgumentException, SetRecord>("Id,Slots[0],Slots[1]\n1,3,3", SetRecord.MapFromCsvRow);

        Assert.Equal(Format(SdpMessages.Composite.MappedDuplicateSetValue, "3", "Slots"), inner.Message);
        Assert.Empty(logger.Logs);
    }

    private static TException ParseFailure<TException, TRecord>(string csv, Func<CsvHeaderIndex, string[], TRecord> map)
        where TException : Exception
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CsvLoader.Parse(csv, map));

        return Assert.IsType<TException>(ex.InnerException);
    }

    private static string Format(CompositeFormat format, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    private TestOutputLogger<CellErrorColumnMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CellErrorColumnMappingTests>() is not TestOutputLogger<CellErrorColumnMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
