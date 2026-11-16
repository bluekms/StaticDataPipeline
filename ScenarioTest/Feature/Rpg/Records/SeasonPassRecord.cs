using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// bool 조건 SwitchFK. 두 대상의 키가 서로 다른 브랜딩이라 열은 int 로 둔다.
[StaticDataRecord("Rpg", "SeasonPass")]
public sealed partial record SeasonPassRecord(
    [Key] int Id,
    [Range(1, 100)] byte Level,
    bool IsPremium,
    [SwitchForeignKey(nameof(IsPremium), "false", "FreeRewardTable", "Id")]
    [SwitchForeignKey(nameof(IsPremium), "true", "PremiumRewardTable", "Id")]
    int RewardId);
