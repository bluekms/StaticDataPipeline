using System.Collections.Immutable;
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
        if (!TestLoggerSyntax.IsTestMethod(methodDeclaration))
        {
            return;
        }

        var classDeclaration = methodDeclaration.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration is null)
        {
            return;
        }

        var loggerPattern = TestLoggerSyntax.FindLoggerPattern(methodDeclaration, context.SemanticModel, out var loggerType);
        if (loggerPattern is null || loggerType is null)
        {
            return;
        }

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
        if (classSymbol is null)
        {
            return;
        }

        var genericArgument = loggerType.TypeArguments[0];
        if (SymbolEqualityComparer.Default.Equals(genericArgument.OriginalDefinition, classSymbol.OriginalDefinition))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, loggerPattern.GetLocation(), genericArgument.Name, classSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
