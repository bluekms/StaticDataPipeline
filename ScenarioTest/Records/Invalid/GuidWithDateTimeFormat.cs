using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [DateTimeFormat] 은 날짜·시간 타입에만 붙는다.
[StaticDataRecord("Invalid", "Tokens")]
public sealed partial record TokenRecord([Key] int Id, [DateTimeFormat("yyyy-MM-dd")] Guid Token);
