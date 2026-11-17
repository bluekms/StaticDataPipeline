using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 대상 키가 서로 다른 브랜딩(ItemId, CurrencyId)이라 열은 int 로 두고 안쪽 값으로 비교한다.
[StaticDataRecord("Rpg", "Attendance")]
public sealed partial record AttendanceRecord(
    [Key] [Range(1, 7)] byte Day,

    [ForeignKey("ItemTable", "Id")]
    [ForeignKey("CurrencyTable", "Id")]
    int RewardId,

    [Range(1, 9999)] uint Amount);
