using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SchemaInfoScanner.NameObjects;

namespace SchemaInfoScanner.Collectors;

public class SemanticModelSet
{
    private readonly Dictionary<RecordName, SemanticModel> semanticModelDictionary = [];

    public SemanticModelSet(RecordSchemaLoader.Result loadResult)
    {
        Collect(loadResult);
    }

    public SemanticModelSet(IReadOnlyList<RecordSchemaLoader.Result> loadResults)
    {
        foreach (var loadResult in loadResults)
        {
            Collect(loadResult);
        }
    }

    private void Collect(RecordSchemaLoader.Result loadResult)
    {
        foreach (var recordDeclaration in loadResult.RecordDeclarationList)
        {
            var recordSymbol = loadResult.SemanticModel.GetDeclaredSymbol(recordDeclaration);
            if (recordSymbol is null)
            {
                throw new NotSupportedException(recordDeclaration.Identifier.ValueText);
            }

            semanticModelDictionary.Add(new RecordName(recordSymbol), loadResult.SemanticModel);
        }
    }

    public IReadOnlyDictionary<RecordName, SemanticModel> GetSemanticModels()
    {
        return semanticModelDictionary;
    }
}
