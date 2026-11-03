using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// nullable 파라미터에 [NullString] 이 없음. 어떤 셀이 null 인지 정할 수 없어 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Students")]
public sealed partial record StudentRecord([Key] int Id, string Name, string? Nickname);
