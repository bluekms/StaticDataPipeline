using Sdp.Attributes;

namespace ScenarioTest.Records.Rpg;

// enum 조건 SwitchFK. 두 대상의 키가 서로 다른 브랜딩(ItemId, CurrencyId)이라 열은 int 로 둔다.
[StaticDataRecord("Rpg", "Rewards")]
public sealed partial record RewardRecord(
    [Key] int Id,
    RewardKind Kind,
    [SwitchForeignKey(nameof(Kind), "Item", "ItemTable", "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId,
    [Range(typeof(long), "1", "9223372036854775807")] long Amount);
