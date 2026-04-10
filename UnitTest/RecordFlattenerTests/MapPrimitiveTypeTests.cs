using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public class MapPrimitiveTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void MapStringToRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapPrimitiveTypeTests>() is not TestOutputLogger<MapPrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            public sealed record UserInfo(
                [Key] string Name,
                int Age,
                string Email
            );

            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [Length(2)] FrozenDictionary<string, UserInfo> Users
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(6, results.Count);
        Assert.Equal("Users[0].Name", results[0]);
        Assert.Equal("Users[0].Age", results[1]);
        Assert.Equal("Users[0].Email", results[2]);
        Assert.Equal("Users[1].Name", results[3]);
        Assert.Equal("Users[1].Age", results[4]);
        Assert.Equal("Users[1].Email", results[5]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapWithInnerRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapPrimitiveTypeTests>() is not TestOutputLogger<MapPrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            public sealed record AddressInfo(
                string City,
                string ZipCode
            );

            public sealed record UserInfo(
                [Key] string Name,
                int Age,
                AddressInfo Address
            );

            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [Length(2)] FrozenDictionary<string, UserInfo> Users
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(8, results.Count);
        Assert.Equal("Users[0].Name", results[0]);
        Assert.Equal("Users[0].Age", results[1]);
        Assert.Equal("Users[0].Address.City", results[2]);
        Assert.Equal("Users[0].Address.ZipCode", results[3]);
        Assert.Equal("Users[1].Name", results[4]);
        Assert.Equal("Users[1].Age", results[5]);
        Assert.Equal("Users[1].Address.City", results[6]);
        Assert.Equal("Users[1].Address.ZipCode", results[7]);

        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapWithColumnNameTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapPrimitiveTypeTests>() is not TestOutputLogger<MapPrimitiveTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            public sealed record UserInfo(
                [Key][ColumnName("UserName")] string Name,
                [ColumnName("UserAge")] int Age
            );

            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [ColumnName("UserData")][Length(2)] FrozenDictionary<string, UserInfo> Users
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(4, results.Count);
        Assert.Contains("UserData[0].UserName", results);
        Assert.Contains("UserData[0].UserAge", results);
        Assert.Contains("UserData[1].UserName", results);
        Assert.Contains("UserData[1].UserAge", results);
        Assert.Empty(logger.Logs);
    }
}
