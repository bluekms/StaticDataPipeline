using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 학생. 가장 많은 종류를 한 Record 에 모았다. Record 안에 선언한 키 브랜딩(StudentId), 한글 [ColumnName], enum, DateOnly 와 DateTimeFormat,
// FK 둘(하나는 nullable + NullString), nullable 문자열, RegularExpression(전화번호), 16자리 ulong(문자열 셀), bool.
// 추출 실패 시나리오의 셀 변조 대부분이 이 시트를 쓴다.
// StudentId 는 단일 값 record struct 브랜딩이다. 헤더 "Id" 와 "Id.Value" 폴백, 브랜딩 안쪽의 Range(학번 대역)를 본다.
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

// 교실. Record 안 브랜딩 키(ClassRoomId), byte/sbyte 와 음수 Range(지하 -1층), TeacherTable 로의 FK.
// 담임 교사가 두 반을 맡는 경우를 SchoolStaticData.Validate 가 잡는다.
[StaticDataRecord("School", "ClassRooms")]
public sealed partial record ClassRoomRecord(
    [Key] ClassRoomRecord.ClassRoomId Id,
    [Range(1, 3)] byte Grade,
    [Range(-1, 5)] sbyte Floor,
    [ForeignKey("TeacherTable", "Id")] TeacherRecord.TeacherId HomeroomTeacherId)
{
    public record struct ClassRoomId(int Value);
}

// 교사. Record 안 브랜딩 키(TeacherId), 빈 문자열을 null 로 읽는 NullString(""), 쉼표로 나뉘는 SingleColumnCollection FrozenSet<string>.
// TeacherScheduleView 가 수업 없는 교사를 잡는 입력이다.
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

// 시간표. 복합 키(Slot), TimeOnly 와 DateTimeFormat("HH:mm"), 시간 타입 Range(수업 시작 08:00~17:00), FK.
// StartTime 은 Excel 시간 셀과 텍스트 셀을 섞어 넣어도 같은 CSV 값이 되는지 본다.
// TimetableTable.Validate 가 교사의 같은 시간 중복 배정을 잡는다.
[StaticDataRecord("School", "Timetable")]
public sealed partial record TimetableRecord(
    [Key] Slot Slot,
    [DateTimeFormat("HH:mm")] [Range(typeof(TimeOnly), "08:00", "17:00")] TimeOnly StartTime,
    SubjectCode Subject,
    [ForeignKey("TeacherTable", "Id")] TeacherRecord.TeacherId TeacherId);

// 성적. 키가 곧 FK 인 경우(StudentTable 참조), [Length] 고정 길이 Map(5과목), 한글·특수문자 ColumnName,
// nullable 요소 컬렉션(결석 "x")과 CountRange. 학생 6명 중 4명만 있어 ReportCardView 의 Grade 가 nullable 이 된다.
[StaticDataRecord("School", "Grades")]
public sealed partial record GradeRecord(
    [Key] [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId StudentId,
    [Length(5)] FrozenDictionary<SubjectCode, SubjectScore> Scores,
    [ColumnName("출석일수(3~7월)")]
    [SingleColumnCollection(",")]
    [NullString("x")]
    [CountRange(5, 5)]
    ImmutableArray<byte?> MonthlyAttendance);

// 학사 일정. DateOnly Range(학년도 범위), nullable DateOnly + NullString("").
// CalendarTable.Validate 가 종료일이 시작일보다 앞서는 행을 잡는다.
[StaticDataRecord("School", "Calendar")]
public sealed partial record CalendarRecord(
    [Key] int Id,
    string Title,
    [DateTimeFormat("yyyy-MM-dd")] [Range(typeof(DateOnly), "2026-03-01", "2027-02-28")] DateOnly Start,
    [DateTimeFormat("yyyy-MM-dd")] [NullString("")] DateOnly? End,
    bool IsHoliday);

// 급식. DateTime 조각이 든 복합 키(MealSlot), 구분자가 다른 SingleColumnCollection 둘(쉼표 메뉴, 마침표 알레르기 번호),
// CountRange, ushort. 알레르기 번호는 문자열 셀로 넣는다.
[StaticDataRecord("School", "Cafeteria")]
public sealed partial record CafeteriaRecord(
    [Key] MealSlot Slot,
    [SingleColumnCollection(",")] [CountRange(1, 8)] ImmutableArray<string> Menu,
    [SingleColumnCollection(".")] FrozenSet<byte> Allergens,
    ushort Calories);

// 동아리. 선언 없이 쓰는 BCL enum(System.DayOfWeek)을 공백이 든 구분자(" / ")의 컬렉션과 CountRange 로 받고,
// 한글 NullString("미정")을 쓰는 nullable bool, Excel 시간 셀과 빈 셀이 섞인 nullable TimeOnly + NullString("").
// StudentRecord.ClubId 의 FK 대상이다.
[StaticDataRecord("School", "Clubs")]
public sealed partial record ClubRecord(
    [Key] int Id,
    string Name,
    [SingleColumnCollection(" / ")] [CountRange(1, 3)] ImmutableArray<System.DayOfWeek> MeetingDays,
    [NullString("미정")] bool? RequiresAudition,
    [NullString("")] [DateTimeFormat("HH:mm")] TimeOnly? MeetingTime);

public record struct BookId(int Value);

// 도서. 밖에 선언한 BookId 브랜딩을 키로 쓴다. ulong Range(ISBN-13)와 string Range(청구기호 사전순 비교), nullable char + NullString.
// Library 워크북의 대출·예약이 가리키는 대상이다.
[StaticDataRecord("Library", "Books")]
public sealed partial record BookRecord(
    [Key] BookId Id,
    [NullString("")] [Range(typeof(ulong), "9780000000000", "9799999999999")] ulong? Isbn13,
    string Title,
    [Range(typeof(string), "000", "999.99")] string CallNumber,
    short PublishedYear,
    [NullString("-")] char? ShelfCode);

// 대출. [Key] 없는 테이블, Record 속성의 시작 셀 "A1" 이 CLI 의 --start-cell B3 보다 우선하는 규칙(정상 추출의 유일한 경고),
// 다른 워크북(School)의 학번을 가리키는 FK, 초 단위 DateTimeFormat 과 TimeSpanFormat("c"), D·B 형식과 대문자가 섞인 Guid.
// 키 없음: 같은 학생이 같은 책을 반납 후 다시 빌릴 수 있어 중복 행을 허용한다.
[StaticDataRecord("Library", "Loans", "A1")]
public sealed partial record LoanRecord(
    [ForeignKey("BookTable", "Id")] BookId BookId,
    [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId Borrower,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss")] DateTime LoanedAt,
    [TimeSpanFormat("c")] TimeSpan Period,
    Guid ReceiptId);

// 예약. 데이터 행이 없는 시트가 "no data rows" Information 만 남기고 통과하는지를 본다.
[StaticDataRecord("Library", "Reservations")]
public sealed partial record ReservationRecord(
    [Key] int Id,
    [ForeignKey("BookTable", "Id")] BookId BookId,
    [ForeignKey("StudentTable", "Id")] StudentRecord.StudentId Borrower,
    [DateTimeFormat("yyyy-MM-dd HH:mm:ss")] DateTime ReservedAt);
