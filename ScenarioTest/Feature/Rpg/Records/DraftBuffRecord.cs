using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// [Ignore] 가 붙은 Record 가 헤더 생성, 추출, 로드 어디에도 나타나지 않는지 보는 입력이다. Buffs 시트를 함께 가리킨다.
[Ignore]
[StaticDataRecord("Rpg", "Buffs")]
public sealed partial record DraftBuffRecord(
    [Key] int Id,
    string Name);
