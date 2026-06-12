using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record RecordAnalysis(
    INamedTypeSymbol Symbol,
    ImmutableArray<ParameterAnalysis> Parameters,
    IReadOnlyList<Diagnostic> Diagnostics,
    bool CanEmit,
    bool CanEmitMapperShell)
{
    public string TypeName => Symbol.Name;

    public string FullyQualifiedTypeName
        => Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string HintName
    {
        get
        {
            var qualified = Symbol.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
            return qualified + ".CsvMapper.g.cs";
        }
    }
}
