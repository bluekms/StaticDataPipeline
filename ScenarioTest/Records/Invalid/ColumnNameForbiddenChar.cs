using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [ColumnName] 에 쉼표. CSV 헤더에 쓸 수 없는 문자를 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Teachers")]
public sealed partial record TeacherRecord([Key] int Id, [ColumnName("a,b")] string Name);
