using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ForeignKeyAnalyzer
{
    public static ImmutableArray<RecordFkInfo> CollectRecordFkInfos(
        ImmutableArray<TableInfo> tables,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        Dictionary<INamedTypeSymbol, ImmutableArray<ParameterAnalysis>> recordParameters,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<RecordFkInfo>();
        var cache = new Dictionary<INamedTypeSymbol, FkScope>(SymbolEqualityComparer.Default);

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (table.RecordSymbol is null)
            {
                continue;
            }

            var isCached = cache.TryGetValue(table.RecordSymbol, out var scope);
            if (!isCached)
            {
                scope = AnalyzeRootScope(table.RecordSymbol, membersByName, recordParameters, cancellationToken);
                cache[table.RecordSymbol] = scope;
            }

            if (scope.IsEmpty)
            {
                continue;
            }

            builder.Add(new RecordFkInfo(table.ParameterName, table.RecordSymbol, scope));
        }

        return builder.ToImmutable();
    }

    // 검증기와 분석기가 같은 Record 를 두 번 분석하지 않도록 매니저 분석 한 번 동안 결과를 나눠 쓴다.
    public static ImmutableArray<ParameterAnalysis> CollectRecordParameters(
        INamedTypeSymbol recordType,
        IMethodSymbol primaryCtor,
        Dictionary<INamedTypeSymbol, ImmutableArray<ParameterAnalysis>> recordParameters,
        CancellationToken cancellationToken)
    {
        var isCached = recordParameters.TryGetValue(recordType, out var parameters);
        if (!isCached)
        {
            parameters = ParameterAnalyzer.CollectParameters(primaryCtor, cancellationToken);
            recordParameters[recordType] = parameters;
        }

        return parameters;
    }

    private static FkScope AnalyzeRootScope(
        INamedTypeSymbol recordType,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        Dictionary<INamedTypeSymbol, ImmutableArray<ParameterAnalysis>> recordParameters,
        CancellationToken cancellationToken)
    {
        var primaryCtor = SymbolResolver.FindPrimaryConstructor(recordType);
        if (primaryCtor is null)
        {
            return new FkScope(ImmutableArray<FkParameter>.Empty, ImmutableArray<FkChildScope>.Empty);
        }

        var parameters = CollectRecordParameters(recordType, primaryCtor, recordParameters, cancellationToken);
        var nextParameterIndex = 0;

        return AnalyzeScope(recordType, primaryCtor, parameters, membersByName, ref nextParameterIndex);
    }

    // parameters 는 ParameterAnalyzer 가 primaryCtor.Parameters 와 같은 순서로 돌려준 것이다.
    private static FkScope AnalyzeScope(
        INamedTypeSymbol recordType,
        IMethodSymbol primaryCtor,
        ImmutableArray<ParameterAnalysis> parameters,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        ref int nextParameterIndex)
    {
        var parameterBuilder = ImmutableArray.CreateBuilder<FkParameter>();
        var childBuilder = ImmutableArray.CreateBuilder<FkChildScope>();

        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            var param = primaryCtor.Parameters[parameterIndex];
            var fkParameter = AnalyzeFkParameter(recordType, param, membersByName, nextParameterIndex);
            if (fkParameter is not null)
            {
                parameterBuilder.Add(fkParameter);
                nextParameterIndex++;
            }

            var childScope = AnalyzeChildScope(param, parameters[parameterIndex], membersByName, ref nextParameterIndex);
            if (childScope is not null)
            {
                childBuilder.Add(childScope);
            }
        }

        return new FkScope(parameterBuilder.ToImmutable(), childBuilder.ToImmutable());
    }

    private static FkChildScope? AnalyzeChildScope(
        IParameterSymbol param,
        ParameterAnalysis parameter,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        ref int nextParameterIndex)
    {
        var position = FkNestedPosition.Find(parameter);
        if (position is null)
        {
            return null;
        }

        // nullable 중첩 record 는 매퍼가 만들지 못해 빌드가 멈추므로 검사 코드를 만들지 않는다.
        // 브랜딩 record 는 안쪽 FK 가 SDP0229 로 거부되고, 브랜딩 파라미터 자체의 FK 는 AnalyzeFkParameter 가 다룬다.
        if (position.IsNullable || position.Nested.IsTypeBrandingRecord)
        {
            return null;
        }

        var nestedType = position.Nested.Symbol;
        var nestedCtor = SymbolResolver.FindPrimaryConstructor(nestedType);
        if (nestedCtor is null)
        {
            return null;
        }

        var scope = AnalyzeScope(nestedType, nestedCtor, position.Nested.Parameters, membersByName, ref nextParameterIndex);
        if (scope.IsEmpty)
        {
            return null;
        }

        return new FkChildScope(param.Name, position.Kind, nestedType, scope);
    }

    private static FkParameter? AnalyzeFkParameter(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        int parameterIndex)
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

            if (attr.AttributeClass!.Name == SdpAttributeNames.ForeignKey &&
                args.Length >= 2 &&
                args[0].Value is string tableSetName &&
                args[1].Value is string columnName)
            {
                var resolution = FkTargetResolver.ResolveFkTarget(param.Type, membersByName, tableSetName, columnName);
                if (resolution is null)
                {
                    continue;
                }

                branchBuilder.Add(FkBranch.ForeignKey(tableSetName, columnName, resolution.TargetUnwrapMember));
            }
            else if (attr.AttributeClass.Name == SdpAttributeNames.SwitchForeignKey &&
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

                var switchResolution = FkTargetResolver.ResolveFkTarget(param.Type, membersByName, switchTableSetName, switchColumnName);
                if (switchResolution is null)
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

                var conditionLiteral = SwitchForeignKeyConditionLiteralBuilder.TryBuild(conditionType, conditionColumn, conditionValue, dateTimeFormat, timeSpanFormat);
                if (conditionLiteral is null)
                {
                    continue;
                }

                branchBuilder.Add(FkBranch.SwitchForeignKey(
                    switchTableSetName,
                    switchColumnName,
                    conditionColumn,
                    conditionValue,
                    conditionLiteral.Code,
                    conditionLiteral.ParsesAtRuntime,
                    dateTimeFormat ?? timeSpanFormat,
                    switchResolution.TargetUnwrapMember));
            }
        }

        if (branchBuilder.Count == 0)
        {
            return null;
        }

        return new FkParameter(
            parameterIndex,
            param.Name,
            FkTargetResolver.ResolveFkValueType(param.Type),
            FkTargetResolver.IsCollectionFk(param.Type),
            branchBuilder.ToImmutable());
    }
}
