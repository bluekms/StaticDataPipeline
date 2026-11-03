using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// DateOnly 에 [DateTimeFormat] 이 없음. 셀 문자열을 어떤 형식으로 읽을지 정할 수 없어 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Holidays")]
public sealed partial record HolidayRecord([Key] int Id, DateOnly Day);
