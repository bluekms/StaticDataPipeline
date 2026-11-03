using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// Guid 에 [DateTimeFormat]. 날짜·시간 타입이 아닌 파라미터라 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Tokens")]
public sealed partial record TokenRecord([Key] int Id, [DateTimeFormat("yyyy-MM-dd")] Guid Token);
