using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;
using SdpMessages = Sdp.Resources.Messages;

namespace UnitTest.CsvLoaderTests;

public class CsvMapperHelperTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void ParseInt32_RejectsThousandsSeparator_WithColumnAndType()
    {
        var logger = CreateLogger();

        var ex = Assert.Throws<FormatException>(() => CsvMapperHelper.ParseInt32("1,000", "Level"));

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "1,000", "int", "Level"), ex.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ParseInt32_Overflow_IsReportedAsInvalidCell()
    {
        var logger = CreateLogger();

        var ex = Assert.Throws<FormatException>(() => CsvMapperHelper.ParseInt32("99999999999", "Level"));

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "99999999999", "int", "Level"), ex.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ParseSingle_AcceptsThousandsSeparatorAndExponent()
    {
        var logger = CreateLogger();

        Assert.Equal(1000.5f, CsvMapperHelper.ParseSingle("1,000.5", "Rate"));
        Assert.Equal(1500f, CsvMapperHelper.ParseSingle("1.5e3", "Rate"));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ParseDateTime_WrongFormat_ReportsFormat()
    {
        var logger = CreateLogger();

        var ex = Assert.Throws<FormatException>(() => CsvMapperHelper.ParseDateTime("2024/01/02", "yyyy-MM-dd", "Start"));

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalidFormat, "2024/01/02", "DateTime", "yyyy-MM-dd", "Start"), ex.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ParseGuid_Invalid_ReportsColumn()
    {
        var logger = CreateLogger();

        var ex = Assert.Throws<FormatException>(() => CsvMapperHelper.ParseGuid("not-a-guid", "Id"));

        Assert.Equal(Format(SdpMessages.Composite.CellValueInvalid, "not-a-guid", "Guid", "Id"), ex.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ValueOutOfRange_CarriesActualValueAndParamName()
    {
        var logger = CreateLogger();

        var ex = Assert.IsType<ArgumentOutOfRangeException>(CsvMapperHelper.ValueOutOfRange("Level", "1", "9", 10));

        Assert.Equal("value", ex.ParamName);
        Assert.Equal(10, ex.ActualValue);
        Assert.StartsWith(Format(SdpMessages.Composite.MappedValueOutOfRange, "Level", "1", "9"), ex.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DuplicateSetValue_FormatsValueInvariantly()
    {
        var logger = CreateLogger();
        var savedCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            var ex = CsvMapperHelper.DuplicateSetValue(1.5, "Rates");

            Assert.Equal(Format(SdpMessages.Composite.MappedDuplicateSetValue, "1.5", "Rates"), ex.Message);
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
        }

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void CountOutOfRange_ReportsBoundsAndActualCount()
    {
        var logger = CreateLogger();

        var ex = CsvMapperHelper.CountOutOfRange("Tags", 1, 3, 4);

        Assert.IsType<ArgumentException>(ex);
        Assert.Equal(Format(SdpMessages.Composite.MappedCountOutOfRange, "Tags", 1, 3, 4), ex.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapperNotSupported_IsNotSupportedException()
    {
        var logger = CreateLogger();

        var ex = CsvMapperHelper.MapperNotSupported("Test.Foo");

        Assert.IsType<NotSupportedException>(ex);
        Assert.Equal(Format(SdpMessages.Composite.MapperNotSupported, "Test.Foo"), ex.Message);
        Assert.Empty(logger.Logs);
    }

    private static string Format(CompositeFormat format, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    private TestOutputLogger<CsvMapperHelperTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CsvMapperHelperTests>() is not TestOutputLogger<CsvMapperHelperTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
