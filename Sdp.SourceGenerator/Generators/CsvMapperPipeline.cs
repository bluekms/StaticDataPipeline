using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class CsvMapperPipeline
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var analyses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SdpAttributeNames.StaticDataRecordMetadataName,
                predicate: static (node, _) => node is RecordDeclarationSyntax,
                transform: static (syntaxContext, cancellationToken) => Analyze(syntaxContext, cancellationToken));
        context.RegisterSourceOutput(analyses, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (analysis is not RecordAnalysis.Mapper mapper)
            {
                return;
            }

            var source = CsvMapperEmitter.Emit(mapper);
            sourceProductionContext.AddSource(mapper.HintName, source);
        });
    }

    private static RecordAnalysis Analyze(
        GeneratorAttributeSyntaxContext attributeContext,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<Diagnostic>();

        var symbol = (INamedTypeSymbol)attributeContext.TargetSymbol;
        var syntax = (RecordDeclarationSyntax)attributeContext.TargetNode;

        if (SymbolResolver.IsGenericOrNestedInGeneric(symbol))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordCannotBeGeneric,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));

            return new RecordAnalysis.DiagnosticsOnly(symbol, diagnostics);
        }

        var isRecordPartial = syntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isRecordPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustBePartial,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var allContainingPartial = SymbolResolver.AreContainingTypesPartial(symbol, diagnostics);

        var primaryCtor = SymbolResolver.FindPrimaryConstructor(symbol);
        if (primaryCtor is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));

            return new RecordAnalysis.DiagnosticsOnly(symbol, diagnostics);
        }

        var parameters = ParameterAnalyzer.CollectParameters(
            primaryCtor,
            cancellationToken);
        var allParameters = FlattenParameters(parameters);

        ValidateKeyUniqueness(symbol, syntax, parameters, diagnostics);

        ValidateCountRangeUsage(symbol, syntax, allParameters, diagnostics);

        ValidateLengthAndSingleColumnCollectionUsage(symbol, syntax, allParameters, diagnostics);

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

        var unsupportedParameterNames = parameters
            .Where(static param => !ParameterEmittability.IsParameterEmittable(param))
            .Where(param => !specificallyRejected.Contains(param.Name))
            .Where(param => !specificallyRejected.Any(
                path => path.StartsWith(param.Name + ".", StringComparison.Ordinal)))
            .Select(static param => param.Name)
            .ToList();
        if (unsupportedParameterNames.Count > 0)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.UnsupportedMapperParameter,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                string.Join(", ", unsupportedParameterNames)));
        }

        var canEmitMapperShell = isRecordPartial && allContainingPartial;
        if (!canEmitMapperShell)
        {
            return new RecordAnalysis.DiagnosticsOnly(symbol, diagnostics);
        }

        var hasUnsupportedParameter = specificallyRejected.Count > 0 || unsupportedParameterNames.Count > 0;
        if (hasUnsupportedParameter)
        {
            return new RecordAnalysis.NotSupportedMapper(symbol, diagnostics);
        }

        return new RecordAnalysis.CsvMapper(symbol, diagnostics, parameters);
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
        var keyNames = parameters
            .Where(static param => param.IsKey)
            .Select(static param => param.Name)
            .ToList();
        if (keyNames.Count > 1)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.MultipleKeyAttributes,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                keyNames.Count,
                string.Join(", ", keyNames)));
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
        if (!TypeClassifier.IsNullable(type))
        {
            return false;
        }

        var collectionType = TypeClassifier.UnwrapNullable(type);

        return TypeClassifier.ClassifyCollection(collectionType) is not null;
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

            var keyParams = valueNested.Parameters.Where(static nestedParameter => nestedParameter.IsKey).ToList();
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
