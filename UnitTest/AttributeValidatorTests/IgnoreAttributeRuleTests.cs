using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.AttributeValidatorTests;

public class IgnoreAttributeRuleTests(ITestOutputHelper testOutputHelper)
{
    // ImmutableArray<int>은 [Length] 없이 사용하면 InvalidAttributeUsageException 발생
    // [Ignore]가 붙은 파라미터는 컴플라이언스 검사를 건너뜀
    [Fact]
    public void ParameterLevel_IgnoredParameter_SkipsComplianceCheck()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<IgnoreAttributeRuleTests>() is not TestOutputLogger<IgnoreAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Ignore]
                       ImmutableArray<int> IgnoredProperty,
                       int Id,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        RecordComplianceChecker.Check(recordSchemaCatalog, logger);
        Assert.Empty(logger.Logs);
    }

    // [Ignore]가 붙은 레코드는 카탈로그에서 제외됨
    [Fact]
    public void RecordLevel_IgnoredRecord_ExcludedFromCatalog()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<IgnoreAttributeRuleTests>() is not TestOutputLogger<IgnoreAttributeRuleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   [Ignore]
                   public sealed record MyRecord(
                       int Id,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        var recordSchemaSet = new RecordSchemaSet(loadResult, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);

        Assert.Empty(recordSchemaCatalog.StaticDataRecordSchemata);
        Assert.Empty(logger.Logs);
    }
}
