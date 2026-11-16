using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 아이템. TableSet 에서 유일한 non-nullable 테이블이고 FK 대상이 가장 많다.
// Tags 는 [Length] 없이 시트의 열 수(Tags[0..2])로 길이가 정해지고, [Ignore] SortCache 는 시트에 열이 없다.
[StaticDataRecord("Rpg", "Items")]
public sealed partial record ItemRecord(
    [Key] ItemRecord.ItemId Id,
    string Name,
    string Description,
    [Range(typeof(Rarity), "Common", "Epic")] Rarity Rarity,
    [NullString("")] EquipSlot? Slot,
    [Range(0, 1_000_000)] uint Price,
    [NullString("-")] ushort? MaxStack,
    Stats Stats,
    ImmutableArray<string> Tags,
    [Ignore] int SortCache)
{
    public record struct ItemId([Range(10000, 19999)] int Value);
}
