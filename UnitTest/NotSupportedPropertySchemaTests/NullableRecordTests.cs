using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.NotSupportedPropertySchemaTests;

public partial class NullableRecordTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void RejectsTopLevelNullableRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullableRecordTests>() is not TestOutputLogger<NullableRecordTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed partial record Position(int X, int Y);

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       int Id,
                       Position? Point
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Throws<NotSupportedException>(() => RecordComplianceChecker.Check(recordSchemaCatalog, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void AcceptsNonNullableRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NullableRecordTests>() is not TestOutputLogger<NullableRecordTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed partial record Position(int X, int Y);

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       int Id,
                       Position Point
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        Assert.Empty(logger.Logs);
    }
}
