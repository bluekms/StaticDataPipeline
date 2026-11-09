using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

public sealed record Mentor(int TeacherId, Mentee Mentee);

public sealed record Mentee(int StudentId, Mentor Mentor);

// 중첩 record 의 순환 참조. 열을 펼치는 일이 끝나지 않으므로 선언 단계에서 막힌다.
[StaticDataRecord("Invalid", "Mentoring")]
public sealed partial record MentoringRecord([Key] int Id, Mentor Mentor);
