using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.TypedPropertySchemaFactories;

namespace SchemaInfoScanner.Collectors;

public sealed class RecordSchemaSet
{
    private readonly Dictionary<RecordName, INamedTypeSymbol> recordNamedTypeSymbolDictionary = [];
    private readonly Dictionary<RecordName, List<AttributeSyntax>> recordAttributeDictionary = [];
    private readonly Dictionary<RecordName, List<PropertySchemaBase>> recordMemberSchemaDictionary = [];

    public int Count => recordAttributeDictionary.Count;

    public IReadOnlyList<RecordName> RecordNames => recordAttributeDictionary.Keys.ToList();

    public RecordSchemaSet(RecordSchemaLoader.Result loadResult, ILogger logger)
    {
        try
        {
            Collect(loadResult);
        }
        catch (NotSupportedException e)
        {
            logger.LogWarning(e, e.Message);
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }

    public RecordSchemaSet(IReadOnlyList<RecordSchemaLoader.Result> loadResults, ILogger logger)
    {
        var errorCount = 0;
        foreach (var loadResult in loadResults)
        {
            try
            {
                Collect(loadResult);
            }
            catch (NotSupportedException e)
            {
                ++errorCount;
                logger.LogWarning(e, e.Message);
            }
            catch (Exception e)
            {
                ++errorCount;
                logger.LogError(e, e.Message);
            }
        }

        if (errorCount > 0)
        {
            throw new AggregateException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ExceptionCountWhileCollecting,
                errorCount));
        }
    }

    private void Collect(RecordSchemaLoader.Result loadResult)
    {
        var result = Parse(loadResult);

        foreach (var (recordName, namedTypeSymbol) in result.RecordNamedTypeSymbolCollector)
        {
            recordNamedTypeSymbolDictionary.Add(recordName, namedTypeSymbol);
        }

        foreach (var (recordName, recordAttributes) in result.RecordAttributeCollector)
        {
            recordAttributeDictionary.Add(recordName, recordAttributes.ToList());
        }

        foreach (var propertyName in result.ParameterNamedTypeSymbolCollector.ParameterNames)
        {
            var propertySymbol = result.ParameterNamedTypeSymbolCollector[propertyName];
            var attributes = result.ParameterAttributeCollector[propertyName];
            var parentRecordSymbol = result.RecordNamedTypeSymbolCollector[propertyName.RecordName];

            var parameterSchema = TypedPropertySchemaFactory.Create(
                propertyName,
                propertySymbol,
                attributes,
                parentRecordSymbol);

            var recordName = propertyName.RecordName;
            if (recordMemberSchemaDictionary.TryGetValue(recordName, out var recordMembers))
            {
                recordMembers.Add(parameterSchema);
            }
            else
            {
                recordMemberSchemaDictionary.Add(recordName, [parameterSchema]);
            }
        }
    }

    public IReadOnlyList<AttributeSyntax> GetRecordAttributes(RecordName recordName)
    {
        return recordAttributeDictionary[recordName];
    }

    public IReadOnlyList<PropertySchemaBase> GetRecordMemberSchemata(RecordName recordName)
    {
        if (recordMemberSchemaDictionary.TryGetValue(recordName, out var recordMembers))
        {
            return recordMembers;
        }

        return [];
    }

    public INamedTypeSymbol GetNamedTypeSymbol(RecordName recordName)
    {
        return recordNamedTypeSymbolDictionary[recordName];
    }

    private static ParseResult Parse(RecordSchemaLoader.Result loadResult)
    {
        var recordNamedTypeSymbolCollector = new Dictionary<RecordName, INamedTypeSymbol>();
        var recordAttributeCollector = new RecordAttributeCollector();
        var parameterAttributeCollector = new ParameterAttributeCollector();
        var parameterNamedTypeSymbolCollector = new ParameterNamedTypeSymbolCollector(loadResult.SemanticModel);

        foreach (var recordDeclaration in loadResult.RecordDeclarationList)
        {
            var recordName = new RecordName(recordDeclaration);
            if (loadResult.SemanticModel.GetDeclaredSymbol(recordDeclaration) is not INamedTypeSymbol namedTypeSymbol)
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.RecordNotNamedTypeSymbol,
                    recordName.FullName));
            }

            recordNamedTypeSymbolCollector.Add(recordName, namedTypeSymbol);

            recordAttributeCollector.Collect(recordDeclaration);

            if (recordDeclaration.ParameterList is null)
            {
                continue;
            }

            foreach (var parameter in recordDeclaration.ParameterList.Parameters)
            {
                if (string.IsNullOrEmpty(parameter.Identifier.ValueText))
                {
                    continue;
                }

                parameterAttributeCollector.Collect(recordDeclaration, parameter);
                parameterNamedTypeSymbolCollector.Collect(recordDeclaration, parameter);
            }
        }

        return new(
            recordNamedTypeSymbolCollector,
            recordAttributeCollector,
            parameterAttributeCollector,
            parameterNamedTypeSymbolCollector);
    }

    private sealed class ParseResult
    {
        public Dictionary<RecordName, INamedTypeSymbol> RecordNamedTypeSymbolCollector { get; }
        public RecordAttributeCollector RecordAttributeCollector { get; }
        public ParameterAttributeCollector ParameterAttributeCollector { get; }
        public ParameterNamedTypeSymbolCollector ParameterNamedTypeSymbolCollector { get; }

        public ParseResult(
            Dictionary<RecordName, INamedTypeSymbol> recordNamedTypeSymbolCollector,
            RecordAttributeCollector recordAttributeCollector,
            ParameterAttributeCollector parameterAttributeCollector,
            ParameterNamedTypeSymbolCollector parameterNamedTypeSymbolCollector)
        {
            if (parameterNamedTypeSymbolCollector.Count != parameterAttributeCollector.Count)
            {
                throw new ArgumentException(Messages.ParseResultCountMismatch);
            }

            foreach (var parameterFullName in parameterNamedTypeSymbolCollector.ParameterNames)
            {
                if (!parameterAttributeCollector.ContainsRecord(parameterFullName))
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.ParseResultParameterNotFound,
                        parameterFullName));
                }
            }

            RecordNamedTypeSymbolCollector = recordNamedTypeSymbolCollector;
            RecordAttributeCollector = recordAttributeCollector;
            ParameterAttributeCollector = parameterAttributeCollector;
            ParameterNamedTypeSymbolCollector = parameterNamedTypeSymbolCollector;
        }
    }
}
