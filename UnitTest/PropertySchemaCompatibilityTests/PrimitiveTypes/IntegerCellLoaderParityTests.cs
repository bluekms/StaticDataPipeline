using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;
using Sdp.Csv;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class IntegerCellLoaderParityTests
{
    public static TheoryData<string> Texts
    {
        get
        {
            return new TheoryData<string>
            {
                "5",
                "-5",
                "+5",
                " 5",
                "5 ",
                "005",
                "1,000",
                "1.0",
                "0x10",
                "1e3",
                string.Empty,
                "255",
                "256",
                "2147483647",
                "2147483648",
                "18446744073709551615",
                "18446744073709551616",
            };
        }
    }

    [Theory]
    [MemberData(nameof(Texts))]
    public void ScannerAndLoaderAcceptTheSameByteText(string text)
    {
        var scannerAccepts = IntegerCell.TryParse<byte>(text, out _);
        var loaderFailure = Record.Exception(() => CsvMapperHelper.ParseByte(text, "Column"));

        Assert.Equal(scannerAccepts, loaderFailure is null);
    }

    [Theory]
    [MemberData(nameof(Texts))]
    public void ScannerAndLoaderAcceptTheSameInt32Text(string text)
    {
        var scannerAccepts = IntegerCell.TryParse<int>(text, out _);
        var loaderFailure = Record.Exception(() => CsvMapperHelper.ParseInt32(text, "Column"));

        Assert.Equal(scannerAccepts, loaderFailure is null);
    }

    [Theory]
    [MemberData(nameof(Texts))]
    public void ScannerAndLoaderAcceptTheSameUInt64Text(string text)
    {
        var scannerAccepts = IntegerCell.TryParse<ulong>(text, out _);
        var loaderFailure = Record.Exception(() => CsvMapperHelper.ParseUInt64(text, "Column"));

        Assert.Equal(scannerAccepts, loaderFailure is null);
    }
}
