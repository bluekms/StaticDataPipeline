using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class StaticDataManagerAnalyzer
{
    public static IncrementalValueProvider<ImmutableArray<StaticDataManagerAnalysis>> Collect(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateStaticDataManagerClass(node),
                transform: static (syntaxContext, cancellationToken) => Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Select(static (analysis, _) => analysis!)
            .Collect();
    }

    private static bool IsCandidateStaticDataManagerClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static StaticDataManagerAnalysis? Analyze(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
        if (declaredSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        if (baseType is null)
        {
            return null;
        }

        if (baseType.ContainingNamespace?.ToDisplayString() != "Sdp.Manager")
        {
            return null;
        }

        if (baseType.Name != "StaticDataManager")
        {
            return null;
        }

        if (baseType.TypeArguments.Length is not (1 or 2))
        {
            return null;
        }

        if (baseType.TypeArguments[0] is not INamedTypeSymbol tableSetType)
        {
            if (baseType.TypeArguments[0] is ITypeParameterSymbol typeParameter)
            {
                var diagnostic = Diagnostic.Create(
                    SdpDiagnostics.StaticDataManagerTypeArgumentMustBeClosed,
                    classDecl.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    typeParameter.Name);

                return StaticDataManagerAnalysis.DiagnosticsOnly(symbol, new[] { diagnostic });
            }

            return null;
        }

        if (SymbolResolver.IsGenericOrNestedInGeneric(symbol))
        {
            var diagnostic = SymbolResolver.CreateGenericTargetDiagnostic(symbol, classDecl.Identifier.GetLocation());

            return StaticDataManagerAnalysis.DiagnosticsOnly(symbol, new[] { diagnostic });
        }

        if (SymbolResolver.IsGenericOrNestedInGeneric(tableSetType))
        {
            var location = tableSetType.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = SymbolResolver.CreateGenericTargetDiagnostic(tableSetType, location);

            return StaticDataManagerAnalysis.DiagnosticsOnly(symbol, new[] { diagnostic });
        }

        var diagnostics = new List<Diagnostic>();
        var tableSetSyntax = SymbolResolver.FindRecordDeclaration(tableSetType);

        var tableSetPartial = false;
        var tableInfos = ImmutableArray<TableInfo>.Empty;
        var allParametersValid = false;
        if (tableSetSyntax is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.TableSetMustBeRecordInCurrentCompilation,
                tableSetType.Locations.FirstOrDefault() ?? Location.None,
                tableSetType.ToDisplayString()));
        }
        else
        {
            tableSetPartial = tableSetSyntax.Modifiers
                .Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
            if (!tableSetPartial)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.TableSetRecordMustBePartial,
                    tableSetSyntax.Identifier.GetLocation(),
                    tableSetType.ToDisplayString()));
            }

            tableInfos = AnalyzeTableInfos(tableSetType, diagnostics, out allParametersValid, cancellationToken);
        }

        var tableSetOuterPartial = SymbolResolver.AreContainingTypesPartial(tableSetType, diagnostics);

        var membersByName = new Dictionary<string, INamedTypeSymbol?>(StringComparer.Ordinal);
        foreach (var tableInfo in tableInfos)
        {
            membersByName[tableInfo.ParameterName] = tableInfo.RecordSymbol;
        }

        var foreignKeysValid = ForeignKeyAttributeValidator.Validate(
            tableInfos,
            membersByName,
            diagnostics,
            cancellationToken);

        var recordFkInfos = ForeignKeyAnalyzer.CollectRecordFkInfos(
            tableInfos,
            membersByName,
            cancellationToken);

        var canEmit = tableSetPartial
            && tableSetOuterPartial
            && allParametersValid
            && foreignKeysValid
            && tableInfos.All(static tableInfo => tableInfo.IsPartial);

        var staticDataManagerDiagnostics = new List<Diagnostic>();
        var staticDataManagerIsPartial = classDecl.Modifiers
            .Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (!staticDataManagerIsPartial)
        {
            staticDataManagerDiagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.StaticDataManagerMustBePartial,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var staticDataManagerContainingPartial =
            SymbolResolver.AreContainingTypesPartial(symbol, staticDataManagerDiagnostics);

        INamedTypeSymbol? viewSetType = null;
        var viewSetArgumentValid = true;
        if (baseType.TypeArguments.Length == 2)
        {
            viewSetType = baseType.TypeArguments[1] as INamedTypeSymbol;
            viewSetArgumentValid = viewSetType is not null;
        }

        var viewSetCanHaveBuilder = viewSetType is null || CanHaveViewSetBuilder(viewSetType);

        var canEmitBridge = staticDataManagerIsPartial
            && staticDataManagerContainingPartial
            && viewSetArgumentValid
            && viewSetCanHaveBuilder
            && canEmit;

        return new StaticDataManagerAnalysis(
            symbol,
            tableSetType,
            viewSetType,
            tableInfos,
            recordFkInfos,
            diagnostics,
            staticDataManagerDiagnostics,
            canEmit,
            canEmitBridge);
    }

    private static bool CanHaveViewSetBuilder(INamedTypeSymbol viewSetType)
    {
        var viewSetSyntax = SymbolResolver.FindRecordDeclaration(viewSetType);
        if (viewSetSyntax is null)
        {
            return false;
        }

        var isPartial = viewSetSyntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        var containingTypesPartial = SymbolResolver.AreContainingTypesPartial(viewSetType);

        return isPartial && containingTypesPartial;
    }

    private static ImmutableArray<TableInfo> AnalyzeTableInfos(
        INamedTypeSymbol tableSetType,
        List<Diagnostic> diagnostics,
        out bool allParametersValid,
        CancellationToken cancellationToken)
    {
        allParametersValid = true;

        var primaryCtor = SymbolResolver.FindPrimaryConstructor(tableSetType);
        if (primaryCtor is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                tableSetType.Locations.FirstOrDefault() ?? Location.None,
                tableSetType.ToDisplayString()));

            return ImmutableArray<TableInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<TableInfo>(primaryCtor.Parameters.Length);

        foreach (var param in primaryCtor.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableType = TypeClassifier.UnwrapNullable(param.Type);
            if (tableType is not INamedTypeSymbol tableSymbol || !IsStaticDataTableSubclass(tableSymbol))
            {
                allParametersValid = false;
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.TableSetMemberMustBeStaticDataTable,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    param.Type.ToDisplayString()));
                continue;
            }

            var tableSyntax = tableSymbol.DeclaringSyntaxReferences
                .Select(static reference => reference.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            var isPartial = tableSyntax is not null &&
                tableSyntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

            if (tableSyntax is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.TableMustBeDeclaredInCurrentCompilation,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    tableSymbol.ToDisplayString()));
            }
            else if (!isPartial)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.StaticDataTableMustBePartial,
                    tableSyntax.Identifier.GetLocation(),
                    tableSymbol.ToDisplayString()));
            }

            var isNullable = param.NullableAnnotation == NullableAnnotation.Annotated;
            var recordType = ExtractRecordType(tableSymbol);
            builder.Add(new TableInfo(param.Name, tableSymbol, recordType, isPartial, isNullable));
        }

        return builder.ToImmutable();
    }

    private static INamedTypeSymbol? ExtractRecordType(INamedTypeSymbol tableSymbol)
    {
        for (var type = tableSymbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == "Sdp.Table"
                && type.Name == "StaticDataTable"
                && type.TypeArguments.Length >= 1)
            {
                return type.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }

    private static bool IsStaticDataTableSubclass(INamedTypeSymbol symbol)
    {
        for (var type = symbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == "Sdp.Table"
                && type.Name == "StaticDataTable")
            {
                return true;
            }
        }

        return false;
    }
}
