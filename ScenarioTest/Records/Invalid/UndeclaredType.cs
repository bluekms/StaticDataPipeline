using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// 어디에도 선언하지 않은 타입(Visibility). Record 소스 폴더에도 BCL 에도 없으니 스캐너가 거부한다.
[StaticDataRecord("Invalid", "Clubs")]
public sealed partial record ClubRecord([Key] int Id, string Name, Visibility Visibility);
