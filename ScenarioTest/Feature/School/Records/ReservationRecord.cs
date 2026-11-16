using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 예약. 데이터 행이 없는 시트가 "no data rows" Information 만 남기고 통과하는지 보는 입력이다.
[StaticDataRecord("Library", "Reservations")]
public sealed partial record ReservationRecord(
    [Key] int Id,
    [ForeignKey("BookTable", "Id")] BookId BookId,
    [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId Borrower,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss")] DateTime ReservedAt);
