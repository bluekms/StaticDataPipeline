using System.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Schemata;

namespace SchemaInfoScanner.Collectors;

public class RecordAttributeCollector : IEnumerable<KeyValuePair<RecordName, IReadOnlyList<AttributeInfo>>>
{
    private readonly Dictionary<RecordName, IReadOnlyList<AttributeInfo>> attributesDictionary = [];

    public int Count => attributesDictionary.Count;

    public void Collect(RecordName recordName, RecordDeclarationSyntax recordDeclaration, SemanticModel semanticModel)
    {
        var attributeList = AttributeInfoReader.Read(recordDeclaration.AttributeLists, semanticModel);

        attributesDictionary.Add(recordName, attributeList);
    }

    public IEnumerator<KeyValuePair<RecordName, IReadOnlyList<AttributeInfo>>> GetEnumerator()
    {
        return attributesDictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
