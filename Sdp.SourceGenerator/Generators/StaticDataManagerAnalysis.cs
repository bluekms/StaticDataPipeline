using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record StaticDataManagerAnalysis(
    INamedTypeSymbol Symbol,
    INamedTypeSymbol TableSetSymbol,
    INamedTypeSymbol? ViewSetSymbol,
    ImmutableArray<TableInfo> Tables,
    ImmutableArray<RecordFkInfo> RecordFkInfos,
    IReadOnlyList<Diagnostic> Diagnostics,
    IReadOnlyList<Diagnostic> StaticDataManagerDiagnostics,
    bool CanEmit,
    bool CanEmitBridge)
{
    public static StaticDataManagerAnalysis DiagnosticsOnly(
        INamedTypeSymbol symbol,
        IReadOnlyList<Diagnostic> staticDataManagerDiagnostics)
    {
        // 이 분석은 로더 스트림에 아무것도 내지 않으므로 TableSetSymbol 값은 쓰이지 않는다. 매니저 자신을 넣는다.
        return new StaticDataManagerAnalysis(
            symbol,
            TableSetSymbol: symbol,
            ViewSetSymbol: null,
            ImmutableArray<TableInfo>.Empty,
            ImmutableArray<RecordFkInfo>.Empty,
            Array.Empty<Diagnostic>(),
            staticDataManagerDiagnostics,
            CanEmit: false,
            CanEmitBridge: false);
    }

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
            var qualified = Symbol.ToDisplayString(new SymbolDisplayFormat(
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
