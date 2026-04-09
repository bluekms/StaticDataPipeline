using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;
using Sdp.Attributes;

namespace SchemaInfoScanner.TypeCheckers;

public static class MapTypeChecker
{
    private static readonly HashSet<string> SupportedTypeNames =
    [
        "FrozenDictionary<, >"
    ];

    public static void Check(
        PropertySchemaBase property,
        RecordSchemaCatalog recordSchemaCatalog,
        HashSet<RecordName> visited,
        ILogger logger)
    {
        if (!IsSupportedMapType(property.NamedTypeSymbol))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.ExpectedDictionaryType,
                property.PropertyName.FullName));
        }

        var keySymbol = (INamedTypeSymbol)property.NamedTypeSymbol.TypeArguments[0];
        if (keySymbol.NullableAnnotation is NullableAnnotation.Annotated)
        {
            throw new NotSupportedException(Messages.DictionaryKeyMustBeNonNullable);
        }

        var valueSymbol = (INamedTypeSymbol)property.NamedTypeSymbol.TypeArguments[1];

        if (PrimitiveTypeChecker.IsSupportedPrimitiveType(valueSymbol))
        {
            return;
        }

        if (valueSymbol.NullableAnnotation is NullableAnnotation.Annotated)
        {
            throw new NotSupportedException(Messages.DictionaryValueMustBeNonNullable);
        }

        var valueRecordSchema = RecordTypeChecker.CheckAndGetSchema(valueSymbol, recordSchemaCatalog, visited, logger);

        var valueRecordKeyParameterSchema = valueRecordSchema.PropertySchemata
            .SingleOrDefault(x => x.HasAttribute<KeyAttribute>());

        if (valueRecordKeyParameterSchema is null)
        {
            throw new InvalidAttributeUsageException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.KeyAttributeRequiredInDictionaryValue,
                valueRecordSchema.RecordName.FullName,
                property.PropertyName.FullName));
        }

        if (RecordTypeChecker.IsSupportedRecordType(keySymbol))
        {
            var keyRecordSchema = RecordTypeChecker.CheckAndGetSchema(keySymbol, recordSchemaCatalog, visited, logger);

            var valueRecordKeyParameterRecordName = new RecordName(valueRecordKeyParameterSchema.NamedTypeSymbol);
            if (!keyRecordSchema.RecordName.Equals(valueRecordKeyParameterRecordName))
            {
                throw new NotSupportedException(Messages.DictionaryKeyValueMustBeSameType);
            }

            return;
        }

        CheckSamePrimitiveType(keySymbol, valueRecordKeyParameterSchema.NamedTypeSymbol);
    }

    public static bool IsSupportedMapType(INamedTypeSymbol symbol)
    {
        if (symbol.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T ||
            symbol.TypeArguments is not [INamedTypeSymbol, INamedTypeSymbol])
        {
            return false;
        }

        var genericTypeDefinitionName = symbol
            .ConstructUnboundGenericType()
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return SupportedTypeNames.Contains(genericTypeDefinitionName);
    }

    public static bool IsPrimitiveKeyRecordValueMapType(INamedTypeSymbol symbol)
    {
        if (!IsSupportedMapType(symbol))
        {
            return false;
        }

        var keySymbol = (INamedTypeSymbol)symbol.TypeArguments[0];
        if (keySymbol.NullableAnnotation is NullableAnnotation.Annotated)
        {
            throw new NotSupportedException(Messages.DictionaryKeyMustBeNonNullable);
        }

        var valueSymbol = (INamedTypeSymbol)symbol.TypeArguments[1];
        if (valueSymbol.NullableAnnotation is NullableAnnotation.Annotated)
        {
            throw new NotSupportedException(Messages.DictionaryValueMustBeNonNullable);
        }

        return PrimitiveTypeChecker.IsSupportedPrimitiveType(keySymbol) &&
               RecordTypeChecker.IsSupportedRecordType(valueSymbol);
    }

    public static bool IsRecordKeyAndValueMapType(INamedTypeSymbol symbol)
    {
        if (!IsSupportedMapType(symbol))
        {
            return false;
        }

        var keySymbol = (INamedTypeSymbol)symbol.TypeArguments[0];
        if (keySymbol.NullableAnnotation is NullableAnnotation.Annotated)
        {
            throw new NotSupportedException(Messages.DictionaryKeyMustBeNonNullable);
        }

        var valueSymbol = (INamedTypeSymbol)symbol.TypeArguments[1];
        if (valueSymbol.NullableAnnotation is NullableAnnotation.Annotated)
        {
            throw new NotSupportedException(Messages.DictionaryValueMustBeNonNullable);
        }

        return RecordTypeChecker.IsSupportedRecordType(keySymbol) &&
               RecordTypeChecker.IsSupportedRecordType(valueSymbol);
    }

    public static bool HasDateTimeProperty(INamedTypeSymbol symbol)
    {
        if (!IsSupportedMapType(symbol))
        {
            return false;
        }

        var keyTypeArgument = symbol.TypeArguments.First();
        if (PrimitiveTypeChecker.IsDateTimeType(keyTypeArgument))
        {
            return true;
        }

        var valueTypeArgument = symbol.TypeArguments.Last();
        if (PrimitiveTypeChecker.IsDateTimeType(valueTypeArgument))
        {
            return true;
        }

        return valueTypeArgument
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.IsStatic)
            .Where(x => symbol.DeclaredAccessibility == Accessibility.Public)
            .Select(x => x.Type)
            .Any(PrimitiveTypeChecker.IsDateTimeType);
    }

    public static bool HasTimeSpanProperty(INamedTypeSymbol symbol)
    {
        if (!IsSupportedMapType(symbol))
        {
            return false;
        }

        var keyTypeArgument = symbol.TypeArguments.First();
        if (PrimitiveTypeChecker.IsTimeSpanType(keyTypeArgument))
        {
            return true;
        }

        var valueTypeArgument = symbol.TypeArguments.Last();
        if (PrimitiveTypeChecker.IsTimeSpanType(valueTypeArgument))
        {
            return true;
        }

        return valueTypeArgument
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.IsStatic)
            .Where(x => symbol.DeclaredAccessibility == Accessibility.Public)
            .Select(x => x.Type)
            .Any(PrimitiveTypeChecker.IsTimeSpanType);
    }

    private static void CheckSamePrimitiveType(INamedTypeSymbol keySymbol, INamedTypeSymbol valueSymbol)
    {
        if (keySymbol.SpecialType is not SpecialType.None &&
            keySymbol.SpecialType == valueSymbol.SpecialType)
        {
            return;
        }

        if (keySymbol.TypeKind is not TypeKind.Enum || valueSymbol.TypeKind is not TypeKind.Enum)
        {
            throw new NotSupportedException(Messages.DictionaryKeyValueMustBeSameType);
        }

        if (keySymbol.Name != valueSymbol.Name)
        {
            throw new NotSupportedException(Messages.DictionaryKeyValueMustBeSameType);
        }
    }
}
