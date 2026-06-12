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
}

internal sealed record NestedRecordInfo(
    INamedTypeSymbol Symbol,
    ImmutableArray<ParameterAnalysis> Parameters);

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
