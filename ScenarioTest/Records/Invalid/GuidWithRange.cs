using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// Guid 에 [Range]. Guid 는 대소 비교를 하지 않는 타입이라 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Receipts")]
public sealed partial record ReceiptRecord(
    [Key] int Id,
    [Range(typeof(Guid), "00000000-0000-0000-0000-000000000000", "ffffffff-ffff-ffff-ffff-ffffffffffff")] Guid Token);
