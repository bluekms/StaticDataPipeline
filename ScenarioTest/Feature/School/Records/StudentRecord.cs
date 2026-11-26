using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 학생. 추출 실패 시나리오의 셀 변조 대부분이 이 시트를 쓴다.
// 16자리 CardNo 는 샘플 엑셀에 문자열 셀로 넣는다(숫자 셀은 Excel 이 15자리에서 끊는다).
// StudentId 헤더는 "Id" 와 "Id.Value" 둘 다 받는다(이 시트는 Id, Grades 시트는 StudentId.Value 를 쓴다).
[StaticDataRecord("School", "Students")]
public sealed partial record StudentRecord(
    [Key] StudentRecord.StudentId Id,

    [ColumnName("이름")] string Name,

    Gender Gender,

    [DateTimeFormat("yyyy-MM-dd")] DateOnly BirthDate,

    [ForeignKey("ClassRoomTable", "Id")] ClassRoomRecord.ClassRoomId ClassRoomId,

    // Seat 는 SchoolStaticData.Validate 가 같은 반의 좌석 중복을 잡는 입력이다.
    Seat Seat,

    [NullString("-")]
    [ForeignKey("ClubTable", "Id")]
    int? ClubId,

    [NullString("-")] string? Nickname,

    [RegularExpression(@"^010-\d{4}-\d{4}$")] string Phone,

    ulong CardNo,

    bool IsDormitory)
{
    public record struct StudentId([Range(20240001, 20269999)] int Value);
}
