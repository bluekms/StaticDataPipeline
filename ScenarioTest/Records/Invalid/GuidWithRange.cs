using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// Guid 는 대소 비교가 없어 [Range] 를 붙일 수 없다.
[StaticDataRecord("Invalid", "Receipts")]
public sealed partial record ReceiptRecord(
    [Key] int Id,
    [Range(typeof(Guid), "00000000-0000-0000-0000-000000000000", "ffffffff-ffff-ffff-ffff-ffffffffffff")] Guid Token);
