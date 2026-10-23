using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal abstract record RecordAnalysis(
    INamedTypeSymbol Symbol,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public sealed record DiagnosticsOnly(
        INamedTypeSymbol Symbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : RecordAnalysis(Symbol, Diagnostics);

    public abstract record Mapper(
        INamedTypeSymbol Symbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : RecordAnalysis(Symbol, Diagnostics)
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

    public sealed record CsvMapper(
        INamedTypeSymbol Symbol,
        IReadOnlyList<Diagnostic> Diagnostics,
        ImmutableArray<ParameterAnalysis> Parameters)
        : Mapper(Symbol, Diagnostics);

    public sealed record NotSupportedMapper(
        INamedTypeSymbol Symbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : Mapper(Symbol, Diagnostics);
}
