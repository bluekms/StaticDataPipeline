using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.DocSampleTests;

// Docs/ko/03-usage/05-complex-record.md 와 06-static-data-manager.md 의 예제를 검증한다.
// 도큐먼트 예제의 Cooldown(TimeSpan) 필드는 현재 런타임 CsvRecordMapper 가 지원하지 않으므로
// 본 테스트에서는 제외한다 (Convert.ChangeType 이 TimeSpan IConvertible 미지원).
public class ComplexRecordAndManagerSampleTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("ItemCatalog", "Categories")]
    private sealed record ItemCategoryRecord(
        [Key] int Id,
        string Name,
        bool IsConsumable);

    [StaticDataRecord("ItemCatalog", "Items")]
    private sealed record ItemRecord(
        [Key] int Id,
        string Name,
        [ForeignKey("Categories", "Id")] int CategoryId,
        [Range(0, 1_000_000)] int Price,
        [DateTimeFormat("yyyy-MM-dd")] DateTime ReleaseDate,
        [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags,
        [RegularExpression(@"^icons/[a-z]+\.png$")] string IconPath,
        [NullString("NULL")] string? Description);

    private sealed class ItemCategoryTable(ImmutableList<ItemCategoryRecord> records)
        : StaticDataTable<ItemCategoryTable, ItemCategoryRecord>(records);

    private sealed class ItemTable(ImmutableList<ItemRecord> records)
        : StaticDataTable<ItemTable, ItemRecord>(records);

    private sealed class GameStaticData(ILogger logger)
        : StaticDataManager<GameStaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            ItemCategoryTable? Categories,
            ItemTable? Items);

        public ItemCategoryTable Categories => Current.Categories!;
        public ItemTable Items => Current.Items!;
    }

    private const string CategoriesCsv =
        """
        Id,Name,IsConsumable
        1,Weapon,false
        2,Armor,false
        3,Consumable,true
        """;

    private const string ItemsCsv =
        """
        Id,Name,CategoryId,Price,ReleaseDate,Tags,IconPath,Description
        1,Iron Sword,1,5000,2026-01-15,"melee,iron",icons/iron.png,NULL
        2,Steel Sword,1,8000,2026-03-10,"melee,steel",icons/steel.png,Stronger edge
        3,Potion,3,100,2026-01-01,"heal,consumable",icons/potion.png,Heals 50 HP
        """;

    private const string ItemsWithBrokenFkCsv =
        """
        Id,Name,CategoryId,Price,ReleaseDate,Tags,IconPath,Description
        1,Iron Sword,1,5000,2026-01-15,"melee,iron",icons/iron.png,NULL
        2,Mystery,99,5000,2026-03-10,unknown,icons/mystery.png,NULL
        """;

    [Fact]
    public async Task Load_ValidData_PopulatesBothTables()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ComplexRecordAndManagerSampleTests>() is not TestOutputLogger<ComplexRecordAndManagerSampleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ItemCatalog.Categories.csv", CategoriesCsv);
        dir.Write("ItemCatalog.Items.csv", ItemsCsv);

        var game = new GameStaticData(logger);
        await game.LoadAsync(dir.Path);

        Assert.Equal(3, game.Categories.Records.Count);
        Assert.Equal(3, game.Items.Records.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_ParsesDateTimeWithFormat()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ComplexRecordAndManagerSampleTests>() is not TestOutputLogger<ComplexRecordAndManagerSampleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ItemCatalog.Categories.csv", CategoriesCsv);
        dir.Write("ItemCatalog.Items.csv", ItemsCsv);

        var game = new GameStaticData(logger);
        await game.LoadAsync(dir.Path);

        var iron = game.Items.Records.First(x => x.Id == 1);

        Assert.Equal(new DateTime(2026, 1, 15), iron.ReleaseDate);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_SingleColumnCollection_SplitsTags()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ComplexRecordAndManagerSampleTests>() is not TestOutputLogger<ComplexRecordAndManagerSampleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ItemCatalog.Categories.csv", CategoriesCsv);
        dir.Write("ItemCatalog.Items.csv", ItemsCsv);

        var game = new GameStaticData(logger);
        await game.LoadAsync(dir.Path);

        var iron = game.Items.Records.First(x => x.Id == 1);

        Assert.Equal(2, iron.Tags.Length);
        Assert.Equal("melee", iron.Tags[0]);
        Assert.Equal("iron", iron.Tags[1]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_NullStringMapsToNull()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ComplexRecordAndManagerSampleTests>() is not TestOutputLogger<ComplexRecordAndManagerSampleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ItemCatalog.Categories.csv", CategoriesCsv);
        dir.Write("ItemCatalog.Items.csv", ItemsCsv);

        var game = new GameStaticData(logger);
        await game.LoadAsync(dir.Path);

        var iron = game.Items.Records.First(x => x.Id == 1);
        var steel = game.Items.Records.First(x => x.Id == 2);

        Assert.Null(iron.Description);
        Assert.Equal("Stronger edge", steel.Description);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_BrokenForeignKey_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ComplexRecordAndManagerSampleTests>() is not TestOutputLogger<ComplexRecordAndManagerSampleTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("ItemCatalog.Categories.csv", CategoriesCsv);
        dir.Write("ItemCatalog.Items.csv", ItemsWithBrokenFkCsv);

        var game = new GameStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => game.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("99", ex.InnerExceptions[0].Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }
}
