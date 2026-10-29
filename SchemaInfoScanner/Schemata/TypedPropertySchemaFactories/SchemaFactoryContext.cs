using Microsoft.CodeAnalysis;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories;

public sealed class SchemaFactoryContext(SemanticModel semanticModel)
{
    public SemanticModel SemanticModel { get; } = semanticModel;

    public HashSet<INamedTypeSymbol> VisitingRecords { get; } = new(SymbolEqualityComparer.Default);
}
