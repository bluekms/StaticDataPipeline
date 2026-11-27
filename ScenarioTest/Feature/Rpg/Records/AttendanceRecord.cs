using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 날마다 아이템이나 재화 가운데 하나를 준다. 두 대상의 키가 서로 다른 브랜딩(ItemId, CurrencyId)이라 열은 int 로 둔다.
[StaticDataRecord("Rpg", "Attendance")]
public sealed partial record AttendanceRecord(
    [Key] [Range(1, 7)] byte Day,

    RewardKind Kind,

    [SwitchForeignKey(nameof(Kind), "Item", "ItemTable", "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int RewardId,

    [Range(1, 9999)] uint Amount);
