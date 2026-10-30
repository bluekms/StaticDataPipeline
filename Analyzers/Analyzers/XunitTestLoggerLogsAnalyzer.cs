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

    private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        if (!TestLoggerSyntax.IsTestMethod(methodDeclaration))
        {
            return;
        }

        var loggerPattern = TestLoggerSyntax.FindLoggerPattern(methodDeclaration, context.SemanticModel, out _);
        if (loggerPattern is null || loggerPattern.Designation is not SingleVariableDesignationSyntax designation)
        {
            return;
        }

        var loggerSymbol = context.SemanticModel.GetDeclaredSymbol(designation, context.CancellationToken);
        if (loggerSymbol is null)
        {
            return;
        }

        var usesLogs = methodDeclaration.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(access => access.Name.Identifier.ValueText == "Logs")
            .Select(access => context.SemanticModel.GetSymbolInfo(access.Expression, context.CancellationToken).Symbol)
            .Any(symbol => SymbolEqualityComparer.Default.Equals(symbol, loggerSymbol));
        if (usesLogs)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, loggerPattern.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
