using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// StaticDataRecord 인 SeasonPassRecord 의 SFK IsPremium=true 쪽 대상.
// ItemId 열은 브랜딩 키(ItemRecord.ItemId)를 가리키는 int 열이다. 로더가 대상 키의 안쪽 값으로 비교한다.
[StaticDataRecord("Rpg", "PremiumRewards")]
public sealed partial record PremiumRewardRecord(
    [Key] PremiumRewardRecord.PremiumRewardId Id,
    [ForeignKey("ItemTable", "Id")] int ItemId,
    [Range(1, 99)] ushort Count)
{
    public record struct PremiumRewardId(int Value);
}
