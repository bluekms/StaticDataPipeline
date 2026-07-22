using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record StaticDataManagerAnalysis(
    INamedTypeSymbol StaticDataManagerSymbol,
    INamedTypeSymbol TableSetSymbol,
    INamedTypeSymbol? ViewSetSymbol,
    ImmutableArray<TableInfo> Tables,
    ImmutableArray<RecordFkInfo> RecordFkInfos,
    IReadOnlyList<Diagnostic> Diagnostics,
    IReadOnlyList<Diagnostic> StaticDataManagerDiagnostics,
    bool CanEmit,
    bool CanEmitBridge)
{
    public string HintName
    {
        get
        {
            var qualified = TableSetSymbol.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

            return qualified + ".TableSetLoader.g.cs";
        }
    }

    public string BridgeHintName
    {
        get
        {
            var qualified = StaticDataManagerSymbol.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

            return qualified + ".StaticDataManager.g.cs";
        }
    }
}

internal sealed record TableInfo(
    string ParameterName,
    INamedTypeSymbol TableSymbol,
    INamedTypeSymbol? RecordSymbol,
    bool IsPartial,
    bool IsNullable);
