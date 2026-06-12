using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class SwitchForeignKeySameConditionMultipleAttrTests(ITestOutputHelper testOutputHelper)
{
    private enum Kind
    {
        Item,
        None,
    }

    private const string QuestCsv =
        """
        QuestId,Kind,RewardId
        1,Item,101
        """;

    private const string ItemCsv =
        """
        Id,Name
        101,sword
        """;

    private const string FooCsv =
        """
        Id,Name
        201,foo-a
        """;

#pragma warning disable SDP0208 // SwitchForeignKey duplicate condition value — 의도된 invalid 구조. 런타임 거부 검증.
    [StaticDataRecord("Quest", "Sheet1")]
    private partial record QuestRecord(
        int QuestId,
        Kind Kind,
        [SwitchForeignKey("Kind", "Item", "Item", "Id")]
        [SwitchForeignKey("Kind", "Item", "Foo",  "Id")]
        int RewardId);
#pragma warning restore SDP0208

    [StaticDataRecord("Item", "Sheet1")]
    private partial record ItemRecord(int Id, string Name);

    [StaticDataRecord("Foo", "Sheet1")]
    private partial record FooRecord(int Id, string Name);

    private sealed partial class QuestTable(ImmutableArray<QuestRecord> records)
        : StaticDataTable<QuestTable, QuestRecord>(records);

    private sealed partial class ItemTable(ImmutableArray<ItemRecord> records)
        : StaticDataTable<ItemTable, ItemRecord>(records);

    private sealed partial class FooTable(ImmutableArray<FooRecord> records)
        : StaticDataTable<FooTable, FooRecord>(records);

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            QuestTable? Quest,
            ItemTable? Item,
            FooTable? Foo);

        public QuestTable QuestTable => Current.Quest!;
    }

    private const string QuestTripleCsv =
        """
        QuestId,Kind,RewardId
        1,Item,101
        """;

    private const string BarCsv =
        """
        Id,Name
        301,bar-a
        """;

#pragma warning disable SDP0208 // SwitchForeignKey duplicate condition value — 의도된 invalid 구조. 런타임 거부 검증.
    [StaticDataRecord("QuestTriple", "Sheet1")]
    private partial record QuestTripleRecord(
        int QuestId,
        Kind Kind,
        [SwitchForeignKey("Kind", "Item", "Item", "Id")]
        [SwitchForeignKey("Kind", "Item", "Foo",  "Id")]
        [SwitchForeignKey("Kind", "Item", "Bar",  "Id")]
        int RewardId);
#pragma warning restore SDP0208

    [StaticDataRecord("Bar", "Sheet1")]
    private partial record BarRecord(int Id, string Name);

    private sealed partial class QuestTripleTable(ImmutableArray<QuestTripleRecord> records)
        : StaticDataTable<QuestTripleTable, QuestTripleRecord>(records);

    private sealed partial class BarTable(ImmutableArray<BarRecord> records)
        : StaticDataTable<BarTable, BarRecord>(records);

    private sealed partial class StaticDataTriple(ILogger logger)
        : StaticDataManager<StaticDataTriple.TableSet>(logger)
    {
        public sealed partial record TableSet(
            QuestTripleTable? QuestTriple,
            ItemTable? Item,
            FooTable? Foo,
            BarTable? Bar);

        public QuestTripleTable QuestTripleTable => Current.QuestTriple!;
    }

    // 같은 condition 의 duplicate value 거부는 SG 컴파일타임 진단(SDP0208)으로 검증된다.
    // 런타임에서는 SG-emit ValidateForeignKeys 가 첫 SwitchForeignKey attribute 만 적용하고
    // 동일 condition 의 후속 attribute 는 무시한다. 아래 테스트가 그 동작을 검증한다.
    [Fact]
    public async Task Load_DuplicateSwitchFkCondition_AppliesFirstAttributeOnly()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeySameConditionMultipleAttrTests>() is not TestOutputLogger<SwitchForeignKeySameConditionMultipleAttrTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // RewardId=101 은 첫 attribute 타겟인 Item 에만 있고 둘째 타겟 Foo 에는 없다.
        // 첫 attribute 만 적용되므로 로드가 성공한다.
        using var dir = new CsvTestDirectory();
        dir.Write("Quest.Sheet1.csv", QuestCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Foo.Sheet1.csv", FooCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Single(staticData.QuestTable.Records);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TripleDuplicateSwitchFkCondition_AppliesFirstAttributeOnly()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeySameConditionMultipleAttrTests>() is not TestOutputLogger<SwitchForeignKeySameConditionMultipleAttrTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // RewardId=101 은 첫 attribute 타겟인 Item 에만 있고 Foo/Bar 에는 없다.
        using var dir = new CsvTestDirectory();
        dir.Write("QuestTriple.Sheet1.csv", QuestTripleCsv);
        dir.Write("Item.Sheet1.csv", ItemCsv);
        dir.Write("Foo.Sheet1.csv", FooCsv);
        dir.Write("Bar.Sheet1.csv", BarCsv);

        var staticData = new StaticDataTriple(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Single(staticData.QuestTripleTable.Records);
        Assert.Empty(logger.Logs);
    }
}
