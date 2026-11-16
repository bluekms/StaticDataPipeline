using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// DateOnly 조건 SwitchFK. 조건 값은 DateTimeFormat 으로 파싱되고 오류 메시지에도 그 포맷(Season=2026-07-01)으로 찍힌다. 열은 int(대상 키가 서로 다른 브랜딩).
[StaticDataRecord("Rpg", "SeasonRewards")]
public sealed partial record SeasonRewardRecord(
    [Key] int Id,
    [DateTimeFormat("yyyy-MM-dd")] DateOnly Season,
    [SwitchForeignKey(nameof(Season), "2026-06-01", "Season1ItemTable", "Id")]
    [SwitchForeignKey(nameof(Season), "2026-11-01", "ItemTable", "Id")]
    int ItemId);
