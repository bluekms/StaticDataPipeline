using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

#pragma warning disable RS1038, RS1041
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038, RS1041
public class XunitTestLoggerLogsAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "SDP1002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Logger Logs Verification Required",
        "If you declared a logger, you must check logger.Logs",
        "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        var hasFactOrTheoryAttribute = methodDeclaration.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() == "Fact" || a.Name.ToString() == "Theory");
        if (!hasFactOrTheoryAttribute)
        {
            return;
        }

        var loggerVariable = methodDeclaration.DescendantNodes()
            .OfType<DeclarationPatternSyntax>()
            .FirstOrDefault(v => v.Type.ToString().StartsWith("TestOutputLogger", StringComparison.Ordinal));
        if (loggerVariable is null)
        {
            return;
        }

        var logsUsage = methodDeclaration.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => invocation.ArgumentList.Arguments.Any(x => x.ToString().Contains(".Logs")));

        if (!logsUsage)
        {
            var diagnostic = Diagnostic.Create(Rule, loggerVariable.GetLocation(), loggerVariable.Type.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
