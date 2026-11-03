using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// School Record 들이 쓰는 enum 과 중첩 record. SchoolLibrary.cs 와 다른 파일에 두어
// 스캐너가 Record 폴더 전체를 한 컴파일로 읽는지를 본다.
public enum Gender
{
    Male,
    Female,
}

public enum SubjectCode
{
    Korean,
    Math,
    English,
    Science,
    Social,
    History,
    PE,
    Music,
    Art,
    Info,
}

public enum Weekday
{
    Mon,
    Tue,
    Wed,
    Thu,
    Fri,
}

public enum MealType
{
    Lunch,
    Dinner,
}

public sealed record Seat([Range(1, 6)] byte Row, [Range(1, 6)] byte Col);

public sealed record Slot(ClassRoomRecord.ClassRoomId ClassRoomId, Weekday Day, [Range(1, 7)] byte Period);

public sealed record SubjectScore(
    [Key] SubjectCode Subject,
    [Range(0, 100)] int Score,
    [Range(typeof(char), "A", "E")] char Rank);

public sealed record MealSlot([DateTimeFormat("yyyy-MM-dd")] DateTime Date, MealType Meal);
