using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

public record struct NpcId([NullString("")] int? Value);

// 값 하나를 감싼 record 의 안쪽 값은 nullable 일 수 없다. 값이 없을 수 있는 자리는 NpcId? 와 [NullString] 으로 적는다.
[StaticDataRecord("Invalid", "Notices")]
public sealed partial record NoticeRecord(
    [Key] int Id,
    string Title,
    NpcId Sender);
