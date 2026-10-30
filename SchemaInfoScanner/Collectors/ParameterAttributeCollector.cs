using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Schemata;

namespace SchemaInfoScanner.Collectors;

public sealed class ParameterAttributeCollector
{
    private readonly Dictionary<PropertyName, IReadOnlyList<AttributeInfo>> attributesDictionary = [];

    public int Count => attributesDictionary.Count;

    public bool ContainsRecord(PropertyName propertyName)
    {
        return attributesDictionary.ContainsKey(propertyName);
    }

    public IReadOnlyList<AttributeInfo> this[PropertyName propertyName] => attributesDictionary[propertyName];

    public void Collect(RecordName recordName, ParameterSyntax parameter, SemanticModel semanticModel)
    {
        var recordPropertyName = new PropertyName(recordName, parameter);
        var attributeList = AttributeInfoReader.Read(parameter.AttributeLists, semanticModel);

        attributesDictionary.Add(recordPropertyName, attributeList);
    }
}
