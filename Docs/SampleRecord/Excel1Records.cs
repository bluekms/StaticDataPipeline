using System.Collections.Immutable;
using Eds.Attributes;

namespace Docs.SampleRecord.Excel1;

/// <summary>
/// Excel1 - 기본 기능 예제
/// 단순한 primitive 타입부터 배열, SingleColumnCollection까지
/// </summary>

// 1. 기본 시트 - primitive 타입 + nullable
[StaticDataRecord("Excel1", "FirstSheet")]
public sealed record FirstSheet(
    int Id,
    string Name,
    double Score,
    [NullString("-")] int? BonusPoint);

// 2. 배열 시트 - ImmutableArray와 Length, ColumnName 사용
[StaticDataRecord("Excel1", "ArraySheet")]
public sealed record ArraySheet(
    int Id,
    string Name,
    [ColumnName("Score")]
    [Length(3)]
    ImmutableArray<int> Scores);

// 3. 텍스트 시트 - 문자열, 줄바꿈, 특수문자, 외국어 통합 테스트
[StaticDataRecord("Excel1", "TextSheet")]
public sealed record TextSheet(
    int Id,
    string Text,
    [NullString("")] string? Description);

// 4. 동일 이름 시트 - 다른 Excel 파일에 같은 이름의 시트 존재
[StaticDataRecord("Excel1", "SameNameSheet")]
public sealed record SameNameSheet(
    int Id,
    string Category);

// 5. SingleColumnCollection - 단일 컬럼에서 여러 값 파싱
[StaticDataRecord("Excel1", "SingleColumnCollectionSheet")]
public sealed record SingleColumnCollectionSheet(
    int Id,
    [SingleColumnCollection(", ")] ImmutableArray<float> Values);
