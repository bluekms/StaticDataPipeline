using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 성적. 학생 6명 중 4명만 있어 ReportCardView 의 Grade 가 nullable 이 된다.
[StaticDataRecord("School", "Grades")]
public sealed partial record GradeRecord(
    [Key] [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId StudentId,
    [Length(5)] FrozenDictionary<SubjectCode, SubjectScore> Scores,
    [ColumnName("출석일수(3~7월)")]
    [SingleColumnCollection(",")]
    [NullString("x")]
    [CountRange(5, 5)]
    ImmutableArray<byte?> MonthlyAttendance);
