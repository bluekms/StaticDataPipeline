using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record StaticDataViewAnalysis(
    INamedTypeSymbol Symbol,
    INamedTypeSymbol TableSetSymbol,
    IReadOnlyList<Diagnostic> Diagnostics,
    bool CanEmit,
    bool CanEmitFactoryShell)
{
    public string HintName
    {
        get
        {
            var qualified = Symbol.ToDisplayString(new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
            return qualified + ".ViewFactory.g.cs";
        }
    }
}
