using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// DayOfWeek 는 BCL 에 있지만 이 파일에 using System 이 없다. 프로젝트의 ImplicitUsings 덕에 빌드는 되어도 추출기는 이 타입을 찾지 못한다.
[StaticDataRecord("Invalid", "Clubs")]
public sealed partial record ClubMeetingRecord(
    [Key] int Id,
    [SingleColumnCollection(",")] ImmutableArray<DayOfWeek> MeetingDays);
