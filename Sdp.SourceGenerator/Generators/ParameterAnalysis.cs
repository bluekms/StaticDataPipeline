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
