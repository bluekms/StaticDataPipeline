using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public partial class IntegerNumberStyleTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("byte", "1,000")]
    [InlineData("sbyte", "1,00")]
    [InlineData("short", "1,000")]
    [InlineData("ushort", "1,000")]
    [InlineData("int", "1,000")]
    [InlineData("int", "1.0")]
    [InlineData("uint", "1,000")]
    [InlineData("long", "1,000")]
    [InlineData("ulong", "1,000")]
    public void IntegerCell_WithSeparatorOrDecimalPoint_IsRejected(string type, string argument)
    {
        var logger = CreateLogger();
        var context = CreateContext(type, argument, logger);

        var propertySchema = Assert.Single(context.Catalogs.RecordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata);
        Assert.Throws<InvalidOperationException>(() => propertySchema.CheckCompatibility(context.Context));
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("int", "1000")]
    [InlineData("int", "-1000")]
    [InlineData("long", "9223372036854775807")]
    public void IntegerCell_PlainDigits_IsAccepted(string type, string argument)
    {
        var logger = CreateLogger();
        var context = CreateContext(type, argument, logger);

        var propertySchema = Assert.Single(context.Catalogs.RecordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata);
        propertySchema.CheckCompatibility(context.Context);
        Assert.Empty(logger.Logs);
    }

    private sealed record CompatibilitySetup(MetadataCatalogs Catalogs, CompatibilityContext Context);

    private static CompatibilitySetup CreateContext(string type, string argument, ILogger logger)
    {
        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord(
                        {type} Property,
                    );
                    """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var enumMemberCatalog = new EnumMemberCatalog(loadResult);
        var catalogs = new MetadataCatalogs(recordSchemaCatalog, enumMemberCatalog);

        var cells = new[] { new CellData("A1", argument) };
        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        return new CompatibilitySetup(catalogs, context);
    }

    private TestOutputLogger<IntegerNumberStyleTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<IntegerNumberStyleTests>() is not TestOutputLogger<IntegerNumberStyleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
