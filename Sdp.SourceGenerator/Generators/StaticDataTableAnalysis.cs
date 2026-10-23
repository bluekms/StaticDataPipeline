using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal abstract record StaticDataTableAnalysis(
    INamedTypeSymbol Symbol,
    INamedTypeSymbol RecordSymbol,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public sealed record DiagnosticsOnly(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol RecordSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : StaticDataTableAnalysis(Symbol, RecordSymbol, Diagnostics);

    public sealed record TableFactory(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol RecordSymbol,
        IReadOnlyList<Diagnostic> Diagnostics,
        string ExcelFileName,
        string SheetName,
        bool HasUserDefinedConstructor)
        : StaticDataTableAnalysis(Symbol, RecordSymbol, Diagnostics)
    {
        public string TypeName => Symbol.Name;

        public string FullyQualifiedTypeName
            => Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        public string FullyQualifiedRecordName
            => RecordSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        public string HintName
        {
            get
            {
                var qualified = Symbol.ToDisplayString(new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

                return qualified + ".TableFactory.g.cs";
            }
        }
    }
}
