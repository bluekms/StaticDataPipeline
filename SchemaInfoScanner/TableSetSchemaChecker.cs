using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace SchemaInfoScanner;

public static class TableSetSchemaChecker
{
    private const string ForeignKeyAttributeName = "ForeignKey";
    private const string SwitchForeignKeyAttributeName = "SwitchForeignKey";

    private const int ForeignKeyTableSetNameIndex = 0;
    private const int SwitchForeignKeyTableSetNameIndex = 2;

    public static void Check(RecordSchemaCatalog recordSchemaCatalog, ILogger logger)
    {
        var tableSetParameterNames = CollectTableSetParameterNames(recordSchemaCatalog);
        if (tableSetParameterNames.Count is 0)
        {
            return;
        }

        var errors = CollectErrors(recordSchemaCatalog, tableSetParameterNames);
        if (errors.Count is 0)
        {
            return;
        }

        foreach (var error in errors)
        {
            LogError(logger, error.Message, error);
        }

        throw new AggregateException(Messages.TableSetValidationFailed, errors);
    }

    private static HashSet<string> CollectTableSetParameterNames(RecordSchemaCatalog recordSchemaCatalog)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var recordSchema in recordSchemaCatalog.WholeRecordSchemata)
        {
            if (IsTableSetCandidate(recordSchema))
            {
                foreach (var property in recordSchema.PropertySchemata)
                {
                    names.Add(property.PropertyName.Name);
                }
            }
        }

        return names;
    }

    private static bool IsTableSetCandidate(RecordSchema recordSchema)
    {
        if (recordSchema.HasAttribute<StaticDataRecordAttribute>())
        {
            return false;
        }

        if (recordSchema.HasAttribute<IgnoreAttribute>())
        {
            return false;
        }

        if (recordSchema.PropertySchemata.Count is 0)
        {
            return false;
        }

        // TableSet 관행: 모든 파라미터를 nullable 테이블로 선언 (disabledTables 지원을 위해)
        return recordSchema.PropertySchemata.All(p => p.IsNullable());
    }

    private static List<Exception> CollectErrors(
        RecordSchemaCatalog recordSchemaCatalog,
        HashSet<string> tableSetParameterNames)
    {
        var errors = new List<Exception>();

        foreach (var recordSchema in recordSchemaCatalog.StaticDataRecordSchemata)
        {
            foreach (var property in recordSchema.PropertySchemata)
            {
                foreach (var attribute in property.AttributeList)
                {
                    var attributeName = attribute.Name.ToString();
                    if (attributeName is ForeignKeyAttributeName)
                    {
                        ValidateForeignKeyTableSetName(
                            attribute,
                            property.PropertyName.FullName,
                            tableSetParameterNames,
                            errors);
                    }
                    else if (attributeName is SwitchForeignKeyAttributeName)
                    {
                        ValidateSwitchForeignKeyTableSetName(
                            attribute,
                            property.PropertyName.FullName,
                            tableSetParameterNames,
                            errors);
                    }
                }
            }
        }

        return errors;
    }

    private static void ValidateForeignKeyTableSetName(
        AttributeSyntax attribute,
        string propertyFullName,
        HashSet<string> tableSetParameterNames,
        List<Exception> errors)
    {
        var args = GetStringArguments(attribute);
        if (args.Count <= ForeignKeyTableSetNameIndex)
        {
            return;
        }

        var tableSetName = args[ForeignKeyTableSetNameIndex];
        if (!tableSetParameterNames.Contains(tableSetName))
        {
            errors.Add(new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ForeignKeyTableSetNameNotFound,
                propertyFullName,
                tableSetName)));
        }
    }

    private static void ValidateSwitchForeignKeyTableSetName(
        AttributeSyntax attribute,
        string propertyFullName,
        HashSet<string> tableSetParameterNames,
        List<Exception> errors)
    {
        var args = GetStringArguments(attribute);
        if (args.Count <= SwitchForeignKeyTableSetNameIndex)
        {
            return;
        }

        var tableSetName = args[SwitchForeignKeyTableSetNameIndex];
        if (!tableSetParameterNames.Contains(tableSetName))
        {
            errors.Add(new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.SwitchForeignKeyTableSetNameNotFound,
                propertyFullName,
                tableSetName)));
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

    private static readonly Action<ILogger, string, Exception?> LogError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(0, nameof(LogError)), "{Message}");
}
