using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;

namespace SchemaInfoScanner.Schemata;

public sealed record RecordSchema(
    RecordName RecordName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeSyntax> RecordAttributeList,
    IReadOnlyList<PropertySchemaBase> PropertySchemata)
{
    public override string ToString()
    {
        return RecordName.FullName;
    }

    public string NestedFullName
    {
        get
        {
            var ns = NamedTypeSymbol.ContainingNamespace.Name;
            var typeNames = GetContainingTypeNames(NamedTypeSymbol);

            return $"{ns}.{string.Join(".", typeNames)}";
        }
    }

    private static Stack<string> GetContainingTypeNames(INamedTypeSymbol namedTypeSymbol)
    {
        var names = new Stack<string>();
        for (var current = namedTypeSymbol; current != null; current = current.ContainingType)
        {
            names.Push(current.Name);
        }

        return names;
    }
}
