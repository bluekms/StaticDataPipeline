using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// nullable 파라미터는 [NullString] 으로 어떤 셀이 null 인지 정해야 한다.
[StaticDataRecord("Invalid", "Students")]
public sealed partial record StudentRecord([Key] int Id, string Name, string? Nickname);
