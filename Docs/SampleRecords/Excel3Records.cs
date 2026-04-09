using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace Docs.SampleRecords.Excel3;

/// <summary>
/// Excel3 - 복합 예제 (School 도메인)
/// nested record Id 래핑 패턴, 외부 정의 record 재사용
/// </summary>

// 외부 정의 nested record - 여러 record에서 재사용
public sealed record Address(
    [ColumnName("도시")] string City,
    [ColumnName("상세주소")] string Detail);

public sealed record ContactInfo(
    string Email,
    [NullString("")] string? Phone);

// School 시트 - 복합 구조
[StaticDataRecord("Excel3", "School")]
public sealed record School(
    School.SchoolId Id,
    string Name,
    Address Address,
    ContactInfo Contact,
    [Length(3)] ImmutableArray<string> Departments,
    [Length(2)] FrozenSet<int> Grades)
{
    public record struct SchoolId(int Value);
}

// Teacher 시트 - School 비PK FK 예제 (SchoolName은 School의 PK가 아님)
[StaticDataRecord("Excel3", "Teacher")]
public sealed record Teacher(
    Teacher.TeacherId Id,
    string Name,
    [ForeignKey("School", "Name")] string SchoolName)
{
    public record struct TeacherId(int Value);
}

// Student 시트 - School + Teacher 다중 FK 예제
[StaticDataRecord("Excel3", "Student")]
public sealed record Student(
    Student.StudentId Id,
    string Name,
    [ForeignKey("School", "Id")] School.SchoolId SchoolId,
    [ForeignKey("Teacher", "Id")] Teacher.TeacherId TeacherId)
{
    public record struct StudentId(int Value);
}
