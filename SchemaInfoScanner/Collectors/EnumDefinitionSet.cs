using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SchemaInfoScanner.NameObjects;

namespace SchemaInfoScanner.Collectors;

public class EnumDefinitionSet
{
    private readonly Dictionary<EnumName, IReadOnlyList<string>> enumDictionary = [];

    public EnumDefinitionSet(RecordSchemaLoader.Result loadResult)
    {
        Collect(loadResult);
    }

    public EnumDefinitionSet(IReadOnlyList<RecordSchemaLoader.Result> loadResults)
    {
        foreach (var loadResult in loadResults)
        {
            Collect(loadResult);
        }
    }

    private void Collect(RecordSchemaLoader.Result loadResult)
    {
        foreach (var enumDeclaration in loadResult.EnumDeclarationList)
        {
            var enumSymbol = loadResult.SemanticModel.GetDeclaredSymbol(enumDeclaration);
            if (enumSymbol is null)
            {
                throw new NotSupportedException(enumDeclaration.Identifier.ValueText);
            }

            var enumName = new EnumName(enumSymbol);
            var members = enumDeclaration.Members.Select(x => x.Identifier.ValueText).ToList();
            enumDictionary.Add(enumName, members);
        }
    }

    public ReadOnlyDictionary<EnumName, IReadOnlyList<string>> AsReadOnly()
    {
        return enumDictionary.AsReadOnly();
    }
}
