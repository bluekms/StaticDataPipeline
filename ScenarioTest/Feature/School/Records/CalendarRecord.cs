using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 학사 일정. CalendarTable.Validate 가 종료일이 시작일보다 앞서는 행을 잡는다.
[StaticDataRecord("School", "Calendar")]
public sealed partial record CalendarRecord(
    [Key] int Id,

    string Title,

    [DateTimeFormat("yyyy-MM-dd")]
    [Range(typeof(DateOnly), "2026-03-01", "2027-02-28")]
    DateOnly Start,

    [DateTimeFormat("yyyy-MM-dd")]
    [NullString("")]
    DateOnly? End,

    bool IsHoliday);
