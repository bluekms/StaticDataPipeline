using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

public enum RewardKind
{
    Item,
    Currency,
}

// 같은 파라미터에 [ForeignKey] 와 [SwitchForeignKey] 를 함께 붙임. 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Rewards")]
public sealed partial record RewardRecord(
    [Key] int Id,
    RewardKind Kind,
    [ForeignKey("ItemTable", "Id")]
    [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
    int TargetId);
