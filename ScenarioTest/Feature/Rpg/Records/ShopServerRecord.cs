using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// Rpg.Shop 시트를 ShopClientRecord 와 나눠 읽는다. 추출 CSV 는 두 Record 열의 합집합이고 각 로더는 자기 열만 읽는다.
// SaleStart 는 오프셋이 든 DateTimeOffset 이라 픽스처에 날짜 셀 대신 텍스트 셀로 넣는다.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopServerRecord(
    [Key] [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [NullString("무제한")] uint? Stock,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss zzz")] DateTimeOffset SaleStart,
    [Length(2)] FrozenDictionary<PriceKey, Price> Prices);
