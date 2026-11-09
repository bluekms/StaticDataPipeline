using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 학생. 추출 실패 시나리오의 셀 변조 대부분이 이 시트를 쓴다. 16자리 CardNo 는 픽스처에 문자열 셀로 넣는다(숫자 셀은 Excel 이 15자리에서 끊는다).
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
    [NullString("-")] [ForeignKey("ClubTable", "Id")] int? ClubId,
    [NullString("-")] string? Nickname,
    [RegularExpression(@"^010-\d{4}-\d{4}$")] string Phone,
    ulong CardNo,
    bool IsDormitory)
{
    public record struct StudentId([Range(20240001, 20269999)] int Value);
}

// 교실. 담임 교사가 두 반을 맡는 경우를 SchoolStaticData.Validate 가 잡는다.
[StaticDataRecord("School", "ClassRooms")]
public sealed partial record ClassRoomRecord(
    [Key] ClassRoomRecord.ClassRoomId Id,
    [Range(1, 3)] byte Grade,
    [Range(-1, 5)] sbyte Floor,
    [ForeignKey("TeacherTable", "Id")] TeacherRecord.TeacherId HomeroomTeacherId)
{
    public record struct ClassRoomId(int Value);
}

// 교사. TeacherScheduleView 가 수업 없는 교사를 잡는 입력이다.
[StaticDataRecord("School", "Teachers")]
public sealed partial record TeacherRecord(
    [Key] TeacherRecord.TeacherId Id,
    string Name,
    SubjectCode MainSubject,
    [NullString("")] string? Office,
    [SingleColumnCollection(",")] FrozenSet<string> Certificates)
{
    public record struct TeacherId(int Value);
}

// 시간표. StartTime 은 픽스처에 Excel 시간 셀과 텍스트 셀을 섞어 넣어 같은 CSV 값이 되는지 본다. TimetableTable.Validate 가 교사의 같은 시간 중복 배정을 잡는다.
[StaticDataRecord("School", "Timetable")]
public sealed partial record TimetableRecord(
    [Key] Slot Slot,
    [DateTimeFormat("HH:mm")] [Range(typeof(TimeOnly), "08:00", "17:00")] TimeOnly StartTime,
    SubjectCode Subject,
    [ForeignKey("TeacherTable", "Id")] TeacherRecord.TeacherId TeacherId);

// 성적. 학생 6명 중 4명만 있어 ReportCardView 의 Grade 가 nullable 이 된다.
[StaticDataRecord("School", "Grades")]
public sealed partial record GradeRecord(
    [Key] [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId StudentId,
    [Length(5)] FrozenDictionary<SubjectCode, SubjectScore> Scores,
    [ColumnName("출석일수(3~7월)")]
    [SingleColumnCollection(",")]
    [NullString("x")]
    [CountRange(5, 5)]
    ImmutableArray<byte?> MonthlyAttendance);

// 학사 일정. CalendarTable.Validate 가 종료일이 시작일보다 앞서는 행을 잡는다.
[StaticDataRecord("School", "Calendar")]
public sealed partial record CalendarRecord(
    [Key] int Id,
    string Title,
    [DateTimeFormat("yyyy-MM-dd")] [Range(typeof(DateOnly), "2026-03-01", "2027-02-28")] DateOnly Start,
    [DateTimeFormat("yyyy-MM-dd")] [NullString("")] DateOnly? End,
    bool IsHoliday);

// 급식. Allergens 는 "5.6" 처럼 숫자로 읽힐 수 있어 픽스처에 문자열 셀로 넣는다.
[StaticDataRecord("School", "Cafeteria")]
public sealed partial record CafeteriaRecord(
    [Key] MealSlot Slot,
    [SingleColumnCollection(",")] [CountRange(1, 8)] ImmutableArray<string> Menu,
    [SingleColumnCollection(".")] FrozenSet<byte> Allergens,
    ushort Calories);

// 동아리. StudentRecord.ClubId 의 FK 대상. MeetingTime 은 픽스처에 Excel 시간 셀과 빈 셀이 섞여 있다.
[StaticDataRecord("School", "Clubs")]
public sealed partial record ClubRecord(
    [Key] int Id,
    string Name,
    [SingleColumnCollection(" / ")] [CountRange(1, 3)] ImmutableArray<System.DayOfWeek> MeetingDays,
    [NullString("미정")] bool? RequiresAudition,
    [NullString("")] [DateTimeFormat("HH:mm")] TimeOnly? MeetingTime);

public record struct BookId(int Value);

// 도서. 다른 Record 와 달리 BookId 브랜딩을 Record 밖에 선언했다. Library 워크북의 대출과 예약이 가리키는 대상이다.
[StaticDataRecord("Library", "Books")]
public sealed partial record BookRecord(
    [Key] BookId Id,
    [NullString("")] [Range(typeof(ulong), "9780000000000", "9799999999999")] ulong? Isbn13,
    string Title,
    [Range(typeof(string), "000", "999.99")] string CallNumber,
    short PublishedYear,
    [NullString("-")] char? ShelfCode);

// 대출. [Key] 가 없다(같은 학생이 같은 책을 반납 후 다시 빌릴 수 있어 중복 행을 허용). 시작 셀 "A1" 이 CLI 의 --start-cell B3 보다 우선해 정상 추출의 유일한 경고를 남긴다.
// Borrower 는 다른 워크북(School)의 학번을 가리킨다.
[StaticDataRecord("Library", "Loans", "A1")]
public sealed partial record LoanRecord(
    [ForeignKey("BookTable", "Id")] BookId BookId,
    [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId Borrower,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss")] DateTime LoanedAt,
    [TimeSpanFormat("c")] TimeSpan Period,
    Guid ReceiptId);

// 예약. 데이터 행이 없는 시트가 "no data rows" Information 만 남기고 통과하는지 보는 입력이다.
[StaticDataRecord("Library", "Reservations")]
public sealed partial record ReservationRecord(
    [Key] int Id,
    [ForeignKey("BookTable", "Id")] BookId BookId,
    [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId Borrower,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss")] DateTime ReservedAt);
