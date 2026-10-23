using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class TableSetLoaderPipeline
{
    public static void Register(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<StaticDataManagerAnalysis>> staticDataManagers)
    {
        var tableSets = staticDataManagers
            .SelectMany(static (analyses, _) => analyses
                .GroupBy(static analysis => analysis.TableSetSymbol, SymbolEqualityComparer.Default)
                .Select(static managers => managers.First())
                .ToList());

        context.RegisterSourceOutput(tableSets, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmit)
            {
                return;
            }

            var source = TableSetLoaderEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.HintName, source);
        });
    }
}
