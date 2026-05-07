using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public class SetTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void SetPrimitiveTypeTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(3)] FrozenSet<int> Ids
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(3, results.Count);
        Assert.Equal("Ids[0]", results[0]);
        Assert.Equal("Ids[1]", results[1]);
        Assert.Equal("Ids[2]", results[2]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SetWithColumnNameTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [ColumnName("UserId")][Length(2)] FrozenSet<int> Ids
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(2, results.Count);
        Assert.Equal("UserId[0]", results[0]);
        Assert.Equal("UserId[1]", results[1]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SetWithSingleColumnCollectionTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [SingleColumnCollection(",")][Length(3)] FrozenSet<string> Tags
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Single(results);
        Assert.Equal("Tags", results[0]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MultipleSetsTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       string Name,
                       [Length(2)] FrozenSet<int> Ids,
                       [Length(3)] FrozenSet<string> Tags
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(6, results.Count);
        Assert.Equal("Name", results[0]);
        Assert.Equal("Ids[0]", results[1]);
        Assert.Equal("Ids[1]", results[2]);
        Assert.Equal("Tags[0]", results[3]);
        Assert.Equal("Tags[1]", results[4]);
        Assert.Equal("Tags[2]", results[5]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SetRecordTypeFlattenTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SetTypeTests>() is not TestOutputLogger<SetTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ItemInfo(
                       [ColumnName("ID")] int ItemId,
                       int Count
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2), ColumnName("Inven")] FrozenSet<ItemInfo> Items
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(4, results.Count);
        Assert.Equal("Inven[0].ID", results[0]);
        Assert.Equal("Inven[0].Count", results[1]);
        Assert.Equal("Inven[1].ID", results[2]);
        Assert.Equal("Inven[1].Count", results[3]);

        Assert.Empty(logger.Logs);
    }
}
