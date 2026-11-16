using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// SeasonPass 의 IsPremium=true 쪽 대상.
[StaticDataRecord("Rpg", "PremiumRewards")]
public sealed partial record PremiumRewardRecord(
    [Key] PremiumRewardRecord.PremiumRewardId Id,
    [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [Range(1, 99)] ushort Count)
{
    public record struct PremiumRewardId(int Value);
}
