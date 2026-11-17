using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// SeasonRewards 의 옛 시즌 대상. 기획 쪽이 시트 이름을 OldSeasonItems 로 바꿨고 코드 이름은 그대로다.
[StaticDataRecord("Rpg", "OldSeasonItems")]
public sealed partial record SeasonItemRecord(
    [Key] SeasonItemRecord.SeasonItemId Id,
    string Name)
{
    public record struct SeasonItemId([Range(20000, 20999)] int Value);
}
