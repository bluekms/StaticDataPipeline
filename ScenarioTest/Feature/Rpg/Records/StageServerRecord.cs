using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// Stages 시트의 서버 쪽 열. 검증에 쓰는 값이고 MinClearSeconds 는 클라이언트에 주지 않는다.
// Id, RequiredLevel, DailyLimit 은 StageClientRecord 와 함께 읽는다.
[StaticDataRecord("Rpg", "Stages")]
public sealed partial record StageServerRecord(
    [Key] StageServerRecord.StageId Id,
    [Range(1, 99)] byte RequiredLevel,
    [NullString("무제한")] uint? DailyLimit,
    [ForeignKey("MonsterTable", "Id")] MonsterId Boss,
    [Range(1, 3600)] ushort MinClearSeconds)
{
    public record struct StageId(int Value);
}
