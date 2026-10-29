using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;

namespace SchemaInfoScanner.Collectors;

public sealed class ParameterNamedTypeSymbolCollector(SemanticModel semanticModel)
{
    private readonly Dictionary<PropertyName, INamedTypeSymbol> namedTypeSymbolDictionary = [];

    public int Count => namedTypeSymbolDictionary.Count;

    public IEnumerable<PropertyName> ParameterNames => namedTypeSymbolDictionary.Keys;

    public INamedTypeSymbol this[PropertyName propertyName] => namedTypeSymbolDictionary[propertyName];

    public void Collect(RecordName recordName, ParameterSyntax parameter)
    {
        if (parameter.Type is null)
        {
            throw new NotSupportedException();
        }

        var typeSymbol = semanticModel.GetTypeInfo(parameter.Type).Type;
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            throw new NotSupportedException();
        }

        var recordPropertyName = new PropertyName(recordName, parameter);
        namedTypeSymbolDictionary.Add(recordPropertyName, namedTypeSymbol);
    }
}
