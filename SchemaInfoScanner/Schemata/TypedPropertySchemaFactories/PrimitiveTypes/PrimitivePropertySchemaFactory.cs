using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;
using SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes.NullableTypes;
using SchemaInfoScanner.TypeCheckers;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemaFactories.PrimitiveTypes;

public static class PrimitivePropertySchemaFactory
{
    public static PropertySchemaBase Create(
        PropertyName propertyName,
        INamedTypeSymbol propertySymbol,
        IReadOnlyList<AttributeSyntax> attributeList)
    {
        if (!PrimitiveTypeChecker.IsSupportedPrimitiveType(propertySymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.FactoryNotSupportedPrimitiveType,
                propertyName,
                propertySymbol.Name));
        }

        var isNullable = propertySymbol.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T;
        var underlyingType = isNullable
            ? propertySymbol.TypeArguments[0]
            : propertySymbol;

        if (underlyingType.TypeKind is TypeKind.Enum)
        {
            return isNullable
                ? new NullableEnumPropertySchema(propertyName, propertySymbol, attributeList)
                : new EnumPropertySchema(propertyName, propertySymbol, attributeList);
        }

        if (PrimitiveTypeChecker.IsDateTimeType(underlyingType))
        {
            return isNullable
                ? new NullableDateTimePropertySchema(propertyName, propertySymbol, attributeList)
                : new DateTimePropertySchema(propertyName, propertySymbol, attributeList);
        }

        if (PrimitiveTypeChecker.IsTimeSpanType(underlyingType))
        {
            return isNullable
                ? new NullableTimeSpanPropertySchema(propertyName, propertySymbol, attributeList)
                : new TimeSpanPropertySchema(propertyName, propertySymbol, attributeList);
        }

        if (PrimitiveTypeChecker.IsClrPrimitiveType(underlyingType))
        {
            var clrName = PrimitiveTypeChecker.GetLastName(underlyingType);
            if (isNullable)
            {
                return clrName switch
                {
                    "Boolean" => new NullableBooleanPropertySchema(propertyName, propertySymbol, attributeList),
                    "Char" => new NullableCharPropertySchema(propertyName, propertySymbol, attributeList),
                    "SByte" => new NullableSBytePropertySchema(propertyName, propertySymbol, attributeList),
                    "Byte" => new NullableBytePropertySchema(propertyName, propertySymbol, attributeList),
                    "Int16" => new NullableInt16PropertySchema(propertyName, propertySymbol, attributeList),
                    "UInt16" => new NullableUInt16PropertySchema(propertyName, propertySymbol, attributeList),
                    "Int32" => new NullableInt32PropertySchema(propertyName, propertySymbol, attributeList),
                    "UInt32" => new NullableUInt32PropertySchema(propertyName, propertySymbol, attributeList),
                    "Int64" => new NullableInt64PropertySchema(propertyName, propertySymbol, attributeList),
                    "UInt64" => new NullableUInt64PropertySchema(propertyName, propertySymbol, attributeList),
                    "Single" => new NullableFloatPropertySchema(propertyName, propertySymbol, attributeList),
                    "Double" => new NullableDoublePropertySchema(propertyName, propertySymbol, attributeList),
                    "Decimal" => new NullableDecimalPropertySchema(propertyName, propertySymbol, attributeList),
                    "String" => new NullableStringPropertySchema(propertyName, propertySymbol, attributeList),
                    _ => throw new NotSupportedException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Messages.Composite.NotSupportedPrimitiveType,
                            propertySymbol.Name))
                };
            }
            else
            {
                return clrName switch
                {
                    "Boolean" => new BooleanPropertySchema(propertyName, propertySymbol, attributeList),
                    "Char" => new CharPropertySchema(propertyName, propertySymbol, attributeList),
                    "SByte" => new SBytePropertySchema(propertyName, propertySymbol, attributeList),
                    "Byte" => new BytePropertySchema(propertyName, propertySymbol, attributeList),
                    "Int16" => new Int16PropertySchema(propertyName, propertySymbol, attributeList),
                    "UInt16" => new UInt16PropertySchema(propertyName, propertySymbol, attributeList),
                    "Int32" => new Int32PropertySchema(propertyName, propertySymbol, attributeList),
                    "UInt32" => new UInt32PropertySchema(propertyName, propertySymbol, attributeList),
                    "Int64" => new Int64PropertySchema(propertyName, propertySymbol, attributeList),
                    "UInt64" => new UInt64PropertySchema(propertyName, propertySymbol, attributeList),
                    "Single" => new FloatPropertySchema(propertyName, propertySymbol, attributeList),
                    "Double" => new DoublePropertySchema(propertyName, propertySymbol, attributeList),
                    "Decimal" => new DecimalPropertySchema(propertyName, propertySymbol, attributeList),
                    "String" => new StringPropertySchema(propertyName, propertySymbol, attributeList),
                    _ => throw new NotSupportedException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Messages.Composite.NotSupportedPrimitiveType,
                            propertySymbol.Name))
                };
            }
        }

        if (isNullable)
        {
            return underlyingType.SpecialType switch
            {
                SpecialType.System_Boolean => new NullableBooleanPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Char => new NullableCharPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_SByte => new NullableSBytePropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Byte => new NullableBytePropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Int16 => new NullableInt16PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_UInt16 => new NullableUInt16PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Int32 => new NullableInt32PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_UInt32 => new NullableUInt32PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Int64 => new NullableInt64PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_UInt64 => new NullableUInt64PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Single => new NullableFloatPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Double => new NullableDoublePropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Decimal => new NullableDecimalPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_String => new NullableStringPropertySchema(propertyName, propertySymbol, attributeList),
                _ => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NotSupportedPrimitiveType,
                        propertySymbol.Name))
            };
        }
        else
        {
            return underlyingType.SpecialType switch
            {
                SpecialType.System_Boolean => new BooleanPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Char => new CharPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_SByte => new SBytePropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Byte => new BytePropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Int16 => new Int16PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_UInt16 => new UInt16PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Int32 => new Int32PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_UInt32 => new UInt32PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Int64 => new Int64PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_UInt64 => new UInt64PropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Single => new FloatPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Double => new DoublePropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_Decimal => new DecimalPropertySchema(propertyName, propertySymbol, attributeList),
                SpecialType.System_String => new StringPropertySchema(propertyName, propertySymbol, attributeList),
                _ => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NotSupportedPrimitiveType,
                        propertySymbol.Name))
            };
        }
    }
}
