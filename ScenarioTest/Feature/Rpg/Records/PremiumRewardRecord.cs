using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// StaticDataRecord 인 SeasonPassRecord 의 SFK IsPremium=true 쪽 대상.
[StaticDataRecord("Rpg", "PremiumRewards")]
public sealed partial record PremiumRewardRecord(
    [Key] PremiumRewardRecord.PremiumRewardId Id,
    [ForeignKey("ItemTable", "Id")] ItemRecord.ItemId ItemId,
    [Range(1, 99)] ushort Count)
{
    public record struct PremiumRewardId(int Value);
}
