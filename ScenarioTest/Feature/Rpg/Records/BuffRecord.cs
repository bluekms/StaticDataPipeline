using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// 한 시트에서 BuffTable 과 DebuffTable 을 IsDebuff 로 갈라 만드는 입력이다.
[StaticDataRecord("Rpg", "Buffs")]
public sealed partial record BuffRecord(
    [Key] BuffRecord.BuffId Id,
    string Name,
    bool IsDebuff,
    [Range(1, 3600)] ushort DurationSec)
{
    public record struct BuffId(int Value);
}
