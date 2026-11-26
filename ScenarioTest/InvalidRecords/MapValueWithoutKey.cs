using System.Collections.Frozen;
using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

public enum SubjectCode
{
    Korean,
    Math,
}

public sealed record SubjectScore(SubjectCode Subject, int Score);

// Map 의 Value record 에 [Key] 가 없으면 FrozenDictionary 의 키를 정할 수 없다.
[StaticDataRecord("Invalid", "Grades")]
public sealed partial record GradeRecord(
    [Key] int StudentId,
    [Length(2)] FrozenDictionary<SubjectCode, SubjectScore> Scores);
