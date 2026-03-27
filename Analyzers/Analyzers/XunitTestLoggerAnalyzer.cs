using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

#pragma warning disable RS1038, RS1041
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038, RS1041
public class XunitTestLoggerTypeAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "SDP1001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Generic type name mismatch",
        "Generic type name '{0}' does not match class name '{1}'",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        var hasFactOrTheoryAttribute = methodDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() == "Fact" || a.Name.ToString() == "Theory");

        if (!hasFactOrTheoryAttribute)
        {
            return;
        }

        var classDeclaration = methodDeclaration.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration == null)
        {
            return;
        }

        var className = classDeclaration.Identifier.Text;
        var loggerVariable = methodDeclaration.DescendantNodes()
            .OfType<DeclarationPatternSyntax>()
            .FirstOrDefault(v => v.Type.ToString().StartsWith("TestOutputLogger", StringComparison.Ordinal));
        if (loggerVariable is null)
        {
            return;
        }

        var genericTypeName = ExtractGenericTypeArgument(loggerVariable.Type.ToString());
        if (genericTypeName is null)
        {
            return;
        }

        if (genericTypeName != className)
        {
            var diagnostic = Diagnostic.Create(Rule, loggerVariable.GetLocation(), genericTypeName, className);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string? ExtractGenericTypeArgument(string typeName)
    {
        var match = Regex.Match(typeName, @"<(.+)>");
        return match.Success ? match.Groups[1].Value : null;
    }
}
