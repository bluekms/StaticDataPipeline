using System.Collections.Frozen;
using System.Collections.Immutable;
using Eds.Attributes;

namespace Docs.SampleRecord.Excel2;

/// <summary>
/// Excel2 - 고급 기능 예제
/// Map, nested record, 다국어 컬럼명
/// </summary>

// 1. Map 시트 - FrozenDictionary와 Key 사용
[StaticDataRecord("Excel2", "MapSheet")]
public sealed record MapSheet(
    int Id,
    [Length(2)] FrozenDictionary<int, MapSheet.ItemData> Items)
{
    // 내부 정의 nested record + nullable
    public sealed record ItemData(
        [Key] int ItemId,
        int Quantity,
        [NullString("N/A")] string? Description);
}

// 2. 동일 이름 시트 - Excel1과 다른 구조
[StaticDataRecord("Excel2", "SameNameSheet")]
public sealed record SameNameSheet(
    int Id,
    string Description,
    decimal Price);

// 3. 학생 정보 - 클라이언트용 (담임 컬럼 포함)
[StaticDataRecord("Excel2", "StudentInfo")]
public sealed record StudentInfoForClient(
    int 번호,
    string 이름,
    int 학년,
    string 담임,
    [NullString("")] string? 비고);

// 4. 학생 정보 - 서버용 (비고, 등록일 컬럼 포함)
[StaticDataRecord("Excel2", "StudentInfo")]
public sealed record StudentInfoForServer(
    [ColumnName("번호")] int Id,
    [ColumnName("학년")] int Grade,
    [ColumnName("등록일")][DateTimeFormat("yyyy-MM-dd")] DateTime RegisteredAt);
