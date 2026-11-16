using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 같은 Shop 시트의 클라이언트 쪽 열. 두 Record 의 [Key] 는 같은 열(ItemId)이다.
[StaticDataRecord("Rpg", "Shop")]
public sealed partial record ShopClientRecord(
    [Key] int ItemId,
    [RegularExpression(@"^icons/[a-z_]+\.png$")] string IconPath,
    byte DisplayOrder);
