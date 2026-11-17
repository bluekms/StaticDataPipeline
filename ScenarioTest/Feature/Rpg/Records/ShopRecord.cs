using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 상점. 키가 곧 아이템을 가리키는 FK 다.
// SaleStart 는 오프셋이 든 DateTimeOffset 이라 픽스처에 날짜 셀 대신 텍스트 셀로 넣는다.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopRecord(
    [Key] [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [RegularExpression(@"^icons/[a-z_]+\.png$")] string IconPath,
    byte DisplayOrder,
    [NullString("무제한")] uint? Stock,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss zzz")] DateTimeOffset SaleStart,
    [Length(2)] FrozenDictionary<PriceKey, Price> Prices);
