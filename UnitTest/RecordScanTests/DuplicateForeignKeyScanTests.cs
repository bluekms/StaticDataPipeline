using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

public class DuplicateForeignKeyScanTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TwoForeignKeysOnOneParameter_FailTheScanWithCompileError()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<DuplicateForeignKeyScanTests>() is not TestOutputLogger<DuplicateForeignKeyScanTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        const string code =
            """
            using Sdp.Attributes;

            [StaticDataRecord("Invalid", "Attendance")]
            public sealed partial record AttendanceRecord(
                [Key] byte Day,
                [ForeignKey("ItemTable", "Id")]
                [ForeignKey("CurrencyTable", "Id")]
                int RewardId);
            """;

        var exception = Record.Exception(() => SimpleCordParser.Parse(code, logger));

        Assert.NotNull(exception);
        var reported = exception.Message + Environment.NewLine + string.Join(Environment.NewLine, logger.Logs.Select(x => x.Message));
        Assert.Contains("CS0579", reported, StringComparison.Ordinal);
    }
}
