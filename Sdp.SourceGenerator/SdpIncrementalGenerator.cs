using Microsoft.CodeAnalysis;
using Sdp.SourceGenerator.Generators;

namespace Sdp.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class SdpIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        CsvMapperPipeline.Register(context);
        TableFactoryPipeline.Register(context);

        var staticDataManagers = StaticDataManagerAnalyzer.Collect(context);
        TableSetLoaderPipeline.Register(context, staticDataManagers);
        StaticDataManagerBridgePipeline.Register(context, staticDataManagers);

        ViewFactoryPipeline.Register(context);
        ViewSetBuilderPipeline.Register(context);
    }
}
