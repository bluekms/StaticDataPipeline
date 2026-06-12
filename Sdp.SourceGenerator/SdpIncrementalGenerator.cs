using Microsoft.CodeAnalysis;
using Sdp.SourceGenerator.Generators;

namespace Sdp.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class SdpIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        CsvMapperGenerator.Register(context);
        StaticDataTableGenerator.Register(context);
        TableSetGenerator.Register(context);
        StaticDataViewGenerator.Register(context);
        ViewSetGenerator.Register(context);
    }
}
