using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// Stages 시트의 클라이언트 쪽 열. 화면에 쓰는 값이다. 추출 CSV 는 두 Record 열의 합집합이고 각 로더는 자기 열만 읽는다.
[StaticDataRecord("Rpg", "Stages")]
public sealed partial record StageClientRecord(
    [Key] StageServerRecord.StageId Id,
    string Name,
    [Range(1, 99)] byte RequiredLevel,
    [NullString("무제한")] uint? DailyLimit,
    [RegularExpression(@"^scenes/[a-z_]+\.unity$")] string ScenePath,
    [NullString("")] string? Bgm);
