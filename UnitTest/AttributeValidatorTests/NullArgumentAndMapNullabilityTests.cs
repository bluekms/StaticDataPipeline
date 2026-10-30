using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Exceptions;
using UnitTest.Utility;
using Xunit.Abstractions;
using ScannerMessages = SchemaInfoScanner.Resources.Messages;

namespace UnitTest.AttributeValidatorTests;

public class NullArgumentAndMapNullabilityTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void NullStringWithNullArgument_IsRejectedWithDedicatedMessage()
    {
        var logger = CreateLogger();

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       int Id,
                       [NullString(null)] int? Bonus
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(new RecordSchemaSet(loadResult, logger));

        var ex = Assert.Throws<InvalidAttributeUsageException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));

        Assert.Contains(Format(ScannerMessages.Composite.AttributeArgumentMustNotBeNull, "NullString", "MyRecord.Bonus"), ex.Message, StringComparison.Ordinal);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void SingleColumnCollectionWithNullSeparator_IsRejectedWithDedicatedMessage()
    {
        var logger = CreateLogger();

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       int Id,
                       [SingleColumnCollection(null)] ImmutableArray<int> Tags
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var ex = Assert.Throws<InvalidOperationException>(() => new RecordSchemaSet(loadResult, logger));

        Assert.Equal(Format(ScannerMessages.Composite.AttributeArgumentMustNotBeNull, "SingleColumnCollection", "MyRecord.Tags"), ex.Message);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DictionaryWithNullableKey_ReportsPropertyName()
    {
        var logger = CreateLogger();

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Length(1)] FrozenDictionary<int?, Reward> Rewards
                   );

                   public sealed record Reward([Key] int Grade, int Amount);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var ex = Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));

        Assert.Equal(Format(ScannerMessages.Composite.DictionaryKeyMustBeNonNullable, "MyRecord.Rewards"), ex.Message);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void DictionaryWithNullableValue_ReportsPropertyName()
    {
        var logger = CreateLogger();

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       [Length(1)] FrozenDictionary<int, Reward?> Rewards
                   );

                   public sealed record Reward([Key] int Grade, int Amount);
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);

        var ex = Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));

        Assert.Equal(Format(ScannerMessages.Composite.DictionaryValueMustBeNonNullable, "MyRecord.Rewards"), ex.Message);
        Assert.Single(logger.Logs);
    }

    private static string Format(CompositeFormat format, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    private TestOutputLogger<NullArgumentAndMapNullabilityTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullArgumentAndMapNullabilityTests>() is not TestOutputLogger<NullArgumentAndMapNullabilityTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
