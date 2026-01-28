using System.Collections.Frozen;
using System.Collections.Immutable;
using Eds.Attributes;

namespace Docs.SampleRecord.Excel3;

/// <summary>
/// Excel3 - 복합 예제 (School 도메인)
/// nested record Id 래핑 패턴, 외부 정의 record 재사용
/// </summary>

// Id 래핑 패턴 - 연산 방지, 비교만 가능
// int Id 대신 record로 래핑하면 실수로 Id + 1 같은 연산을 방지
public sealed record SchoolId(int Value);

public sealed record TeacherId(int Value);

public sealed record StudentId(int Value);

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
    SchoolId Id,
    string Name,
    Address Address,
    ContactInfo Contact,
    [Length(3)] ImmutableArray<string> Departments,
    [Length(2)] FrozenSet<int> Grades);
