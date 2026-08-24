using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static partial class TableSetEmitter
{
    private static bool IsFormattable(ITypeSymbol type)
    {
        var underlying = TypeClassifier.UnwrapNullable(type);
        if (underlying.TypeKind == TypeKind.Enum)
        {
            return false;
        }

        return underlying.AllInterfaces.Any(i =>
            i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.IFormattable");
    }

    private static List<string> TryBuildTypedConditionLiterals(
        ITypeSymbol conditionType,
        List<FkBranch> switchBranches)
    {
        var literals = new List<string>(switchBranches.Count);
        foreach (var branch in switchBranches)
        {
            var built = TryBuildTypedConditionLiteral(conditionType, branch.ConditionValue!, out var literal);
            if (!built)
            {
                return new List<string>();
            }

            literals.Add(literal);
        }

        return literals;
    }

    private static bool TryBuildTypedConditionLiteral(
        ITypeSymbol conditionType,
        string conditionValue,
        out string literal)
    {
        literal = string.Empty;

        if (conditionType.TypeKind == TypeKind.Enum && conditionType is INamedTypeSymbol enumType)
        {
            var enumFullyQualifiedName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.IsConst && string.Equals(member.Name, conditionValue, StringComparison.Ordinal))
                {
                    literal = enumFullyQualifiedName + "." + EmitHelper.EscapeIdentifier(member.Name);

                    return true;
                }
            }

            if (SwitchForeignKeyConditionValueValidator.HasFlagsAttribute(enumType))
            {
                var parsedFlags = SwitchForeignKeyConditionValueValidator.TryParseFlagsConditionValue(
                    enumType,
                    conditionValue,
                    out var flagsNumericLiteral);
                if (!parsedFlags)
                {
                    return false;
                }

                literal = "(" + enumFullyQualifiedName + ")(" + flagsNumericLiteral + ")";

                return true;
            }

            var parsedEnumNumeric = long.TryParse(
                conditionValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var enumNumeric);
            if (!parsedEnumNumeric)
            {
                return false;
            }

            var underlyingSpecialType = enumType.EnumUnderlyingType!.SpecialType;
            if (!SwitchForeignKeyConditionValueValidator.FitsInIntegralType(underlyingSpecialType, enumNumeric))
            {
                return false;
            }

            literal = "(" + enumFullyQualifiedName + ")(" + enumNumeric.ToString(CultureInfo.InvariantCulture) + ")";

            return true;
        }

        switch (conditionType.SpecialType)
        {
            case SpecialType.System_String:
                literal = SymbolDisplay.FormatLiteral(conditionValue, quote: true);

                return true;

            case SpecialType.System_Char:
                if (conditionValue.Length != 1)
                {
                    return false;
                }

                literal = SymbolDisplay.FormatLiteral(conditionValue[0], quote: true);

                return true;

            case SpecialType.System_Boolean:
            {
                if (!bool.TryParse(conditionValue, out var boolValue))
                {
                    return false;
                }

                literal = boolValue ? "true" : "false";

                return true;
            }

            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            {
                var parsedSigned = long.TryParse(
                    conditionValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var signed);
                if (!parsedSigned)
                {
                    return false;
                }

                if (!SwitchForeignKeyConditionValueValidator.FitsInIntegralType(conditionType.SpecialType, signed))
                {
                    return false;
                }

                literal = signed.ToString(CultureInfo.InvariantCulture);

                return true;
            }

            case SpecialType.System_UInt64:
            {
                var parsedUnsigned = ulong.TryParse(
                    conditionValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var unsigned);
                if (!parsedUnsigned)
                {
                    return false;
                }

                literal = unsigned.ToString(CultureInfo.InvariantCulture);

                return true;
            }

            default:
                return false;
        }
    }
}
