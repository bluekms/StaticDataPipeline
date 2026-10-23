using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal abstract record StaticDataViewAnalysis(
    INamedTypeSymbol Symbol,
    INamedTypeSymbol TableSetSymbol,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public sealed record DiagnosticsOnly(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : StaticDataViewAnalysis(Symbol, TableSetSymbol, Diagnostics);

    public abstract record Factory(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : StaticDataViewAnalysis(Symbol, TableSetSymbol, Diagnostics)
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

    public sealed record ViewFactory(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : Factory(Symbol, TableSetSymbol, Diagnostics);

    public sealed record NotSupportedFactory(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : Factory(Symbol, TableSetSymbol, Diagnostics);
}
