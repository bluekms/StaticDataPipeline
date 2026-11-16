using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 대출. [Key] 가 없다(같은 학생이 같은 책을 반납 후 다시 빌릴 수 있어 중복 행을 허용).
// Borrower 는 다른 워크북(School)의 학번을 가리킨다.
[StaticDataRecord("Library", "Loans")]
public sealed partial record LoanRecord(
    [ForeignKey("BookTable", "Id")] BookId BookId,
    [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId Borrower,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss")] DateTime LoanedAt,
    [TimeSpanFormat("c")] TimeSpan Period,
    Guid ReceiptId);
