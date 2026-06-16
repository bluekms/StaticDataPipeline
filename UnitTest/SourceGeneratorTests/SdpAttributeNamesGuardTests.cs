using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.SourceGenerator.Generators;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class SdpAttributeNamesGuardTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void AttributeNameConstants_MatchRealAttributeTypes()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SdpAttributeNamesGuardTests>() is not TestOutputLogger<SdpAttributeNamesGuardTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        Assert.Equal(nameof(ColumnNameAttribute), SdpAttributeNames.ColumnName);
        Assert.Equal(nameof(KeyAttribute), SdpAttributeNames.Key);
        Assert.Equal(nameof(NullStringAttribute), SdpAttributeNames.NullString);
        Assert.Equal(nameof(DateTimeFormatAttribute), SdpAttributeNames.DateTimeFormat);
        Assert.Equal(nameof(TimeSpanFormatAttribute), SdpAttributeNames.TimeSpanFormat);
        Assert.Equal(nameof(RangeAttribute), SdpAttributeNames.Range);
        Assert.Equal(nameof(RegularExpressionAttribute), SdpAttributeNames.RegularExpression);
        Assert.Equal(nameof(LengthAttribute), SdpAttributeNames.Length);
        Assert.Equal(nameof(SingleColumnCollectionAttribute), SdpAttributeNames.SingleColumnCollection);
        Assert.Equal(nameof(CountRangeAttribute), SdpAttributeNames.CountRange);
        Assert.Equal(nameof(ForeignKeyAttribute), SdpAttributeNames.ForeignKey);
        Assert.Equal(nameof(SwitchForeignKeyAttribute), SdpAttributeNames.SwitchForeignKey);
        Assert.Equal(nameof(IgnoreAttribute), SdpAttributeNames.Ignore);

        Assert.Equal(SdpAttributeNames.Namespace, typeof(SingleColumnCollectionAttribute).Namespace);

        Assert.Empty(logger.Logs);
    }
}
