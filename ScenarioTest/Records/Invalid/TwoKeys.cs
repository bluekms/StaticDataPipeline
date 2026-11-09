using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [Key] 는 하나. 복합 키는 중첩 record 로 표현한다.
[StaticDataRecord("Invalid", "Students")]
public sealed partial record StudentRecord([Key] int Id, [Key] string Name, int ClassRoomId);
