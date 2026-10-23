using ExcelColumnExtractor.Extensions;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StartCellResolverTests;

public partial class StartCellResolverTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void NoStartCellInAttribute_ReturnsFallback()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StartCellResolverTests>() is not TestOutputLogger<StartCellResolverTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       int Id,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var recordSchema = recordSchemaCatalog.StaticDataRecordSchemata.Single();

        var result = StartCellResolver.Resolve(recordSchema, "B5");

        Assert.Equal("B5", result);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void StartCellSpecifiedInAttribute_OverridesFallback()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StartCellResolverTests>() is not TestOutputLogger<StartCellResolverTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet", "C7")]
                   public sealed partial record MyRecord(
                       int Id,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var recordSchema = recordSchemaCatalog.StaticDataRecordSchemata.Single();

        var result = StartCellResolver.Resolve(recordSchema, "B5");

        Assert.Equal("C7", result);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void EmptyStartCellInAttribute_ThrowsArgumentException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<StartCellResolverTests>() is not TestOutputLogger<StartCellResolverTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet", "")]
                   public sealed partial record MyRecord(
                       int Id,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        var recordSchema = recordSchemaCatalog.StaticDataRecordSchemata.Single();

        Assert.Throws<ArgumentException>(() => StartCellResolver.Resolve(recordSchema, "B5"));
        Assert.Empty(logger.Logs);
    }
}
