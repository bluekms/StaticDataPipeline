using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class SymbolResolver
{
    public static INamedTypeSymbol? FindStaticDataViewBase(INamedTypeSymbol symbol)
    {
        for (var type = symbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == "Sdp.View" && type.Name == "StaticDataView")
            {
                return type;
            }
        }

        return null;
    }

    public static bool AreContainingTypesPartial(INamedTypeSymbol symbol, List<Diagnostic> diagnostics)
    {
        var allPartial = true;

        for (var parent = symbol.ContainingType; parent is not null; parent = parent.ContainingType)
        {
            var parentSyntax = parent.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault();
            if (parentSyntax is null)
            {
                continue;
            }

            if (!parentSyntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                allPartial = false;
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ContainingTypeMustBePartial,
                    parentSyntax.Identifier.GetLocation(),
                    parent.ToDisplayString()));
            }
        }

        return allPartial;
    }

    public static IMethodSymbol? FindPrimaryConstructor(INamedTypeSymbol type)
    {
        return type.InstanceConstructors
            .Where(ctor => !ctor.IsImplicitlyDeclared)
            .FirstOrDefault(IsPrimaryConstructor);
    }

    private static bool IsPrimaryConstructor(IMethodSymbol constructor)
    {
        foreach (var reference in constructor.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is RecordDeclarationSyntax { ParameterList: not null })
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasSingleTableSetConstructor(INamedTypeSymbol viewSymbol, INamedTypeSymbol tableSetType)
    {
        var declaredConstructors = viewSymbol.InstanceConstructors
            .Where(ctor => !ctor.IsImplicitlyDeclared)
            .ToList();

        return declaredConstructors.Count == 1
            && declaredConstructors[0].Parameters.Length == 1
            && SymbolEqualityComparer.Default.Equals(declaredConstructors[0].Parameters[0].Type, tableSetType);
    }
}
