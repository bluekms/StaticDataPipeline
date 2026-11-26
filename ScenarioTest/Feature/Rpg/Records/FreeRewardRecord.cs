using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// StaticDataRecord 인 SeasonPassRecord 의 SFK IsPremium=false 쪽 대상.
[StaticDataRecord("Rpg", "FreeRewards")]
public sealed partial record FreeRewardRecord(
    [Key] FreeRewardRecord.FreeRewardId Id,
    [ForeignKey("CurrencyTable", "Id")] CurrencyRecord.CurrencyId CurrencyId,
    [Range(1, 999_999)] uint Amount)
{
    public record struct FreeRewardId(int Value);
}
