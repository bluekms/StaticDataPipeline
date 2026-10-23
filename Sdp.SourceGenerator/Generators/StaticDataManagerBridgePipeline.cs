using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class StaticDataManagerBridgePipeline
{
    public static void Register(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<StaticDataManagerAnalysis>> staticDataManagers)
    {
        var bridges = staticDataManagers
            .SelectMany(static (analyses, _) => analyses
                .GroupBy(static analysis => analysis.Symbol, SymbolEqualityComparer.Default)
                .Select(static declarations => declarations.First())
                .ToList());

        context.RegisterSourceOutput(bridges, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.StaticDataManagerDiagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmitBridge)
            {
                return;
            }

            var source = StaticDataManagerBridgeEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.BridgeHintName, source);
        });
    }
}
