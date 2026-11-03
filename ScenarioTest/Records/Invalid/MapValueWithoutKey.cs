using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

public enum SubjectCode
{
    Korean,
    Math,
}

public sealed record SubjectScore(SubjectCode Subject, int Score);

// Map Value record 에 [Key] 가 없음. FrozenDictionary 의 키를 정할 수 없어 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Grades")]
public sealed partial record GradeRecord(
    [Key] int StudentId,
    [Length(2)] FrozenDictionary<SubjectCode, SubjectScore> Scores);
