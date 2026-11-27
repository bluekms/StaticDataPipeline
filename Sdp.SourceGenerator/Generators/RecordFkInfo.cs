using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record RecordFkInfo(
    string TableSetMember,
    INamedTypeSymbol RecordSymbol,
    FkScope Scope);

internal sealed record FkScope(
    ImmutableArray<FkParameter> Parameters,
    ImmutableArray<FkChildScope> Children)
{
    public bool IsEmpty => Parameters.IsEmpty && Children.IsEmpty;
}

internal sealed record FkChildScope(
    string PropertyName,
    FkChildKind Kind,
    INamedTypeSymbol RecordSymbol,
    FkScope Scope);

internal enum FkChildKind
{
    Record,
    ArrayElement,
    SetElement,
    MapValue,
}

internal sealed record FkNestedPosition(
    FkChildKind Kind,
    NestedRecordInfo Nested,
    bool IsNullable)
{
    public static FkNestedPosition? Find(ParameterAnalysis parameter)
    {
        if (parameter.Nested is { } nested)
        {
            return new FkNestedPosition(FkChildKind.Record, nested, parameter.IsNullable);
        }

        if (parameter.Collection is not { } collection)
        {
            return null;
        }

        var isElementNullable = TypeClassifier.IsNullable(collection.ElementType);
        if (collection.ValueNested is { } valueNested)
        {
            return new FkNestedPosition(FkChildKind.MapValue, valueNested, isElementNullable);
        }

        if (collection.ElementNested is not { } elementNested)
        {
            return null;
        }

        var elementKind = collection.Kind == CollectionKind.FrozenSet
            ? FkChildKind.SetElement
            : FkChildKind.ArrayElement;

        return new FkNestedPosition(elementKind, elementNested, isElementNullable);
    }
}

internal sealed record FkParameter(
    int Index,
    string PropertyName,
    ITypeSymbol ValueType,
    FkCollectionKind Collection,
    string? ValueDisplayMember,
    FkBranch? ForeignKey,
    ImmutableArray<FkBranch> SwitchBranches)
{
    public ImmutableArray<FkBranch> Branches
    {
        get
        {
            if (ForeignKey is null)
            {
                return SwitchBranches;
            }

            return SwitchBranches.Insert(0, ForeignKey);
        }
    }
}

internal enum FkCollectionKind
{
    None,
    Array,
    Set,
}

internal enum FkConditionDisplayKind
{
    Default,
    Boolean,
    NullableBoolean,
}

internal sealed record FkBranch(
    string TableSetMember,
    string TargetColumn,
    string? ConditionColumn,
    string? ConditionValue,
    string? ConditionLiteral,
    bool ConditionParsesAtRuntime,
    string? ConditionDisplayFormat,
    FkConditionDisplayKind ConditionDisplayKind,
    string? TargetUnwrapMember,
    bool IsTargetNullable)
{
    public static FkBranch ForeignKey(string tableSetMember, string targetColumn, FkTargetResolution target)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            ConditionColumn: null,
            ConditionValue: null,
            ConditionLiteral: null,
            ConditionParsesAtRuntime: false,
            ConditionDisplayFormat: null,
            ConditionDisplayKind: FkConditionDisplayKind.Default,
            target.TargetUnwrapMember,
            target.IsTargetNullable);
    }

    public static FkBranch SwitchForeignKey(
        string tableSetMember,
        string targetColumn,
        string conditionColumn,
        string conditionValue,
        string conditionLiteral,
        bool conditionParsesAtRuntime,
        string? conditionDisplayFormat,
        FkConditionDisplayKind conditionDisplayKind,
        FkTargetResolution target)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            conditionColumn,
            conditionValue,
            conditionLiteral,
            conditionParsesAtRuntime,
            conditionDisplayFormat,
            conditionDisplayKind,
            target.TargetUnwrapMember,
            target.IsTargetNullable);
    }
}
