using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [ColumnName] 의 쉼표는 CSV 헤더 구분자와 겹친다.
[StaticDataRecord("Invalid", "Teachers")]
public sealed partial record TeacherRecord([Key] int Id, [ColumnName("a,b")] string Name);
