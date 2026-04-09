using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata;

namespace SchemaInfoScanner.TypeCheckers;

internal static class PrimitiveTypeChecker
{
    public static void Check(PropertySchemaBase property)
    {
        if (!IsSupportedPrimitiveType(property.NamedTypeSymbol))
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.NotSupportedPrimitiveType,
                property.PropertyName.FullName));
        }
    }

    public static bool IsSupportedPrimitiveType(INamedTypeSymbol symbol)
    {
        var isNullable = symbol.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T;
        var underlyingType = isNullable
            ? symbol.TypeArguments[0]
            : symbol;

        if (CheckSpecialType(underlyingType))
        {
            return true;
        }

        if (CheckEnumType(underlyingType))
        {
            return true;
        }

        if (IsDateTimeType(underlyingType))
        {
            return true;
        }

        if (IsTimeSpanType(underlyingType))
        {
            return true;
        }

        return IsClrPrimitiveType(underlyingType);
    }

    public static bool IsDateTimeType(ITypeSymbol symbol)
    {
        return GetLastName(symbol) is "DateTime" or "DateTime?";
    }

    public static bool IsTimeSpanType(ITypeSymbol symbol)
    {
        return GetLastName(symbol) is "TimeSpan" or "TimeSpan?";
    }

    public static bool IsClrPrimitiveType(ITypeSymbol symbol)
    {
        var name = GetLastName(symbol);
        return ClrPrimitiveTypes.Contains(name);
    }

    public static string GetLastName(ITypeSymbol symbol)
    {
        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }

    private static bool CheckSpecialType(ITypeSymbol symbol)
    {
        return symbol.SpecialType switch
        {
            SpecialType.System_Boolean => true,
            SpecialType.System_Char => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_Decimal => true,
            SpecialType.System_String => true,
            _ => false
        };
    }

    private static readonly HashSet<string> ClrPrimitiveTypes = new(
        [
            "Boolean",
            "Byte",
            "SByte",
            "Char",
            "Decimal",
            "Double",
            "Single",
            "Int16",
            "Int32",
            "Int64",
            "UInt16",
            "UInt32",
            "UInt64",
            "String",
            "Boolean?",
            "Byte?",
            "SByte?",
            "Char?",
            "Decimal?",
            "Double?",
            "Single?",
            "Int16?",
            "Int32?",
            "Int64?",
            "UInt16?",
            "UInt32?",
            "UInt64?",
            "String?",
        ],
        StringComparer.Ordinal);

    private static bool CheckEnumType(ITypeSymbol symbol)
    {
        return symbol.TypeKind is TypeKind.Enum;
    }
}
