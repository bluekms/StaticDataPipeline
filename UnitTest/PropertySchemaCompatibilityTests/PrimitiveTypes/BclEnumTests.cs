using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes;

public class BclEnumTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("DayOfWeek Day", "Monday", true)]
    [InlineData("DayOfWeek Day", "Funday", false)]
    [InlineData("DayOfWeek Day", "1", false)]
    [InlineData("DayOfWeek Day", "monday", false)]
    [InlineData("[Range(typeof(DayOfWeek), \"Monday\", \"Friday\")] DayOfWeek Day", "Wednesday", true)]
    [InlineData("[Range(typeof(DayOfWeek), \"Monday\", \"Friday\")] DayOfWeek Day", "Saturday", false)]
    [InlineData("[NullString(\"\")] DayOfWeek? Day", "", true)]
    [InlineData("[NullString(\"\")] DayOfWeek? Day", "Sunday", true)]
    [InlineData("[NullString(\"\")] DayOfWeek? Day", "Someday", false)]
    public void BclEnumCell_IsCheckedAgainstMembers(string parameterDeclaration, string cellValue, bool accepted)
    {
        var logger = CreateLogger();

        // language=C#
        var code = $"""
                    using System;
                    using Sdp.Attributes;

                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord([Key] int Id, {parameterDeclaration});
                    """;

        var catalogs = RangeAttributeTestHelpers.CreateCatalogs(code, logger);
        var schema = catalogs.RecordSchemaCatalog.StaticDataRecordSchemata[0].PropertySchemata[1];
        var context = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("B2", cellValue)]);

        var ex = Record.Exception(() => schema.CheckCompatibility(context));

        Assert.Equal(accepted, ex is null);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<BclEnumTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<BclEnumTests>() is not TestOutputLogger<BclEnumTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
