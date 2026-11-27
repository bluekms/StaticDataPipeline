using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [ForeignKey] 는 한 열에 하나만 붙일 수 있다. 대상이 여럿이면 구분 열을 두고 [SwitchForeignKey] 를 쓴다.
[StaticDataRecord("Invalid", "Attendance")]
public sealed partial record AttendanceRecord(
    [Key] byte Day,
    [ForeignKey("ItemTable", "Id")]
    [ForeignKey("CurrencyTable", "Id")]
    int RewardId);
