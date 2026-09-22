using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class StaticDataTableGenerator
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var tables = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateTableClass(node),
                transform: static (syntaxContext, cancellationToken) => Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Collect()
            .SelectMany(static (analyses, _) =>
            {
                var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var list = new List<StaticDataTableAnalysis>(analyses.Length);
                foreach (var analysis in analyses)
                {
                    if (seen.Add(analysis!.Symbol))
                    {
                        list.Add(analysis);
                    }
                }

                return list;
            });

        context.RegisterSourceOutput(tables, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmit)
            {
                return;
            }

            var source = StaticDataTableEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.HintName, source);
        });
    }

    private static bool IsCandidateTableClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static StaticDataTableAnalysis? Analyze(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
        if (declaredSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = FindStaticDataTableBase(symbol);
        if (baseType is null)
        {
            return null;
        }

        if (baseType.TypeArguments.Length < 1)
        {
            return null;
        }

        var recordType = baseType.TypeArguments[0] as INamedTypeSymbol;
        if (recordType is null)
        {
            return null;
        }

        var diagnostics = new List<Diagnostic>();
        var isPartial = classDecl.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.StaticDataTableMustBePartial,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var containingPartial = SymbolResolver.AreContainingTypesPartial(symbol, diagnostics);

        var hasUserDefinedConstructor = symbol.InstanceConstructors
            .Any(static ctor => !ctor.IsImplicitlyDeclared);

        var recordSource = ExtractStaticDataRecordAttribute(recordType);
        if (recordSource is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.TableRecordMustHaveStaticDataRecordAttribute,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                recordType.ToDisplayString()));

            return new StaticDataTableAnalysis(
                symbol,
                recordType,
                ExcelFileName: string.Empty,
                SheetName: string.Empty,
                hasUserDefinedConstructor,
                diagnostics,
                CanEmit: false);
        }

        var canEmit = isPartial && containingPartial;

        return new StaticDataTableAnalysis(
            symbol,
            recordType,
            recordSource.ExcelFileName,
            recordSource.SheetName,
            hasUserDefinedConstructor,
            diagnostics,
            canEmit);
    }

    private static INamedTypeSymbol? FindStaticDataTableBase(INamedTypeSymbol symbol)
    {
        for (var type = symbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == "Sdp.Table" && type.Name == "StaticDataTable")
            {
                return type;
            }
        }

        return null;
    }

    private static StaticDataRecordSource? ExtractStaticDataRecordAttribute(INamedTypeSymbol recordType)
    {
        foreach (var attr in recordType.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
            {
                continue;
            }

            var fullName = attrClass.ToDisplayString();
            if (fullName != "Sdp.Attributes.StaticDataRecordAttribute")
            {
                continue;
            }

            var arguments = attr.ConstructorArguments;
            if (arguments.Length >= 2 &&
                arguments[0].Value is string excel &&
                arguments[1].Value is string sheet)
            {
                return new StaticDataRecordSource(excel, sheet);
            }
        }

        return null;
    }

    private sealed record StaticDataRecordSource(string ExcelFileName, string SheetName);
}
