using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// ItemIds 는 같은 아이템을 두 번 적어 두 개를 줄 수 있도록 집합이 아니라 배열로 받는다.
// SaleStart 는 오프셋이 든 DateTimeOffset 이라 샘플 엑셀에 날짜 셀 대신 텍스트 셀로 넣는다.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopRecord(
    [Key] ShopRecord.ShopItemId Id,

    [Length(2)]
    [ForeignKey("ItemTable", "Id")]
    ImmutableArray<ItemRecord.ItemId> ItemIds,

    [RegularExpression(@"^icons/[a-z_]+\.png$")] string IconPath,

    byte DisplayOrder,

    LimitKind LimitKind,

    uint LimitCount,

    [DateTimeFormat("yyyy-MM-dd HH:mm:ss zzz")] DateTimeOffset SaleStart,

    [NullString("상시")]
    [TimeSpanFormat("d'일'")]
    TimeSpan? SaleDuration,

    [Length(2)] FrozenDictionary<PriceKey, Price> Prices)
{
    public record struct ShopItemId(int Value);
}
