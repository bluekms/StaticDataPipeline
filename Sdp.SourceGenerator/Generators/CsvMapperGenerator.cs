using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class CsvMapperGenerator
{
    private const string StaticDataRecordAttributeMetadataName = "Sdp.Attributes.StaticDataRecordAttribute";

    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var analyses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                StaticDataRecordAttributeMetadataName,
                predicate: static (node, _) => node is RecordDeclarationSyntax,
                transform: static (attributeContext, cancellationToken) =>
                    Analyze(attributeContext, cancellationToken));

        context.RegisterSourceOutput(analyses, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmitMapperShell)
            {
                return;
            }

            var source = CsvMapperEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.HintName, source);
        });
    }

    private static RecordAnalysis Analyze(
        GeneratorAttributeSyntaxContext attributeContext,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<Diagnostic>();

        var symbol = (INamedTypeSymbol)attributeContext.TargetSymbol;
        var syntax = (RecordDeclarationSyntax)attributeContext.TargetNode;

        if (IsGenericOrNestedInGeneric(symbol))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordCannotBeGeneric,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));

            return new RecordAnalysis(
                symbol,
                ImmutableArray<ParameterAnalysis>.Empty,
                diagnostics,
                CanEmit: false,
                CanEmitMapperShell: false);
        }

        var isRecordPartial = syntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isRecordPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustBePartial,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var allContainingPartial = ContainingTypePartialChecker.Check(symbol, diagnostics);

        var canEmit = false;
        var canEmitMapperShell = false;

        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(symbol);
        if (primaryCtor is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));

            return new RecordAnalysis(
                symbol,
                ImmutableArray<ParameterAnalysis>.Empty,
                diagnostics,
                canEmit,
                canEmitMapperShell);
        }

        var parameters = ParameterAnalyzer.CollectParameters(
            primaryCtor,
            cancellationToken);

        var allParameters = FlattenParameters(parameters);

        ValidateKeyUniqueness(symbol, syntax, parameters, diagnostics);

        ValidateCountRangeUsage(symbol, syntax, allParameters, diagnostics);

        ValidateLengthAndSingleColumnCollectionUsage(symbol, syntax, allParameters, diagnostics);

        canEmitMapperShell = isRecordPartial && allContainingPartial;
        if (canEmitMapperShell)
        {
            var rangeValidator = new RangeAttributeValidator(symbol, syntax, allParameters, diagnostics);

            var specificallyRejected = ValidateCollectionParameterUsage(symbol, syntax, allParameters, diagnostics);

            specificallyRejected.UnionWith(rangeValidator.ValidateRedundantTypedRange());

            specificallyRejected.UnionWith(rangeValidator.ValidateNumericRangeBounds());

            specificallyRejected.UnionWith(rangeValidator.ValidateTypedRangeBounds());

            specificallyRejected.UnionWith(
                new AttributeApplicabilityValidator(symbol, syntax, allParameters, diagnostics).Validate());

            specificallyRejected.UnionWith(ValidateLengthValue(symbol, syntax, allParameters, diagnostics));

            specificallyRejected.UnionWith(rangeValidator.ValidateRangeOrdering());

            specificallyRejected.UnionWith(ValidateFrozenDictionaryKeys(symbol, syntax, allParameters, diagnostics));

            // 중복 보고 제외
            // emit 할 수 없으며,
            // 이름 자체가 거부 목록에 없으며,
            // 상위 경로도 거부 목록에 없는 (Outer.Inner도 Outer가 거부되면 출력 제외)
            // param.Name만 보고
            var unsupportedParameterNames = parameters
                .Where(param => !ParameterEmittability.IsParameterEmittable(param))
                .Where(param => !specificallyRejected.Contains(param.Name))
                .Where(param => !specificallyRejected.Any(
                    path => path.StartsWith(param.Name + ".", StringComparison.Ordinal)))
                .Select(param => param.Name)
                .ToList();

            if (unsupportedParameterNames.Count > 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.UnsupportedMapperParameter,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    string.Join(", ", unsupportedParameterNames)));
            }

            canEmit = specificallyRejected.Count == 0 && unsupportedParameterNames.Count == 0;
        }

        return new RecordAnalysis(symbol, parameters, diagnostics, canEmit, canEmitMapperShell);
    }

    private static bool IsGenericOrNestedInGeneric(INamedTypeSymbol symbol)
    {
        if (symbol.IsGenericType)
        {
            return true;
        }

        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }

    private static List<QualifiedParameter> FlattenParameters(ImmutableArray<ParameterAnalysis> parameters)
    {
        var result = new List<QualifiedParameter>();
        var visitedNestedRecords = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        AddParameters(parameters, pathPrefix: string.Empty, visitedNestedRecords, result);
        return result;
    }

    private static void AddParameters(
        ImmutableArray<ParameterAnalysis> parameters,
        string pathPrefix,
        HashSet<INamedTypeSymbol> visitedNestedRecords,
        List<QualifiedParameter> result)
    {
        foreach (var param in parameters)
        {
            if (param.IsIgnored)
            {
                continue;
            }

            var path = pathPrefix.Length == 0 ? param.Name : pathPrefix + "." + param.Name;
            result.Add(new QualifiedParameter(path, param));

            if (param.Nested is { } nested && visitedNestedRecords.Add(nested.Symbol))
            {
                AddParameters(nested.Parameters, path, visitedNestedRecords, result);
            }

            if (param.Collection is not null)
            {
                if (param.Collection.ElementNested is { } elementNested && visitedNestedRecords.Add(elementNested.Symbol))
                {
                    AddParameters(elementNested.Parameters, path, visitedNestedRecords, result);
                }

                if (param.Collection.ValueNested is { } valueNested && visitedNestedRecords.Add(valueNested.Symbol))
                {
                    AddParameters(valueNested.Parameters, path, visitedNestedRecords, result);
                }
            }
        }
    }

    private static void ValidateKeyUniqueness(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        ImmutableArray<ParameterAnalysis> parameters,
        List<Diagnostic> diagnostics)
    {
        var keyCount = parameters.Count(static param => param.IsKey);
        if (keyCount > 1)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.MultipleKeyAttributes,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                keyCount));
        }
    }

    private static HashSet<string> ValidateCollectionParameterUsage(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;

            if (IsNullableCollection(param.Type))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.NullableCollectionNotSupported,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
                rejected.Add(qualified.Path);
                continue;
            }

            if (param is { HasSingleColumnCollectionAttribute: true, Collection.Kind: CollectionKind.FrozenDictionary })
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.SingleColumnCollectionNotAllowedOnDictionary,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }

    private static bool IsNullableCollection(ITypeSymbol type)
    {
        // ImmutableArray<T>? 는 Nullable<ImmutableArray<T>> (값 타입)
        if (type is INamedTypeSymbol nullableValue &&
            nullableValue.IsValueType &&
            nullableValue.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return TypeClassifier.ClassifyCollection(nullableValue.TypeArguments[0]) is not null;
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return TypeClassifier.ClassifyCollection(type) is not null;
        }

        return false;
    }

    private static void ValidateCountRangeUsage(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (!param.HasCountRangeAttribute)
            {
                continue;
            }

            if (!param.HasSingleColumnCollectionAttribute)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.CountRangeRequiresSingleColumnCollection,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
            }

            if (param.HasLengthAttribute)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.CountRangeAndLengthMutuallyExclusive,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
            }
        }
    }

    private static void ValidateLengthAndSingleColumnCollectionUsage(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param is { HasLengthAttribute: true, HasSingleColumnCollectionAttribute: true })
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.LengthAndSingleColumnCollectionMutuallyExclusive,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
            }
        }
    }

    private static HashSet<string> ValidateLengthValue(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (!param.HasLengthAttribute)
            {
                continue;
            }

            if (param.Collection is not { Kind: CollectionKind.ImmutableArray or CollectionKind.FrozenSet or CollectionKind.FrozenDictionary } collection)
            {
                continue;
            }

            if (collection.Length < 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.LengthMustBePositive,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    collection.Length));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }

    private static HashSet<string> ValidateFrozenDictionaryKeys(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.Collection is not { Kind: CollectionKind.FrozenDictionary, ValueNested: { } valueNested } info)
            {
                continue;
            }

            var keyParams = valueNested.Parameters.Where(nestedParameter => nestedParameter.IsKey).ToList();
            if (keyParams.Count != 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.FrozenDictionaryValueMustHaveSingleKey,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    valueNested.Symbol.ToDisplayString(),
                    keyParams.Count));
                rejected.Add(qualified.Path);
                continue;
            }

            var keyParam = keyParams[0];
            if (!ParameterEmittability.IsFrozenDictionaryKeyCompatible(info, keyParam))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.FrozenDictionaryKeyTypeMismatch,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    info.KeyType!.ToDisplayString(),
                    valueNested.Symbol.ToDisplayString(),
                    keyParam.Name,
                    keyParam.Type.ToDisplayString()));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }
}
