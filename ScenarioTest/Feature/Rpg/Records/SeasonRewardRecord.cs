using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 시즌은 시작일로 구분한다. DateOnly 조건 SwitchFK. 조건 값은 DateTimeFormat 으로 파싱되고 오류 메시지에도 그 포맷(SeasonStartDay=2026-07-01)으로 찍힌다. 열은 int(대상 키가 서로 다른 브랜딩).
[StaticDataRecord("Rpg", "SeasonRewards")]
public sealed partial record SeasonRewardRecord(
    [Key] int Id,

    [DateTimeFormat("yyyy-MM-dd")] DateOnly SeasonStartDay,

    [SwitchForeignKey(nameof(SeasonStartDay), "2026-06-01", "SeasonItemTable", "Id")]
    [SwitchForeignKey(nameof(SeasonStartDay), "2026-11-01", "SeasonItemTableV2", "Id")]
    int ItemId);
