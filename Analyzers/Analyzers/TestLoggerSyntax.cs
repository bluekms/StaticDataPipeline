using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzers;

internal static class TestLoggerSyntax
{
    private const string LoggerTypeName = "TestOutputLogger";

    public static bool IsTestMethod(MethodDeclarationSyntax methodDeclaration)
    {
        return methodDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Select(a => SimpleName(a.Name))
            .Any(name => name is "Fact" or "Theory" or "FactAttribute" or "TheoryAttribute");
    }

    private static string SimpleName(NameSyntax name)
    {
        return name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => name.ToString(),
        };
    }

    public static DeclarationPatternSyntax? FindLoggerPattern(
        MethodDeclarationSyntax methodDeclaration,
        SemanticModel semanticModel,
        out INamedTypeSymbol? loggerType)
    {
        foreach (var pattern in methodDeclaration.DescendantNodes().OfType<DeclarationPatternSyntax>())
        {
            var type = semanticModel.GetTypeInfo(pattern.Type).Type as INamedTypeSymbol;
            if (type is { Name: LoggerTypeName, IsGenericType: true, TypeArguments.Length: 1 })
            {
                loggerType = type;

                return pattern;
            }
        }

        loggerType = null;

        return null;
    }
}
