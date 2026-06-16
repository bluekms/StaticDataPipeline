using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class SinglePrimaryConstructorResolver
{
    public static IMethodSymbol? Resolve(INamedTypeSymbol type)
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
}
