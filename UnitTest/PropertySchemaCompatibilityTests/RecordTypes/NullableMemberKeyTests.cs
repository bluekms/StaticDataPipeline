using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.RecordTypes;

public class NullableMemberKeyTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string RecordSetSource = """
        [StaticDataRecord("Shop", "Bundles")]
        public sealed partial record BundleRecord(
            [Length(2)] FrozenSet<BundleItem> Items
        );

        public sealed record BundleItem(int ItemId, [NullString("-")] int? Bonus);
        """;

    // language=C#
    private const string RecordMapSource = """
        [StaticDataRecord("Shop", "Rewards")]
        public sealed partial record RewardRecord(
            [Length(2)] FrozenDictionary<int, Reward> Rewards
        );

        public sealed record Reward([Key] int Grade, [NullString("-")] int? Bonus);
        """;

    // language=C#
    private const string NullableOnlyMemberSource = """
        [StaticDataRecord("Shop", "Tags")]
        public sealed partial record TagRecord(
            [Length(2)] FrozenSet<Tag> Tags
        );

        public sealed record Tag([NullString("-")] string? Label);
        """;

    [Fact]
    public void RecordSet_NullMemberInElementsWithDifferentValues_IsNotDuplicate()
    {
        var logger = CreateLogger();

        Check(RecordSetSource, ["100", "-", "200", "-"], logger);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordSet_SameElementsWithNullMember_IsDuplicate()
    {
        var logger = CreateLogger();

        var ex = Assert.Throws<InvalidOperationException>(() => Check(RecordSetSource, ["100", "-", "100", "-"], logger));

        Assert.StartsWith(DuplicatePrefix(), ex.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordMap_NonKeyMemberNullInTwoValues_IsNotDuplicate()
    {
        var logger = CreateLogger();

        Check(RecordMapSource, ["1", "-", "2", "-"], logger);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordSet_OnlyMemberNull_IsNullKey()
    {
        var logger = CreateLogger();

        Check(NullableOnlyMemberSource, ["-", "x"], logger);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void RecordSet_OnlyMemberNullInTwoElements_IsDuplicate()
    {
        var logger = CreateLogger();

        var ex = Assert.Throws<InvalidOperationException>(() => Check(NullableOnlyMemberSource, ["-", "-"], logger));

        Assert.StartsWith(DuplicatePrefix(), ex.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private static string DuplicatePrefix()
    {
        var format = Messages.DuplicateValuesInArgument;

        return format[..format.IndexOf('{', StringComparison.Ordinal)];
    }

    private static void Check(string code, string[] values, ILogger logger)
    {
        var catalogs = CreateCatalogs(code, logger);
        var cells = values
            .Select((value, i) => new CellData(FormattableString.Invariant($"A{i + 1}"), value))
            .ToList();
        var context = CompatibilityContext.CreateCollectKey(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }
    }

    private static MetadataCatalogs CreateCatalogs(string code, ILogger logger)
    {
        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var enumMemberCatalog = new EnumMemberCatalog(loadResult);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        return new(recordSchemaCatalog, enumMemberCatalog);
    }

    private TestOutputLogger<NullableMemberKeyTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullableMemberKeyTests>() is not TestOutputLogger<NullableMemberKeyTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
