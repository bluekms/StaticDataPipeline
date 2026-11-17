using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// SeasonRewards 의 새 시즌 대상. 시트 이름은 SeasonItems 이고 코드에서는 V2 로 더했다.
[StaticDataRecord("Rpg", "SeasonItems")]
public sealed partial record SeasonItemRecordV2(
    [Key] SeasonItemRecordV2.SeasonItemId Id,
    string Name,
    Rarity Rarity)
{
    public record struct SeasonItemId([Range(30000, 30999)] int Value);
}
