using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ForeignKeyAnalyzer
{
    public static ImmutableArray<RecordFkInfo> CollectRecordFkInfos(
        ImmutableArray<TableInfo> tables,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<RecordFkInfo>();
        var cache = new Dictionary<INamedTypeSymbol, ImmutableArray<FkParameter>>(SymbolEqualityComparer.Default);

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (table.RecordSymbol is null)
            {
                continue;
            }

            if (!cache.TryGetValue(table.RecordSymbol, out var paramInfos))
            {
                paramInfos = AnalyzeFkParameters(table.RecordSymbol, membersByName);
                cache[table.RecordSymbol] = paramInfos;
            }

            if (paramInfos.Length == 0)
            {
                continue;
            }

            builder.Add(new RecordFkInfo(table.ParameterName, table.RecordSymbol, paramInfos));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<FkParameter> AnalyzeFkParameters(
        INamedTypeSymbol recordType,
        Dictionary<string, INamedTypeSymbol?> membersByName)
    {
        var primaryCtor = SymbolResolver.FindPrimaryConstructor(recordType);
        if (primaryCtor is null)
        {
            return ImmutableArray<FkParameter>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<FkParameter>();

        foreach (var param in primaryCtor.Parameters)
        {
            var branchBuilder = ImmutableArray.CreateBuilder<FkBranch>();
            string? formatsConditionColumn = null;
            string? dateTimeFormat = null;
            string? timeSpanFormat = null;
            foreach (var attr in param.GetAttributes())
            {
                if (!SdpAttributeNames.IsSdpAttribute(attr))
                {
                    continue;
                }

                var args = attr.ConstructorArguments;

                if (attr.AttributeClass!.Name == "ForeignKeyAttribute" &&
                    args.Length >= 2 &&
                    args[0].Value is string tableSetName &&
                    args[1].Value is string columnName)
                {
                    if (!FkTargetResolver.IsValidFkTarget(param.Type, membersByName, tableSetName, columnName))
                    {
                        continue;
                    }

                    branchBuilder.Add(FkBranch.ForeignKey(tableSetName, columnName));
                }
                else if (attr.AttributeClass.Name == "SwitchForeignKeyAttribute" &&
                         args.Length >= 4 &&
                         args[0].Value is string conditionColumn &&
                         args[1].Value is string conditionValue &&
                         args[2].Value is string switchTableSetName &&
                         args[3].Value is string switchColumnName)
                {
                    var conditionProperty = SwitchForeignKeyConditionLiteralBuilder.FindConditionParameter(recordType, conditionColumn);
                    if (conditionProperty is null)
                    {
                        continue;
                    }

                    if (!FkTargetResolver.IsValidFkTarget(param.Type, membersByName, switchTableSetName, switchColumnName))
                    {
                        continue;
                    }

                    // 조건 타입·값이 유효하지 않은 분기는 진단(SDP0227/0228 등)이 빌드를 막으므로 생성 대상에서 뺀다.
                    var conditionType = TypeClassifier.UnwrapNullable(conditionProperty.Type);
                    if (!SwitchForeignKeyConditionLiteralBuilder.IsScalarConditionType(conditionType))
                    {
                        continue;
                    }

                    if (!string.Equals(formatsConditionColumn, conditionColumn, StringComparison.Ordinal))
                    {
                        SwitchForeignKeyConditionLiteralBuilder.ReadFormats(recordType, conditionColumn, out dateTimeFormat, out timeSpanFormat);
                        formatsConditionColumn = conditionColumn;
                    }

                    var conditionLiteral = SwitchForeignKeyConditionLiteralBuilder.TryBuild(conditionType, conditionValue, dateTimeFormat, timeSpanFormat);
                    if (conditionLiteral is null)
                    {
                        continue;
                    }

                    branchBuilder.Add(FkBranch.SwitchForeignKey(
                        switchTableSetName,
                        switchColumnName,
                        conditionColumn,
                        conditionValue,
                        conditionLiteral.Code));
                }
            }

            if (branchBuilder.Count == 0)
            {
                continue;
            }

            builder.Add(new FkParameter(
                param.Name,
                FkTargetResolver.ResolveFkValueType(param.Type),
                FkTargetResolver.IsCollectionFk(param.Type),
                branchBuilder.ToImmutable()));
        }

        return builder.ToImmutable();
    }
}
