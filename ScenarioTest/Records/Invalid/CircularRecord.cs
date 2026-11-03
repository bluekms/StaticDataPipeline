using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

public sealed record Mentor(int TeacherId, Mentee Mentee);

public sealed record Mentee(int StudentId, Mentor Mentor);

// 중첩 record 가 서로를 참조하는 순환. 스캐너가 Circular reference 로 거부해야 한다.
[StaticDataRecord("Invalid", "Mentoring")]
public sealed partial record MentoringRecord([Key] int Id, Mentor Mentor);
