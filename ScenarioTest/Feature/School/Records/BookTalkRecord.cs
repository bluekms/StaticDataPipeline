using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 독서 토론 모임 일정. [Key] 가 없다(같은 학생이 같은 책으로 여러 번 발제할 수 있어 중복 행을 허용).
// Presenter 는 다른 워크북(School)의 학번을 가리킨다.
[StaticDataRecord("Library", "BookTalks")]
public sealed partial record BookTalkRecord(
    [ForeignKey("BookTable", "Id")] BookId BookId,
    [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId Presenter,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss")] DateTime StartsAt,
    [TimeSpanFormat("c")] TimeSpan Duration,
    Guid RoomId);
