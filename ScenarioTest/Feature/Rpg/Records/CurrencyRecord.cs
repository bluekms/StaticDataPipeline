using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 재화. SwitchFK 와 다중 FK 의 두 번째 대상이다.
[StaticDataRecord("Rpg", "Currencies")]
public sealed partial record CurrencyRecord(
    [Key] CurrencyRecord.CurrencyId Id,
    string Name,
    long Cap)
{
    public record struct CurrencyId([Range(900, 999)] int Value);
}
