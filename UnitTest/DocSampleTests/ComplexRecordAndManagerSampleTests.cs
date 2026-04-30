using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Manager;
using Sdp.Table;

namespace UnitTest.DocSampleTests;

// Docs/ko/03-usage/05-complex-record.md 와 06-static-data-manager.md 의 예제를 검증한다.
// 도큐먼트 예제의 Cooldown(TimeSpan) 필드는 현재 런타임 CsvRecordMapper 가 지원하지 않으므로
// 본 테스트에서는 제외한다 (Convert.ChangeType 이 TimeSpan IConvertible 미지원).
public class ComplexRecordAndManagerSampleTests
{
    [StaticDataRecord("ItemCatalog", "Categories")]
    private sealed record ItemCategory(
        [Key] int Id,
        string Name,
        bool IsConsumable);

    [StaticDataRecord("ItemCatalog", "Items")]
    private sealed record Item(
        [Key] int Id,
        string Name,
        [ForeignKey("Categories", "Id")] int CategoryId,
        [Range(0, 1_000_000)] int Price,
        [DateTimeFormat("yyyy-MM-dd")] DateTime ReleaseDate,
        [SingleColumnCollection(",")][CountRange(1, 5)] ImmutableArray<string> Tags,
        [RegularExpression(@"^icons/[a-z]+\.png$")] string IconPath,
        [NullString("NULL")] string? Description);

    private sealed class ItemCategoryTable(ImmutableList<ItemCategory> records)
        : StaticDataTable<ItemCategoryTable, ItemCategory>(records);

    private sealed class ItemTable(ImmutableList<Item> records)
        : StaticDataTable<ItemTable, Item>(records);

    private sealed class GameStaticData : StaticDataManager<GameStaticData.TableSet>
    {
        public sealed record TableSet(
            ItemCategoryTable? Categories,
            ItemTable? Items);

        public ItemCategoryTable Categories => Current.Categories!;
        public ItemTable Items => Current.Items!;

        public void LoadFromCsv(string categoriesCsv, string itemsCsv)
            => Load(new TableSet(
                new ItemCategoryTable(CsvLoader.Parse<ItemCategory>(categoriesCsv)),
                new ItemTable(CsvLoader.Parse<Item>(itemsCsv))));
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
    public void Load_ValidData_PopulatesBothTables()
    {
        var game = new GameStaticData();

        game.LoadFromCsv(CategoriesCsv, ItemsCsv);

        Assert.Equal(3, game.Categories.Records.Count);
        Assert.Equal(3, game.Items.Records.Count);
    }

    [Fact]
    public void Load_ParsesDateTimeWithFormat()
    {
        var game = new GameStaticData();
        game.LoadFromCsv(CategoriesCsv, ItemsCsv);

        var iron = game.Items.Records.First(x => x.Id == 1);

        Assert.Equal(new DateTime(2026, 1, 15), iron.ReleaseDate);
    }

    [Fact]
    public void Load_SingleColumnCollection_SplitsTags()
    {
        var game = new GameStaticData();
        game.LoadFromCsv(CategoriesCsv, ItemsCsv);

        var iron = game.Items.Records.First(x => x.Id == 1);

        Assert.Equal(2, iron.Tags.Length);
        Assert.Equal("melee", iron.Tags[0]);
        Assert.Equal("iron", iron.Tags[1]);
    }

    [Fact]
    public void Load_NullStringMapsToNull()
    {
        var game = new GameStaticData();
        game.LoadFromCsv(CategoriesCsv, ItemsCsv);

        var iron = game.Items.Records.First(x => x.Id == 1);
        var steel = game.Items.Records.First(x => x.Id == 2);

        Assert.Null(iron.Description);
        Assert.Equal("Stronger edge", steel.Description);
    }

    [Fact]
    public void Load_BrokenForeignKey_ThrowsAggregateException()
    {
        var game = new GameStaticData();

        var ex = Assert.Throws<AggregateException>(
            () => game.LoadFromCsv(CategoriesCsv, ItemsWithBrokenFkCsv));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("99", ex.InnerExceptions[0].Message);
    }
}
