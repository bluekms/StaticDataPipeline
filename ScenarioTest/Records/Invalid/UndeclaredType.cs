using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// Visibility 는 Record 소스 폴더에도 BCL 에도 없는 타입이다.
[StaticDataRecord("Invalid", "Clubs")]
public sealed partial record ClubRecord([Key] int Id, string Name, Visibility Visibility);
