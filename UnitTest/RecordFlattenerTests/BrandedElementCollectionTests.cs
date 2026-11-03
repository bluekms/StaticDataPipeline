using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public partial class BrandedElementCollectionTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void BrandedElementArray_FoldsToIndexedHeaders()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record ItemId(int Value);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord([Length(2)] ImmutableArray<ItemId> Ids);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.FlattenHeaders(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        Assert.Equal(["Ids[0]", "Ids[1]"], results.Select(x => x.Name));
        Assert.Equal(["Ids[0].Value", "Ids[1].Value"], results.Select(x => x.ExpandedName));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SingleValueElementWithOtherParameterName_ExpandsToThatName()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record Code(string Text);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(Code Main, [Length(2)] ImmutableArray<Code> Codes);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.FlattenHeaders(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        Assert.Equal(["Main", "Codes[0]", "Codes[1]"], results.Select(x => x.Name));
        Assert.Equal(["Main.Text", "Codes[0].Text", "Codes[1].Text"], results.Select(x => x.ExpandedName));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void NestedBrandedElementSet_FoldsToIndexedHeaders()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record ItemRecord([Key] ItemRecord.ItemId Id, string Name)
            {
                public readonly record struct ItemId(int Value);
            }

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord([Length(2)] FrozenSet<ItemRecord.ItemId> PickupItemIds);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.FlattenHeaders(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        Assert.Equal(["PickupItemIds[0]", "PickupItemIds[1]"], results.Select(x => x.Name));
        Assert.Equal(["PickupItemIds[0].Value", "PickupItemIds[1].Value"], results.Select(x => x.ExpandedName));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void BrandedElementWithDateTimeFormat_KeepsFormat()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record Day([DateTimeFormat("yyyy-MM-dd")] DateTime Value);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord([Length(1)] ImmutableArray<Day> Days);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.FlattenHeaders(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        var header = Assert.Single(results);
        Assert.Equal("Days[0]", header.Name);
        Assert.Equal("yyyy-MM-dd", header.DateTimeFormat);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MultiParameterRecordElement_IsNotFolded()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record Drop(int ItemId, int Rate);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord([Length(2)] ImmutableArray<Drop> Drops);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.FlattenHeaders(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        Assert.Equal(["Drops[0].ItemId", "Drops[0].Rate", "Drops[1].ItemId", "Drops[1].Rate"], results.Select(x => x.Name));
        Assert.All(results, x => Assert.Null(x.ExpandedName));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapWithBrandedKey_FoldsOnlyKeyParameter()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record ItemId(int Value);

            public sealed partial record Drop([Key] ItemId Id, int Rate);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord([Length(2)] FrozenDictionary<ItemId, Drop> Drops);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.FlattenHeaders(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        Assert.Equal(["Drops[0].Id", "Drops[0].Rate", "Drops[1].Id", "Drops[1].Rate"], results.Select(x => x.Name));
        Assert.Equal(["Drops[0].Id.Value", null, "Drops[1].Id.Value", null], results.Select(x => x.ExpandedName));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapWithSingleValueRecordValue_FoldsToIndexedHeaders()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record Tag([Key] int Id);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord([Length(2)] FrozenDictionary<int, Tag> Tags);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.FlattenHeaders(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger);

        Assert.Equal(["Tags[0]", "Tags[1]"], results.Select(x => x.Name));
        Assert.Equal(["Tags[0].Id", "Tags[1].Id"], results.Select(x => x.ExpandedName));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void WithSheet_MapSingleValueRecordValue_CountsBothHeaderForms()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record Tag([Key] int Id);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(int Id, FrozenDictionary<int, Tag> Tags);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);
        string[] sheetHeaders = ["Id", "Tags[0]", "Tags[1].Id"];

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders);

        Assert.Equal(["Id", "Tags[0]", "Tags[1]"], results);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void WithSheet_BrandedElement_CountsBothHeaderForms()
    {
        var logger = CreateLogger();

        // language=C#
        const string code = """
            public sealed partial record ItemId(int Value);

            [StaticDataRecord("Test", "TestSheet")]
            public sealed partial record MyRecord(int Id, ImmutableArray<ItemId> Ids);
            """;
        var parseResult = SimpleCordParser.Parse(code, logger);
        string[] sheetHeaders = ["Id", "Ids[0]", "Ids[1].Value"];

        var results = RecordFlattener.Flatten(parseResult.RawRecordSchemata[0], parseResult.RecordSchemaCatalog, logger, sheetHeaders);

        Assert.Equal(["Id", "Ids[0]", "Ids[1]"], results);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<BrandedElementCollectionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<BrandedElementCollectionTests>() is not TestOutputLogger<BrandedElementCollectionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
