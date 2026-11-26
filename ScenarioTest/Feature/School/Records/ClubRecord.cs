using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 동아리. StudentRecord.ClubId 의 FK 대상. MeetingTime 은 샘플 엑셀에 Excel 시간 셀과 빈 셀이 섞여 있다.
[StaticDataRecord("School", "Clubs")]
public sealed partial record ClubRecord(
    [Key] int Id,

    string Name,

    [SingleColumnCollection(" / ")]
    [CountRange(1, 3)]
    ImmutableArray<System.DayOfWeek> MeetingDays,

    [NullString("미정")] bool? RequiresAudition,

    [NullString("")]
    [DateTimeFormat("HH:mm")]
    TimeOnly? MeetingTime);
