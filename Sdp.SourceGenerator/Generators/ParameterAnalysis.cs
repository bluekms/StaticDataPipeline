using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record ParameterAnalysis(
    string Name,
    string ColumnName,
    ITypeSymbol Type,
    ScalarKind Kind,
    bool IsNullable,
    bool IsKey,
    string? NullString,
    string? DateTimeFormat,
    string? TimeSpanFormat,
    RangeInfo? Range,
    string? RegexPattern,
    NestedRecordInfo? Nested,
    CollectionInfo? Collection,
    bool HasUnsupportedAttribute,
    bool HasCountRangeAttribute,
    bool HasLengthAttribute,
    bool HasSingleColumnCollectionAttribute,
    bool IsIgnored)
{
    public bool IsCollection => Collection is not null;

    public bool IsRecord => Nested is not null;

    public ScalarKind EffectiveKind => Collection is { } collection ? collection.ElementKind : Kind;

    public ITypeSymbol EffectiveType => Collection is { } collection ? collection.ElementType : Type;

    public static ParameterAnalysis Ignored(string name, string columnName, ITypeSymbol type)
    {
        return new ParameterAnalysis(
            name,
            columnName,
            type,
            ScalarKind.Unsupported,
            IsNullable: false,
            IsKey: false,
            NullString: null,
            DateTimeFormat: null,
            TimeSpanFormat: null,
            Range: null,
            RegexPattern: null,
            Nested: null,
            Collection: null,
            HasUnsupportedAttribute: false,
            HasCountRangeAttribute: false,
            HasLengthAttribute: false,
            HasSingleColumnCollectionAttribute: false,
            IsIgnored: true);
    }
}

internal sealed record NestedRecordInfo(
    INamedTypeSymbol Symbol,
    ImmutableArray<ParameterAnalysis> Parameters)
{
    // single-param scalar record — 예: record ItemId(long Value).
    // record 를 감싼 체인(예: record Middle(Inner Inner))은 브랜딩이 아니다 — 마지막 스칼라 세그먼트만 축약 가능.
    public bool IsTypeBrandingRecord
        => Parameters.Length == 1
           && Parameters[0].Nested is null
           && Parameters[0].Collection is null
           && Parameters[0].Kind != ScalarKind.Unsupported;
}

internal sealed record CollectionInfo(
    CollectionKind Kind,
    int Length,
    ITypeSymbol ElementType,
    ScalarKind ElementKind,
    NestedRecordInfo? ValueNested,
    ITypeSymbol? KeyType,
    ScalarKind KeyKind,
    string? Separator,
    int? MinCount,
    int? MaxCount,
    NestedRecordInfo? ElementNested = null);

internal enum CollectionKind
{
    ImmutableArray,
    FrozenSet,
    FrozenDictionary,
    SingleColumnImmutableArray,
    SingleColumnFrozenSet,
}

internal sealed record RangeInfo(
    object? Minimum,
    object? Maximum,
    RangeArgKind ArgKind);

internal enum RangeArgKind
{
    Numeric,
    Typed,
}
