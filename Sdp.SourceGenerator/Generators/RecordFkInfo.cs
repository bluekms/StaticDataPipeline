using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record RecordFkInfo(
    string TableSetMember,
    INamedTypeSymbol RecordSymbol,
    ImmutableArray<FkParameter> Parameters);

internal sealed record FkParameter(
    string PropertyName,
    ITypeSymbol PropertyType,
    ImmutableArray<FkBranch> Branches);

internal sealed record FkBranch(
    string TableSetMember,
    string TargetColumn,
    bool IsSwitch,
    string? ConditionColumn,
    string? ConditionValue,
    ITypeSymbol? ConditionType)
{
    public static FkBranch ForeignKey(string tableSetMember, string targetColumn)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: false,
            ConditionColumn: null,
            ConditionValue: null,
            ConditionType: null);
    }

    public static FkBranch SwitchForeignKey(
        string tableSetMember,
        string targetColumn,
        string conditionColumn,
        string conditionValue,
        ITypeSymbol conditionType)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: true,
            conditionColumn,
            conditionValue,
            conditionType);
    }
}
