using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// DateOnly 는 [DateTimeFormat] 없이는 셀 문자열을 읽을 형식을 정할 수 없다.
[StaticDataRecord("Invalid", "Holidays")]
public sealed partial record HolidayRecord([Key] int Id, DateOnly Day);
