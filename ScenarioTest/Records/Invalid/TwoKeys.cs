using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [Key] 가 둘. 복합 키는 중첩 record 로 표현해야 하므로 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Students")]
public sealed partial record StudentRecord([Key] int Id, [Key] string Name, int ClassRoomId);
