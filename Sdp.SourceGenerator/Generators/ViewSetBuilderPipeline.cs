using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class ViewSetBuilderPipeline
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var viewSets = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateStaticDataManagerClass(node),
                transform: static (syntaxContext, cancellationToken) => Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Select(static (analysis, _) => analysis!)
            .Collect()
            .SelectMany(static (analyses, _) => ResolveSharedViewSets(analyses));

        context.RegisterSourceOutput(viewSets, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (analysis is not ViewSetAnalysis.Builder builder)
            {
                return;
            }

            var source = ViewSetBuilderEmitter.Emit(builder);
            sourceProductionContext.AddSource(builder.HintName, source);
        });
    }

    private static List<ViewSetAnalysis> ResolveSharedViewSets(ImmutableArray<ViewSetAnalysis> analyses)
    {
        var resolved = new List<ViewSetAnalysis>(analyses.Length);
        var analysesByViewSet = analyses.GroupBy(static analysis => analysis.Symbol, SymbolEqualityComparer.Default);
        foreach (var viewSetAnalyses in analysesByViewSet)
        {
            var first = viewSetAnalyses.First();
            resolved.Add(first);

            var reportedTableSets = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var other in viewSetAnalyses.Skip(1))
            {
                var usesFirstTableSet = SymbolEqualityComparer.Default.Equals(first.TableSetSymbol, other.TableSetSymbol);
                if (usesFirstTableSet)
                {
                    resolved.Add(other.Suppressed());
                    continue;
                }

                var isFirstConflictForTableSet = reportedTableSets.Add(other.TableSetSymbol);
                var conflictDiagnostics = isFirstConflictForTableSet
                    ? ImmutableArray.Create(CreateSharedByDifferentTableSets(first, other))
                    : ImmutableArray<Diagnostic>.Empty;
                resolved.Add(new ViewSetAnalysis.DiagnosticsOnly(other.Symbol, other.TableSetSymbol, conflictDiagnostics));
            }
        }

        return resolved;
    }

    private static Diagnostic CreateSharedByDifferentTableSets(ViewSetAnalysis first, ViewSetAnalysis other)
    {
        return Diagnostic.Create(
            SdpDiagnostics.ViewSetSharedByDifferentTableSets,
            other.Symbol.Locations.FirstOrDefault() ?? Location.None,
            other.Symbol.ToDisplayString(),
            first.TableSetSymbol.ToDisplayString(),
            other.TableSetSymbol.ToDisplayString());
    }

    private static bool IsCandidateStaticDataManagerClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static ViewSetAnalysis? Analyze(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
        if (declaredSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        if (baseType is null
            || baseType.ContainingNamespace?.ToDisplayString() != "Sdp.Manager"
            || baseType.Name != "StaticDataManager"
            || baseType.TypeArguments.Length != 2)
        {
            return null;
        }

        if (baseType.TypeArguments[0] is not INamedTypeSymbol tableSetType)
        {
            return null;
        }

        if (baseType.TypeArguments[1] is not INamedTypeSymbol viewSetType)
        {
            if (baseType.TypeArguments[1] is ITypeParameterSymbol typeParameter)
            {
                var diagnostic = Diagnostic.Create(
                    SdpDiagnostics.StaticDataManagerTypeArgumentMustBeClosed,
                    classDecl.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    typeParameter.Name);

                // ViewSet 심볼이 없으므로 매니저 심볼을 중복 정리 키로 쓴다.
                return new ViewSetAnalysis.DiagnosticsOnly(symbol, tableSetType, new[] { diagnostic });
            }

            return null;
        }

        if (SymbolResolver.IsGenericOrNestedInGeneric(viewSetType))
        {
            var location = viewSetType.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = SymbolResolver.CreateGenericTargetDiagnostic(viewSetType, location);

            return new ViewSetAnalysis.DiagnosticsOnly(viewSetType, tableSetType, new[] { diagnostic });
        }

        var diagnostics = new List<Diagnostic>();
        var viewSetSyntax = SymbolResolver.FindRecordDeclaration(viewSetType);
        var viewSetPartial = IsPartialRecord(viewSetType, viewSetSyntax, diagnostics);
        var viewSetOuterPartial = SymbolResolver.AreContainingTypesPartial(viewSetType, diagnostics);

        IMethodSymbol? primaryCtor = null;
        if (viewSetSyntax is not null)
        {
            primaryCtor = SymbolResolver.FindPrimaryConstructor(viewSetType);
            if (primaryCtor is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                    viewSetType.Locations.FirstOrDefault() ?? Location.None,
                    viewSetType.ToDisplayString()));
            }
        }

        var viewInfos = ImmutableArray<ViewInfo>.Empty;
        if (primaryCtor is not null)
        {
            viewInfos = AnalyzeViewInfos(primaryCtor, tableSetType, diagnostics, cancellationToken);
        }

        var canEmitBuilderShell = viewSetPartial && viewSetOuterPartial;
        if (!canEmitBuilderShell)
        {
            return new ViewSetAnalysis.DiagnosticsOnly(viewSetType, tableSetType, diagnostics);
        }

        if (primaryCtor is null)
        {
            return new ViewSetAnalysis.NotSupportedBuilder(viewSetType, tableSetType, diagnostics);
        }

        var hasNonViewParameter = viewInfos.Length < primaryCtor.Parameters.Length;
        if (hasNonViewParameter)
        {
            return new ViewSetAnalysis.NotSupportedBuilder(viewSetType, tableSetType, diagnostics);
        }

        var hasInvalidView = viewInfos.Any(static view => !view.IsFullyValid);
        if (hasInvalidView)
        {
            return new ViewSetAnalysis.DeferredErrorBuilder(viewSetType, tableSetType, diagnostics, viewInfos);
        }

        return new ViewSetAnalysis.ViewSetBuilder(viewSetType, tableSetType, diagnostics, viewInfos);
    }

    private static bool IsPartialRecord(
        INamedTypeSymbol viewSetType,
        RecordDeclarationSyntax? viewSetSyntax,
        List<Diagnostic> diagnostics)
    {
        if (viewSetSyntax is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ViewSetMustBeRecordInCurrentCompilation,
                viewSetType.Locations.FirstOrDefault() ?? Location.None,
                viewSetType.ToDisplayString()));

            return false;
        }

        var isPartial = viewSetSyntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ViewSetRecordMustBePartial,
                viewSetSyntax.Identifier.GetLocation(),
                viewSetType.ToDisplayString()));
        }

        return isPartial;
    }

    private static ImmutableArray<ViewInfo> AnalyzeViewInfos(
        IMethodSymbol primaryCtor,
        INamedTypeSymbol tableSetType,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<ViewInfo>(primaryCtor.Parameters.Length);

        foreach (var param in primaryCtor.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isNullable = param.NullableAnnotation == NullableAnnotation.Annotated;
            if (isNullable)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ViewSetMemberMustBeNonNullable,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    param.Type.ToDisplayString()));
            }

            var viewSymbol = param.Type as INamedTypeSymbol;
            var viewBase = viewSymbol is null ? null : SymbolResolver.FindStaticDataViewBase(viewSymbol);
            if (viewSymbol is null || viewBase is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ViewSetMemberMustBeStaticDataView,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    param.Type.ToDisplayString()));
                continue;
            }

            var viewDeclarations = viewSymbol.DeclaringSyntaxReferences
                .Select(static reference => reference.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .ToList();
            if (viewDeclarations.Count == 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ViewMustBeDeclaredInCurrentCompilation,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    viewSymbol.ToDisplayString()));
                continue;
            }

            INamedTypeSymbol? viewTableSet = null;
            if (viewBase.TypeArguments.Length == 1
                && viewBase.TypeArguments[0] is INamedTypeSymbol viewBaseTableSet)
            {
                viewTableSet = viewBaseTableSet;
            }

            var targetsStaticDataManagerTableSet = viewTableSet is null
                || SymbolEqualityComparer.Default.Equals(viewTableSet, tableSetType);
            if (!targetsStaticDataManagerTableSet)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ViewTargetsDifferentTableSet,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    viewSymbol.ToDisplayString(),
                    viewTableSet!.ToDisplayString(),
                    tableSetType.ToDisplayString()));
            }

            var isPartial = viewDeclarations.All(static declaration =>
                declaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));

            var containingTypesPartial = SymbolResolver.AreContainingTypesPartial(viewSymbol);

            var hasValidCtor = SymbolResolver.HasSingleTableSetConstructor(viewSymbol, tableSetType);

            builder.Add(new ViewInfo(
                param.Name,
                viewSymbol.ToDisplayString(),
                viewSymbol,
                isNullable,
                isPartial,
                containingTypesPartial,
                hasValidCtor,
                targetsStaticDataManagerTableSet,
                viewTableSet?.ToDisplayString()));
        }

        return builder.ToImmutable();
    }
}
