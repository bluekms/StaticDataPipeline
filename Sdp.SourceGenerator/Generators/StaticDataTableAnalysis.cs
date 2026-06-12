using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record StaticDataTableAnalysis(
    INamedTypeSymbol Symbol,
    INamedTypeSymbol RecordSymbol,
    string ExcelFileName,
    string SheetName,
    bool HasUserDefinedConstructor,
    IReadOnlyList<Diagnostic> Diagnostics,
    bool CanEmit)
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
