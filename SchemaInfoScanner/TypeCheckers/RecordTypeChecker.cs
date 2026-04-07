using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace SchemaInfoScanner.TypeCheckers;

internal static class RecordTypeChecker
{
    public static void Check(
        RecordSchema recordSchema,
        RecordSchemaCatalog recordSchemaCatalog,
        HashSet<RecordName> visited,
        ILogger logger)
    {
        if (recordSchema.HasAttribute<IgnoreAttribute>())
        {
            LogTrace(logger, Messages.Ignored(recordSchema.RecordName.FullName), null);
            return;
        }

        if (!IsSupportedRecordType(recordSchema.NamedTypeSymbol))
        {
            throw new InvalidOperationException($"{recordSchema.RecordName.FullName} is not supported record type.");
        }

        if (!visited.Add(recordSchema.RecordName))
        {
            LogTrace(logger, Messages.AlreadyVisited(recordSchema.RecordName.FullName), null);
            return;
        }

        LogTrace(logger, Messages.RecordStarted(recordSchema.RecordName.FullName), null);

        foreach (var recordParameterSchema in recordSchema.PropertySchemata)
        {
            SupportedTypeChecker.Check(recordParameterSchema, recordSchemaCatalog, visited, logger);
        }

        LogTrace(logger, Messages.RecordFinished(recordSchema.RecordName.FullName), null);
    }

    public static bool IsSupportedRecordType(INamedTypeSymbol symbol)
    {
        var methodSymbols = symbol
            .GetMembers().OfType<IMethodSymbol>()
            .Select(x => x.Name);

        return !RecordMethodNames.Except(methodSymbols).Any();
    }

    public static bool TryFindNestedRecordTypeSymbol(
        INamedTypeSymbol recordSymbol,
        ITypeSymbol propertySymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? nestedRecordSymbol)
    {
        nestedRecordSymbol = null;

        if (propertySymbol is not IErrorTypeSymbol errorType)
        {
            return false;
        }

        var candidate = recordSymbol
            .GetTypeMembers()
            .FirstOrDefault(x => x.Name == errorType.Name);

        if (candidate is not null && IsSupportedRecordType(candidate))
        {
            nestedRecordSymbol = candidate;
            return nestedRecordSymbol.IsRecord;
        }

        return false;
    }

    public static RecordSchema CheckAndGetSchema(
        INamedTypeSymbol symbol,
        RecordSchemaCatalog recordSchemaCatalog,
        HashSet<RecordName> visited,
        ILogger logger)
    {
        var recordSchema = recordSchemaCatalog.TryFind(symbol);
        if (recordSchema is null)
        {
            var innerException = new KeyNotFoundException($"{symbol.Name} is not found in the RecordSchemaDictionary");
            throw new NotSupportedException($"{symbol.Name} is not supported type.", innerException);
        }

        Check(recordSchema, recordSchemaCatalog, visited, logger);
        return recordSchema;
    }

    private static readonly string[] RecordMethodNames = [
        "Equals",
        "GetHashCode",
        "ToString",
        "PrintMembers"
    ];

    private static readonly Action<ILogger, string, Exception?> LogTrace =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(0), "{Message}");
}
