using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public class ArrayTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void ArrayPrimitiveTypeTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [Length(3)] ImmutableArray<int> Scores
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(3, results.Count);
        Assert.Equal("Scores[0]", results[0]);
        Assert.Equal("Scores[1]", results[1]);
        Assert.Equal("Scores[2]", results[2]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ArrayWithColumnNameTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [ColumnName("Score")][Length(3)] ImmutableArray<int> Scores
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(3, results.Count);
        Assert.Equal("Score[0]", results[0]);
        Assert.Equal("Score[1]", results[1]);
        Assert.Equal("Score[2]", results[2]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ArrayWithSingleColumnCollectionTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [SingleColumnCollection(",")][Length(3)] ImmutableArray<int> Scores
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Single(results);
        Assert.Equal("Scores", results[0]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MultipleArraysTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                string Name,
                [Length(2)] ImmutableArray<int> Scores,
                [Length(3)] ImmutableArray<string> Tags
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(6, results.Count);
        Assert.Equal("Name", results[0]);
        Assert.Equal("Scores[0]", results[1]);
        Assert.Equal("Scores[1]", results[2]);
        Assert.Equal("Tags[0]", results[3]);
        Assert.Equal("Tags[1]", results[4]);
        Assert.Equal("Tags[2]", results[5]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ArrayRecordTypeFlattenTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ArrayTypeTests>() is not TestOutputLogger<ArrayTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
            public sealed record Character(
                [ColumnName("Name")] string Nickname,
                Character.Stat Info
            )
            {
                public sealed record Stat(int Hp, int Mp);
            }

            [StaticDataRecord("Test", "TestSheet")]
            public sealed record MyRecord(
                [Length(2), ColumnName("Hero")] ImmutableArray<Character> Party
            );
            """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(6, results.Count);
        Assert.Equal("Hero[0].Name", results[0]);
        Assert.Equal("Hero[0].Info.Hp", results[1]);
        Assert.Equal("Hero[0].Info.Mp", results[2]);
        Assert.Equal("Hero[1].Name", results[3]);
        Assert.Equal("Hero[1].Info.Hp", results[4]);
        Assert.Equal("Hero[1].Info.Mp", results[5]);
        Assert.Empty(logger.Logs);
    }
}
