using System.Globalization;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using SchemaInfoScanner.Schemata.SchemaValidators;
using Sdp.Attributes;

namespace SchemaInfoScanner.TypeCheckers;

internal static class SupportedTypeChecker
{
    public static void Check(
        PropertySchemaBase property,
        RecordSchemaCatalog recordSchemaCatalog,
        HashSet<RecordName> visited,
        ILogger logger)
    {
        if (property.HasAttribute<IgnoreAttribute>())
        {
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.Ignored,
                property.PropertyName.FullName);
            LogTrace(logger, msg, null);
            return;
        }

        LogTrace(logger, property.PropertyName.FullName, null);

        var validator = new SchemaRuleValidator();
        var validateResult = validator.Validate(property);
        if (!validateResult.IsValid)
        {
            var errorMessage = string.Join(", ", validateResult.Errors.Select(e => e.ErrorMessage));
            throw new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedRecordTypeWithMessage,
                property.PropertyName.FullName,
                errorMessage));
        }

        if (PrimitiveTypeChecker.IsSupportedPrimitiveType(property.NamedTypeSymbol))
        {
            PrimitiveTypeChecker.Check(property);
            return;
        }

        if (CollectionTypeChecker.IsSupportedCollectionType(property.NamedTypeSymbol))
        {
            CheckSupportedCollectionType(property, recordSchemaCatalog, visited, logger);
            return;
        }

        var recordSchema = recordSchemaCatalog.TryFind(property.NamedTypeSymbol);
        if (recordSchema is null)
        {
            var innerException = new KeyNotFoundException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotFoundInRecordSchemaDictionary,
                property.NamedTypeSymbol.Name));

            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedRecordType,
                property.PropertyName.FullName);
            throw new NotSupportedException(msg, innerException);
        }

        RecordTypeChecker.Check(recordSchema, recordSchemaCatalog, visited, logger);
    }

    private static void CheckSupportedCollectionType(
        PropertySchemaBase property,
        RecordSchemaCatalog recordSchemaCatalog,
        HashSet<RecordName> visited,
        ILogger logger)
    {
        if (SetTypeChecker.IsSupportedSetType(property.NamedTypeSymbol))
        {
            SetTypeChecker.Check(property, recordSchemaCatalog, visited, logger);
        }
        else if (ArrayTypeChecker.IsSupportedArrayType(property.NamedTypeSymbol))
        {
            ArrayTypeChecker.Check(property, recordSchemaCatalog, visited, logger);
        }
        else if (MapTypeChecker.IsSupportedMapType(property.NamedTypeSymbol))
        {
            MapTypeChecker.Check(property, recordSchemaCatalog, visited, logger);
        }
        else
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedCollectionType,
                property.PropertyName.FullName));
        }
    }

    private static readonly Action<ILogger, string, Exception?> LogTrace =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(0), "{Message}");
}
