using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class ViewFactoryPipeline
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var views = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateViewClass(node),
                transform: static (syntaxContext, cancellationToken) => Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Select(static (analysis, _) => analysis!)
            .Collect()
            .SelectMany(static (analyses, _) => ResolveDuplicateDeclarations(analyses));

        context.RegisterSourceOutput(views, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (analysis is not StaticDataViewAnalysis.Factory factory)
            {
                return;
            }

            var source = ViewFactoryEmitter.Emit(factory);
            sourceProductionContext.AddSource(factory.HintName, source);
        });
    }

    private static List<StaticDataViewAnalysis> ResolveDuplicateDeclarations(ImmutableArray<StaticDataViewAnalysis> analyses)
    {
        var resolved = new List<StaticDataViewAnalysis>(analyses.Length);
        var analysesByView = analyses.GroupBy(static analysis => analysis.Symbol, SymbolEqualityComparer.Default);
        foreach (var declarations in analysesByView)
        {
            resolved.Add(declarations.First());
            foreach (var other in declarations.Skip(1))
            {
                resolved.Add(new StaticDataViewAnalysis.DiagnosticsOnly(other.Symbol, other.TableSetSymbol, other.Diagnostics));
            }
        }

        return resolved;
    }

    private static bool IsCandidateViewClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static StaticDataViewAnalysis? Analyze(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
        if (declaredSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = SymbolResolver.FindStaticDataViewBase(symbol);
        if (baseType is null || baseType.TypeArguments.Length != 1)
        {
            return null;
        }

        var tableSetType = baseType.TypeArguments[0] as INamedTypeSymbol;
        if (tableSetType is null)
        {
            return null;
        }

        if (SymbolResolver.IsGenericOrNestedInGeneric(symbol))
        {
            var diagnostic = SymbolResolver.CreateGenericTargetDiagnostic(symbol, classDecl.Identifier.GetLocation());

            return new StaticDataViewAnalysis.DiagnosticsOnly(symbol, tableSetType, new[] { diagnostic });
        }

        var isPartial = classDecl.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

        var hasValidCtor = SymbolResolver.HasSingleTableSetConstructor(symbol, tableSetType);

        var diagnostics = new List<Diagnostic>();
        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.StaticDataViewMustBePartial,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var containingPartial = SymbolResolver.AreContainingTypesPartial(symbol, diagnostics);

        if (!hasValidCtor)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.StaticDataViewMustHaveTableSetConstructor,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                tableSetType.ToDisplayString()));
        }

        var canEmitFactoryShell = isPartial && containingPartial;
        if (!canEmitFactoryShell)
        {
            return new StaticDataViewAnalysis.DiagnosticsOnly(symbol, tableSetType, diagnostics);
        }

        if (!hasValidCtor)
        {
            return new StaticDataViewAnalysis.NotSupportedFactory(symbol, tableSetType, diagnostics);
        }

        return new StaticDataViewAnalysis.ViewFactory(symbol, tableSetType, diagnostics);
    }
}
