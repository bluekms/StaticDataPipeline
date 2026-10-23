using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal abstract record ViewSetAnalysis(
    INamedTypeSymbol Symbol,
    INamedTypeSymbol TableSetSymbol,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public DiagnosticsOnly Suppressed()
    {
        return new DiagnosticsOnly(Symbol, TableSetSymbol, ImmutableArray<Diagnostic>.Empty);
    }

    public sealed record DiagnosticsOnly(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : ViewSetAnalysis(Symbol, TableSetSymbol, Diagnostics);

    public abstract record Builder(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : ViewSetAnalysis(Symbol, TableSetSymbol, Diagnostics)
    {
        public string HintName
        {
            get
            {
                var qualified = Symbol.ToDisplayString(new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

                return qualified + ".ViewSetBuilder.g.cs";
            }
        }
    }

    public sealed record ViewSetBuilder(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics,
        ImmutableArray<ViewInfo> Views)
        : Builder(Symbol, TableSetSymbol, Diagnostics);

    public sealed record DeferredErrorBuilder(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics,
        ImmutableArray<ViewInfo> Views)
        : Builder(Symbol, TableSetSymbol, Diagnostics);

    public sealed record NotSupportedBuilder(
        INamedTypeSymbol Symbol,
        INamedTypeSymbol TableSetSymbol,
        IReadOnlyList<Diagnostic> Diagnostics)
        : Builder(Symbol, TableSetSymbol, Diagnostics);
}

internal sealed record ViewInfo(
    string ParameterName,
    string TypeName,
    INamedTypeSymbol ViewSymbol,
    bool IsNullable,
    bool IsPartial,
    bool ContainingTypesPartial,
    bool HasValidCtor,
    bool TargetsStaticDataManagerTableSet,
    string? ViewTableSetName)
{
    public bool IsFullyValid => IsPartial
        && ContainingTypesPartial // 바깥 타입이 partial이 아닌 View에는 ViewFactoryPipeline이 BuildView를 만들지 않는다. 그 View를 유효로 보면 생성된 Build가 없는 BuildView를 호출해 CS0117이 난다.
        && HasValidCtor
        && TargetsStaticDataManagerTableSet // View가 상속한 StaticDataView<T>의 T가 매니저의 TableSet과 다르면 BuildView(T)와 Build(매니저 TableSet)의 타입이 어긋나 CS0311이 난다(SDP0308).
        && !IsNullable;
}
