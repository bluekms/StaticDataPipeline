using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace SchemaInfoScanner;

public static class ForeignKeySchemaChecker
{
    private const string ForeignKeyAttributeName = "ForeignKey";
    private const string SwitchForeignKeyAttributeName = "SwitchForeignKey";

    private const int ForeignKeyTableSetNameIndex = 0;
    private const int ForeignKeyRecordColumnNameIndex = 1;
    private const int SwitchForeignKeyConditionColumnNameIndex = 0;
    private const int SwitchForeignKeyTableSetNameIndex = 2;
    private const int SwitchForeignKeyRecordColumnNameIndex = 3;

    public static void Check(RecordSchemaCatalog recordSchemaCatalog, ILogger logger)
    {
        var tablePropertyMap = CollectTablePropertyMap(recordSchemaCatalog);
        var errors = CollectErrors(recordSchemaCatalog, tablePropertyMap);

        if (errors.Count is 0)
        {
            return;
        }

        foreach (var error in errors)
        {
            LogError(logger, error.Message, error);
        }

        throw new AggregateException(Messages.ForeignKeyValidationFailed, errors);
    }

    private static Dictionary<string, HashSet<string>> CollectTablePropertyMap(
        RecordSchemaCatalog recordSchemaCatalog)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var recordSchema in recordSchemaCatalog.WholeRecordSchemata)
        {
            if (!IsTableSetContainer(recordSchema))
            {
                continue;
            }

            foreach (var property in recordSchema.PropertySchemata)
            {
                var recordType = ExtractWrappedRecordType(property.NamedTypeSymbol);
                if (recordType is null)
                {
                    continue;
                }

                var targetSchema = recordSchemaCatalog.TryFind(recordType);
                if (targetSchema is null)
                {
                    continue;
                }

                var propertyNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var targetProperty in targetSchema.PropertySchemata)
                {
                    propertyNames.Add(targetProperty.PropertyName.Name);
                }

                map[property.PropertyName.Name] = propertyNames;
            }
        }

        return map;
    }

    private static bool IsTableSetContainer(RecordSchema recordSchema)
    {
        if (recordSchema.HasAttribute<StaticDataRecordAttribute>())
        {
            return false;
        }

        if (recordSchema.HasAttribute<IgnoreAttribute>())
        {
            return false;
        }

        return recordSchema.PropertySchemata.Count > 0;
    }

    private static List<Exception> CollectErrors(
        RecordSchemaCatalog recordSchemaCatalog,
        Dictionary<string, HashSet<string>> tablePropertyMap)
    {
        var errors = new List<Exception>();

        foreach (var recordSchema in recordSchemaCatalog.StaticDataRecordSchemata)
        {
            var selfPropertyNames = recordSchema.PropertySchemata
                .Select(p => p.PropertyName.Name)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var property in recordSchema.PropertySchemata)
            {
                foreach (var attribute in property.AttributeList)
                {
                    var attributeName = attribute.Name.ToString();
                    if (attributeName is ForeignKeyAttributeName)
                    {
                        ValidateForeignKey(attribute, property.PropertyName.FullName, tablePropertyMap, errors);
                    }
                    else if (attributeName is SwitchForeignKeyAttributeName)
                    {
                        ValidateSwitchForeignKey(
                            attribute,
                            property.PropertyName.FullName,
                            selfPropertyNames,
                            tablePropertyMap,
                            errors);
                    }
                }
            }
        }

        return errors;
    }

    private static void ValidateForeignKey(
        AttributeSyntax attribute,
        string propertyFullName,
        Dictionary<string, HashSet<string>> tablePropertyMap,
        List<Exception> errors)
    {
        var args = GetStringArguments(attribute);
        if (args.Count <= ForeignKeyRecordColumnNameIndex)
        {
            return;
        }

        var tableSetName = args[ForeignKeyTableSetNameIndex];
        var recordColumnName = args[ForeignKeyRecordColumnNameIndex];

        if (!tablePropertyMap.TryGetValue(tableSetName, out var propertyNames))
        {
            return;
        }

        if (!propertyNames.Contains(recordColumnName))
        {
            errors.Add(new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ForeignKeyColumnNotFound,
                propertyFullName,
                recordColumnName)));
        }
    }

    private static void ValidateSwitchForeignKey(
        AttributeSyntax attribute,
        string propertyFullName,
        HashSet<string> selfPropertyNames,
        Dictionary<string, HashSet<string>> tablePropertyMap,
        List<Exception> errors)
    {
        var args = GetStringArguments(attribute);
        if (args.Count <= SwitchForeignKeyRecordColumnNameIndex)
        {
            return;
        }

        var conditionColumnName = args[SwitchForeignKeyConditionColumnNameIndex];
        var tableSetName = args[SwitchForeignKeyTableSetNameIndex];
        var recordColumnName = args[SwitchForeignKeyRecordColumnNameIndex];

        if (!selfPropertyNames.Contains(conditionColumnName))
        {
            errors.Add(new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SwitchForeignKeyConditionColumnNotFound,
                propertyFullName,
                conditionColumnName,
                GetOwnerRecordName(propertyFullName))));
        }

        if (!tablePropertyMap.TryGetValue(tableSetName, out var propertyNames))
        {
            return;
        }

        if (!propertyNames.Contains(recordColumnName))
        {
            errors.Add(new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SwitchForeignKeyColumnNotFound,
                propertyFullName,
                recordColumnName)));
        }
    }

    private static List<string> GetStringArguments(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList is null)
        {
            return [];
        }

        return attribute.ArgumentList.Arguments
            .Select(x => x.Expression switch
            {
                LiteralExpressionSyntax literal => literal.Token.ValueText,
                _ => string.Empty,
            })
            .ToList();
    }

    private static string GetOwnerRecordName(string propertyFullName)
    {
        var lastDot = propertyFullName.LastIndexOf('.');
        return lastDot >= 0 ? propertyFullName[..lastDot] : propertyFullName;
    }

    private static INamedTypeSymbol? ExtractWrappedRecordType(INamedTypeSymbol type)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType &&
                string.Equals(current.OriginalDefinition.Name, "StaticDataTable", StringComparison.Ordinal))
            {
                if (current.TypeArguments.Length >= 2 &&
                    current.TypeArguments[1] is INamedTypeSymbol recordType)
                {
                    return recordType;
                }
            }

            current = current.BaseType;
        }

        return null;
    }

    private static readonly Action<ILogger, string, Exception?> LogError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{Message}");
}
