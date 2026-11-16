using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 교사. TeacherScheduleView 가 수업 없는 교사를 잡는 입력이다.
[StaticDataRecord("School", "Teachers")]
public sealed partial record TeacherRecord(
    [Key] TeacherRecord.TeacherId Id,
    string Name,
    SubjectCode MainSubject,
    [NullString("")] string? Office,
    [SingleColumnCollection(",")] FrozenSet<string> Certificates)
{
    public record struct TeacherId(int Value);
}
