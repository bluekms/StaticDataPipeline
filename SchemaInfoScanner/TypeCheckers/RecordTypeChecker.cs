using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var ignoredMsg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.Ignored,
                recordSchema.RecordName.FullName);
            LogTrace(logger, ignoredMsg, null);

            return;
        }

        if (!IsSupportedRecordType(recordSchema.NamedTypeSymbol))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.NotSupportedRecordType,
                    recordSchema.RecordName.FullName));
        }

        if (!visited.Add(recordSchema.RecordName))
        {
            var visitedMsg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.AlreadyVisited,
                recordSchema.RecordName.FullName);
            LogTrace(logger, visitedMsg, null);

            return;
        }

        var startedMsg = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.RecordStarted,
            recordSchema.RecordName.FullName);
        LogTrace(logger, startedMsg, null);

        foreach (var recordParameterSchema in recordSchema.PropertySchemata)
        {
            SupportedTypeChecker.Check(recordParameterSchema, recordSchemaCatalog, visited, logger);
        }

        var finishedMsg = string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.RecordFinished,
            recordSchema.RecordName.FullName);
        LogTrace(logger, finishedMsg, null);
    }

    public static bool IsSupportedRecordType(INamedTypeSymbol symbol)
    {
        var methodSymbols = symbol
            .GetMembers().OfType<IMethodSymbol>()
            .Select(x => x.Name);

        return !RecordMethodNames.Except(methodSymbols).Any();
    }

    // 생성기의 NestedRecordInfo.IsTypeBrandingRecord 와 같은 기준이어야 한다. [Ignore] 파라미터도 생성자 파라미터로 센다.
    public static bool IsSingleValueRecord(INamedTypeSymbol recordSymbol, IReadOnlyList<PropertySchemaBase> members)
    {
        if (members.Count != 1 || !PrimitiveTypeChecker.IsSupportedPrimitiveType(members[0].NamedTypeSymbol))
        {
            return false;
        }

        var primaryConstructor = recordSymbol.InstanceConstructors
            .Where(x => !x.IsImplicitlyDeclared)
            .FirstOrDefault(IsPrimaryConstructor);

        return primaryConstructor is { Parameters.Length: 1 };
    }

    private static bool IsPrimaryConstructor(IMethodSymbol constructor)
    {
        return constructor.DeclaringSyntaxReferences
            .Select(x => x.GetSyntax())
            .Any(x => x is RecordDeclarationSyntax { ParameterList: not null });
    }

    public static INamedTypeSymbol UnwrapNullable(INamedTypeSymbol symbol)
    {
        if (symbol.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T &&
            symbol.TypeArguments is [INamedTypeSymbol underlying])
        {
            return underlying;
        }

        return symbol;
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
            var innerException = new KeyNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.RecordSchemaTypeArgNotFound,
                    symbol.Name));
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedType,
                symbol.Name);
            throw new NotSupportedException(msg, innerException);
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
