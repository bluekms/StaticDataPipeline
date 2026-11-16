using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// SeasonRewards 의 과거 시즌 대상. Id 대역을 ItemTable(10000대)과 겹치지 않게 20000대로 둔다.
[StaticDataRecord("Rpg", "Season1Items")]
public sealed partial record Season1ItemRecord(
    [Key] Season1ItemRecord.Season1ItemId Id,
    string Name)
{
    public record struct Season1ItemId([Range(20000, 20999)] int Value);
}
