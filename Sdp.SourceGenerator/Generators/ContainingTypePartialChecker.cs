using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class ContainingTypePartialChecker
{
    public static bool Check(INamedTypeSymbol symbol, List<Diagnostic> diagnostics)
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
}
