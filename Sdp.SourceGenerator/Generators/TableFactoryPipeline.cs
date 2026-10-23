using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class TableFactoryPipeline
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var tables = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateTableClass(node),
                transform: static (syntaxContext, cancellationToken) => Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Select(static (analysis, _) => analysis!)
            .Collect()
            .SelectMany(static (analyses, _) => analyses
                .GroupBy(static analysis => analysis.Symbol, SymbolEqualityComparer.Default)
                .Select(static declarations => declarations.First())
                .ToList());

        context.RegisterSourceOutput(tables, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (analysis is not StaticDataTableAnalysis.TableFactory tableFactory)
            {
                return;
            }

            var source = TableFactoryEmitter.Emit(tableFactory);
            sourceProductionContext.AddSource(tableFactory.HintName, source);
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

        var recordSource = ExtractStaticDataRecordAttribute(recordType);
        if (recordSource is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.TableRecordMustHaveStaticDataRecordAttribute,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                recordType.ToDisplayString()));

            return new StaticDataTableAnalysis.DiagnosticsOnly(symbol, recordType, diagnostics);
        }

        var canEmitFactory = isPartial && containingPartial;
        if (!canEmitFactory)
        {
            return new StaticDataTableAnalysis.DiagnosticsOnly(symbol, recordType, diagnostics);
        }

        var hasUserDefinedConstructor = symbol.InstanceConstructors
            .Any(static ctor => !ctor.IsImplicitlyDeclared);

        return new StaticDataTableAnalysis.TableFactory(
            symbol,
            recordType,
            diagnostics,
            recordSource.ExcelFileName,
            recordSource.SheetName,
            hasUserDefinedConstructor);
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
            if (fullName != SdpAttributeNames.StaticDataRecordMetadataName)
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
