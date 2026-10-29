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
    string? ConditionLiteral)
{
    public static FkBranch ForeignKey(string tableSetMember, string targetColumn)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: false,
            ConditionColumn: null,
            ConditionValue: null,
            ConditionLiteral: null);
    }

    public static FkBranch SwitchForeignKey(
        string tableSetMember,
        string targetColumn,
        string conditionColumn,
        string conditionValue,
        string conditionLiteral)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: true,
            conditionColumn,
            conditionValue,
            conditionLiteral);
    }
}
