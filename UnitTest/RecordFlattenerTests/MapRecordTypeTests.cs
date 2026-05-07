using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordFlattenerTests;

public class MapRecordTypeTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void MapRecordToRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapRecordTypeTests>() is not TestOutputLogger<MapRecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ItemKey(
                       int Id,
                       string Type
                   );

                   public sealed record ItemStatus(
                       [Key] ItemKey Key,
                       int Level,
                       int Power
                   );

                   [StaticDataRecord("GameData", "Items")]
                   public sealed record ItemCollection(
                       [Length(2)] FrozenDictionary<ItemKey, ItemStatus> Inventory
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(8, results.Count);
        Assert.Equal("Inventory[0].Key.Id", results[0]);
        Assert.Equal("Inventory[0].Key.Type", results[1]);
        Assert.Equal("Inventory[0].Level", results[2]);
        Assert.Equal("Inventory[0].Power", results[3]);
        Assert.Equal("Inventory[1].Key.Id", results[4]);
        Assert.Equal("Inventory[1].Key.Type", results[5]);
        Assert.Equal("Inventory[1].Level", results[6]);
        Assert.Equal("Inventory[1].Power", results[7]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapNestedRecordToRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapRecordTypeTests>() is not TestOutputLogger<MapRecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ItemKey(
                       int Id,
                       string Type
                   );

                   public sealed record StatInfo(
                       int Attack,
                       int Defense
                   );

                   public sealed record ItemStatus(
                       [Key] ItemKey Key,
                       int Level,
                       StatInfo Stats
                   );

                   [StaticDataRecord("GameData", "Items")]
                   public sealed record ItemCollection(
                       [Length(2)] FrozenDictionary<ItemKey, ItemStatus> Inventory
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(10, results.Count);
        Assert.Equal("Inventory[0].Key.Id", results[0]);
        Assert.Equal("Inventory[0].Key.Type", results[1]);
        Assert.Equal("Inventory[0].Level", results[2]);
        Assert.Equal("Inventory[0].Stats.Attack", results[3]);
        Assert.Equal("Inventory[0].Stats.Defense", results[4]);
        Assert.Equal("Inventory[1].Key.Id", results[5]);
        Assert.Equal("Inventory[1].Key.Type", results[6]);
        Assert.Equal("Inventory[1].Level", results[7]);
        Assert.Equal("Inventory[1].Stats.Attack", results[8]);
        Assert.Equal("Inventory[1].Stats.Defense", results[9]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapNestedRecordToRecordFlattenTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapRecordTypeTests>() is not TestOutputLogger<MapRecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record ItemKey(
                       int Id,
                       string Type
                   );

                   public sealed record ItemStatus(
                       [Key] ItemKey Key,
                       int Level,
                       ItemStatus.StatInfo Stats
                   )
                   {
                       public sealed record StatInfo(int Attack, int Defense);
                   }

                   [StaticDataRecord("GameData", "Items")]
                   public sealed record ItemCollection(
                       [Length(2)] FrozenDictionary<ItemKey, ItemStatus> Inventory
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(10, results.Count);
        Assert.Equal("Inventory[0].Key.Id", results[0]);
        Assert.Equal("Inventory[0].Key.Type", results[1]);
        Assert.Equal("Inventory[0].Level", results[2]);
        Assert.Equal("Inventory[0].Stats.Attack", results[3]);
        Assert.Equal("Inventory[0].Stats.Defense", results[4]);
        Assert.Equal("Inventory[1].Key.Id", results[5]);
        Assert.Equal("Inventory[1].Key.Type", results[6]);
        Assert.Equal("Inventory[1].Level", results[7]);
        Assert.Equal("Inventory[1].Stats.Attack", results[8]);
        Assert.Equal("Inventory[1].Stats.Defense", results[9]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapRecordWithArrayToRecordTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapRecordTypeTests>() is not TestOutputLogger<MapRecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record KeyRecord(
                       int Id,
                       [Length(3)] ImmutableArray<string> Tags
                   );

                   public sealed record ValueRecord(
                       [Key] KeyRecord Name,
                       int Score
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2)] FrozenDictionary<KeyRecord, ValueRecord> Data
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(10, results.Count);
        Assert.Equal("Data[0].Name.Id", results[0]);
        Assert.Equal("Data[0].Name.Tags[0]", results[1]);
        Assert.Equal("Data[0].Name.Tags[1]", results[2]);
        Assert.Equal("Data[0].Name.Tags[2]", results[3]);
        Assert.Equal("Data[0].Score", results[4]);
        Assert.Equal("Data[1].Name.Id", results[5]);
        Assert.Equal("Data[1].Name.Tags[0]", results[6]);
        Assert.Equal("Data[1].Name.Tags[1]", results[7]);
        Assert.Equal("Data[1].Name.Tags[2]", results[8]);
        Assert.Equal("Data[1].Score", results[9]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MapRecordWithArrayToRecordWithColumnNameTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapRecordTypeTests>() is not TestOutputLogger<MapRecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record KeyRecord(
                       [ColumnName("ID")] int Id,
                       [Length(3), ColumnName("Tag")] ImmutableArray<string> Tags
                   );

                   public sealed record ValueRecord(
                       [Key] KeyRecord Name,
                       [ColumnName("Point")] int Score
                   );

                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed record MyRecord(
                       [Length(2), ColumnName("Inven")] FrozenDictionary<KeyRecord, ValueRecord> Data
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(10, results.Count);
        Assert.Equal("Inven[0].Name.ID", results[0]);
        Assert.Equal("Inven[0].Name.Tag[0]", results[1]);
        Assert.Equal("Inven[0].Name.Tag[1]", results[2]);
        Assert.Equal("Inven[0].Name.Tag[2]", results[3]);
        Assert.Equal("Inven[0].Point", results[4]);
        Assert.Equal("Inven[1].Name.ID", results[5]);
        Assert.Equal("Inven[1].Name.Tag[0]", results[6]);
        Assert.Equal("Inven[1].Name.Tag[1]", results[7]);
        Assert.Equal("Inven[1].Name.Tag[2]", results[8]);
        Assert.Equal("Inven[1].Point", results[9]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void ComplexNestedTypeCollectionFlattenTest()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MapRecordTypeTests>() is not TestOutputLogger<MapRecordTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   public sealed record UserKey(
                       [ColumnName("UID")] int UserId,
                       [Length(2), ColumnName("TR")] ImmutableArray<UserKey.TraitInfo> Traits
                   )
                   {
                       public sealed record TraitInfo([ColumnName("Tid")] int TraitId, int Level);
                   }

                   public sealed record UserProfile(
                       [Key] UserKey Key,
                       [Length(2), ColumnName("SK")] FrozenSet<UserProfile.SkillInfo> Skills,
                       int Rank
                   )
                   {
                       public sealed record SkillInfo([ColumnName("Sid")] string SkillId, bool IsActive);
                   }

                   [StaticDataRecord("GameData", "Users")]
                   public sealed record UserDataCatalog(
                       [Length(2), ColumnName("Player")] FrozenDictionary<UserKey, UserProfile> Data
                   );
                   """;

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        Assert.Equal(20, results.Count);
        Assert.Equal("Player[0].Key.UID", results[0]);
        Assert.Equal("Player[0].Key.TR[0].Tid", results[1]);
        Assert.Equal("Player[0].Key.TR[0].Level", results[2]);
        Assert.Equal("Player[0].Key.TR[1].Tid", results[3]);
        Assert.Equal("Player[0].Key.TR[1].Level", results[4]);
        Assert.Equal("Player[0].SK[0].Sid", results[5]);
        Assert.Equal("Player[0].SK[0].IsActive", results[6]);
        Assert.Equal("Player[0].SK[1].Sid", results[7]);
        Assert.Equal("Player[0].SK[1].IsActive", results[8]);
        Assert.Equal("Player[0].Rank", results[9]);
        Assert.Equal("Player[1].Key.UID", results[10]);
        Assert.Equal("Player[1].Key.TR[0].Tid", results[11]);
        Assert.Equal("Player[1].Key.TR[0].Level", results[12]);
        Assert.Equal("Player[1].Key.TR[1].Tid", results[13]);
        Assert.Equal("Player[1].Key.TR[1].Level", results[14]);
        Assert.Equal("Player[1].SK[0].Sid", results[15]);
        Assert.Equal("Player[1].SK[0].IsActive", results[16]);
        Assert.Equal("Player[1].SK[1].Sid", results[17]);
        Assert.Equal("Player[1].SK[1].IsActive", results[18]);
        Assert.Equal("Player[1].Rank", results[19]);
        Assert.Empty(logger.Logs);
    }
}
