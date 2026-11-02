using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record RecordFkInfo(
    string TableSetMember,
    INamedTypeSymbol RecordSymbol,
    ImmutableArray<FkParameter> Parameters);

internal sealed record FkParameter(
    string PropertyName,
    ITypeSymbol ValueType,
    bool IsCollection,
    ImmutableArray<FkBranch> Branches);

internal sealed record FkBranch(
    string TableSetMember,
    string TargetColumn,
    bool IsSwitch,
    string? ConditionColumn,
    string? ConditionValue,
    string? ConditionLiteral,
    bool ConditionParsesAtRuntime,
    string? ConditionDisplayFormat,
    string? TargetUnwrapMember)
{
    public static FkBranch ForeignKey(string tableSetMember, string targetColumn, string? targetUnwrapMember)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: false,
            ConditionColumn: null,
            ConditionValue: null,
            ConditionLiteral: null,
            ConditionParsesAtRuntime: false,
            ConditionDisplayFormat: null,
            targetUnwrapMember);
    }

    public static FkBranch SwitchForeignKey(
        string tableSetMember,
        string targetColumn,
        string conditionColumn,
        string conditionValue,
        string conditionLiteral,
        bool conditionParsesAtRuntime,
        string? conditionDisplayFormat,
        string? targetUnwrapMember)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: true,
            conditionColumn,
            conditionValue,
            conditionLiteral,
            conditionParsesAtRuntime,
            conditionDisplayFormat,
            targetUnwrapMember);
    }
}
