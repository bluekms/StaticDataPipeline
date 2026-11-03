using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.NotSupportedPropertySchemaTests;

public class UndeclaredTypeTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("DayOfWeek Day")]
    [InlineData("System.DayOfWeek Day")]
    [InlineData("[NullString(\"\")] DayOfWeek? Day")]
    public void BclEnum_IsAccepted(string parameterDeclaration)
    {
        var logger = CreateLogger();

        // language=C#
        var code = $"""
                    using System;
                    using Sdp.Attributes;

                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord(
                        [Key] int Id,
                        {parameterDeclaration},
                    );
                    """;

        var catalogs = CreateCatalogs(code, logger);

        var recordSchema = Assert.Single(catalogs.RecordSchemaCatalog.StaticDataRecordSchemata);
        Assert.Equal(2, recordSchema.PropertySchemata.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void TypeFromUnreferencedAssembly_IsRejectedWithFolderGuidance()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            using Sdp.Attributes;

                            [StaticDataRecord("Test", "TestSheet")]
                            public sealed partial record MyRecord(
                                [Key] int Id,
                                Game.Shared.Grade Grade,
                            );
                            """;

        var ex = Assert.Throws<NotSupportedException>(() => CreateCatalogs(code, logger));
        Assert.Contains("Grade", ex.Message);
        Assert.Contains("MyRecord.Grade", ex.Message);
    }

    [Fact]
    public void EnumDeclaredInRecordSource_IsAccepted()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
                            using Sdp.Attributes;

                            public enum Weekday { Mon, Tue, Wed, Thu, Fri }

                            [StaticDataRecord("Test", "TestSheet")]
                            public sealed partial record MyRecord(
                                [Key] int Id,
                                Weekday Day,
                            );
                            """;

        var catalogs = CreateCatalogs(code, logger);

        Assert.Single(catalogs.RecordSchemaCatalog.StaticDataRecordSchemata);
        Assert.Empty(logger.Logs);
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

    private TestOutputLogger<UndeclaredTypeTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<UndeclaredTypeTests>() is not TestOutputLogger<UndeclaredTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
