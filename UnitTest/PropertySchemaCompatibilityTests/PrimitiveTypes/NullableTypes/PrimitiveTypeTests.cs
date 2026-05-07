using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.PropertySchemaCompatibilityTests.PrimitiveTypes.NullableTypes;

public class PrimitiveTypeTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("bool?", "true")]
    [InlineData("bool?", "True")]
    [InlineData("bool?", "TRUE")]
    [InlineData("bool?", "false")]
    [InlineData("bool?", "False")]
    [InlineData("bool?", "FALSE")]
    [InlineData("byte?", "0")]
    [InlineData("byte?", "255")]
    [InlineData("char?", "\0")]
    [InlineData("char?", "\uffff")]
    [InlineData("decimal?", "-79228162514264337593543950335")]
    [InlineData("decimal?", "-79,228,162,514,264,337,593,543,950,335")]
    [InlineData("decimal?", "79228162514264337593543950335")]
    [InlineData("decimal?", "79,228,162,514,264,337,593,543,950,335")]
    [InlineData("double?", "-1.7976931348623157E+308")]
    [InlineData("double?", "-179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368")]
    [InlineData("double?", "-179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368\n")]
    [InlineData("double?", "1.7976931348623157E+308")]
    [InlineData("double?", "179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368")]
    [InlineData("double?", "179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368\n")]
    [InlineData("float?", "-3.40282346638528859e+38")]
    [InlineData("float?", "-340282346638528859811704183484516925440")]
    [InlineData("float?", "-340,282,346,638,528,859,811,704,183,484,516,925,440")]
    [InlineData("float?", "3.40282346638528859e+38")]
    [InlineData("float?", "340282346638528859811704183484516925440")]
    [InlineData("float?", "340,282,346,638,528,859,811,704,183,484,516,925,440")]
    [InlineData("int?", "-2,147,483,648")]
    [InlineData("int?", "-2147483648")]
    [InlineData("int?", "2,147,483,647")]
    [InlineData("int?", "2147483647")]
    [InlineData("long?", "-9,223,372,036,854,775,808")]
    [InlineData("long?", "-9223372036854775808")]
    [InlineData("long?", "9,223,372,036,854,775,807")]
    [InlineData("long?", "9223372036854775807")]
    [InlineData("sbyte?", "-128")]
    [InlineData("sbyte?", "127")]
    [InlineData("short?", "-32,768")]
    [InlineData("short?", "-32768")]
    [InlineData("short?", "32,767")]
    [InlineData("short?", "32767")]
    [InlineData("string?", "")]
    [InlineData("string?", "Hello, World!")]
    [InlineData("uint?", "0")]
    [InlineData("uint?", "4,294,967,295")]
    [InlineData("uint?", "4294967295")]
    [InlineData("ulong?", "0")]
    [InlineData("ulong?", "18,446,744,073,709,551,615")]
    [InlineData("ulong?", "18446744073709551615")]
    [InlineData("ushort?", "0")]
    [InlineData("ushort?", "65,535")]
    [InlineData("ushort?", "65535")]
    public void PrimitiveTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed record MyRecord(
                        [NullString("")] {type} Property,
                    );
                    """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("System.Boolean?", "true")]
    [InlineData("System.Boolean?", "True")]
    [InlineData("System.Boolean?", "TRUE")]
    [InlineData("System.Boolean?", "false")]
    [InlineData("System.Boolean?", "False")]
    [InlineData("System.Boolean?", "FALSE")]
    [InlineData("System.Byte?", "0")]
    [InlineData("System.Byte?", "255")]
    [InlineData("System.Char?", "\0")]
    [InlineData("System.Char?", "\uffff")]
    [InlineData("System.Decimal?", "-79228162514264337593543950335")]
    [InlineData("System.Decimal?", "-79,228,162,514,264,337,593,543,950,335")]
    [InlineData("System.Decimal?", "79228162514264337593543950335")]
    [InlineData("System.Decimal?", "79,228,162,514,264,337,593,543,950,335")]
    [InlineData("System.Double?", "-1.7976931348623157E+308")]
    [InlineData("System.Double?", "-179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368")]
    [InlineData("System.Double?", "-179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368\n")]
    [InlineData("System.Double?", "1.7976931348623157E+308")]
    [InlineData("System.Double?", "179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368")]
    [InlineData("System.Double?", "179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368\n")]
    [InlineData("System.Single?", "-3.40282346638528859e+38")]
    [InlineData("System.Single?", "-340282346638528859811704183484516925440")]
    [InlineData("System.Single?", "-340,282,346,638,528,859,811,704,183,484,516,925,440")]
    [InlineData("System.Single?", "3.40282346638528859e+38")]
    [InlineData("System.Single?", "340282346638528859811704183484516925440")]
    [InlineData("System.Single?", "340,282,346,638,528,859,811,704,183,484,516,925,440")]
    [InlineData("System.Int32?", "-2,147,483,648")]
    [InlineData("System.Int32?", "-2147483648")]
    [InlineData("System.Int32?", "2,147,483,647")]
    [InlineData("System.Int32?", "2147483647")]
    [InlineData("System.Int64?", "-9,223,372,036,854,775,808")]
    [InlineData("System.Int64?", "-9223372036854775808")]
    [InlineData("System.Int64?", "9,223,372,036,854,775,807")]
    [InlineData("System.Int64?", "9223372036854775807")]
    [InlineData("System.SByte?", "-128")]
    [InlineData("System.SByte?", "127")]
    [InlineData("System.Int16?", "-32,768")]
    [InlineData("System.Int16?", "-32768")]
    [InlineData("System.Int16?", "32,767")]
    [InlineData("System.Int16?", "32767")]
    [InlineData("System.String?", "")]
    [InlineData("System.String?", "Hello, World!")]
    [InlineData("System.UInt32?", "0")]
    [InlineData("System.UInt32?", "4,294,967,295")]
    [InlineData("System.UInt32?", "4294967295")]
    [InlineData("System.UInt64?", "0")]
    [InlineData("System.UInt64?", "18,446,744,073,709,551,615")]
    [InlineData("System.UInt64?", "18446744073709551615")]
    [InlineData("System.UInt16?", "0")]
    [InlineData("System.UInt16?", "65,535")]
    [InlineData("System.UInt16?", "65535")]
    public void ClrPrimitiveTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed record MyRecord(
                        [NullString("")] {type} Property,
                    );
                    """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("MyEnum?", "A")]
    public void EnumTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $$"""
                     public enum MyEnum { A, B, C }

                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                        [NullString("")] {{type}} Property,
                     );
                     """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("DateTime?", "2025-05-26 01:05:00.000")]
    public void DateTimeTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed record MyRecord(
                        [DateTimeFormat("yyyy-MM-dd HH:mm:ss.fff")]
                        [NullString("")]
                        {type} Property,
                    );
                    """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("TimeSpan?", "1.02:03:04.5670000")] // 1일 2시간 3분 4.567초
    public void TimeSpanTest(string type, string argument)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<PrimitiveTypeTests>() is not TestOutputLogger<PrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed record MyRecord(
                        [TimeSpanFormat("c")]
                        [NullString("")]
                        {type} Property,
                    );
                    """;

        var catalogs = CreateCatalogs(code, logger);

        var cells = new[]
        {
            new CellData("A1", argument)
        };

        var context = CompatibilityContext.CreateNoCollect(catalogs, cells);

        foreach (var recordSchema in catalogs.RecordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var propertySchema in recordSchema.PropertySchemata)
            {
                propertySchema.CheckCompatibility(context);
            }
        }

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
}
