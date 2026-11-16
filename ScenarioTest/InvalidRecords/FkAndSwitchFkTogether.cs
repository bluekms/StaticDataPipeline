using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

public enum RewardKind
{
    Item,
    Currency,
}

// 한 열의 대상 테이블을 고정([ForeignKey])과 조건부([SwitchForeignKey])로 동시에 정할 수 없다.
[StaticDataRecord("Invalid", "Rewards")]
public sealed partial record RewardRecord(
    [Key] int Id,
    RewardKind Kind,
    [ForeignKey("ItemTable", "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId);
