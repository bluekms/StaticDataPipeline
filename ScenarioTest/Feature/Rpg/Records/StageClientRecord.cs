using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// Stages 시트의 클라이언트 쪽 열. 화면에 쓰는 값이다. 추출 CSV 는 두 Record 열의 합집합이고 각 로더는 자기 열만 읽는다.
[StaticDataRecord("Rpg", "Stages")]
public sealed partial record StageClientRecord(
    [Key] StageClientRecord.StageId Id,
    string Name,
    [Range(1, 99)] byte RequiredLevel,
    [NullString("무제한")] uint? DailyLimit,
    [RegularExpression(@"^scenes/[a-z_]+\.unity$")] string ScenePath,
    [NullString("")] string? Bgm,
    [ForeignKey("MonsterTable", "Id")] MonsterId Boss)
{
    // 서버의 StageId 와 같은 열을 읽지만, 실제 클라이언트는 서버 어셈블리의 타입을 참조할 수 없어 따로 둔다.
    public record struct StageId(int Value);
}
