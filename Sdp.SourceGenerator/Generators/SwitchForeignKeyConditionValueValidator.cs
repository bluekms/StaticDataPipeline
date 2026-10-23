using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class SwitchForeignKeyConditionValueValidator
{
    public static void Validate(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var firstArgs = switchFkAttrs[0].ConstructorArguments;
        if (firstArgs.Length < 1 || firstArgs[0].Value is not string conditionColumn)
        {
            return;
        }

        var conditionProperty = recordType.GetMembers(conditionColumn).OfType<IPropertySymbol>().FirstOrDefault();
        if (conditionProperty is null)
        {
            return;
        }

        var conditionType = TypeClassifier.UnwrapNullable(conditionProperty.Type);
        var conditionCollection = TypeClassifier.ClassifyCollection(conditionType);
        if (conditionCollection is not null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyConditionColumnIsCollection,
                ParameterLocation(param),
                recordType.Name,
                conditionColumn,
                conditionType.ToDisplayString()));

            return;
        }

        if (conditionType.TypeKind == TypeKind.Enum && conditionType is INamedTypeSymbol enumType)
        {
            if (HasFlagsAttribute(enumType))
            {
                ValidateFlagsEnumConditionValue(
                    recordType, param, switchFkAttrs, conditionColumn, enumType, diagnostics);
            }
            else
            {
                ValidateEnumConditionValue(
                    recordType, param, switchFkAttrs, conditionColumn, enumType, diagnostics);
            }
        }
        else if (IsIntegralType(conditionType.SpecialType))
        {
            ValidateIntegralConditionValue(
                recordType, param, switchFkAttrs, conditionColumn, conditionType, diagnostics);
        }
        else if (conditionType.SpecialType == SpecialType.System_Boolean)
        {
            ValidateBoolConditionValue(recordType, param, switchFkAttrs, conditionColumn, diagnostics);
        }
        else if (conditionType.SpecialType == SpecialType.System_Char)
        {
            ValidateCharConditionValue(recordType, param, switchFkAttrs, conditionColumn, diagnostics);
        }
        else if (TypeClassifier.ClassifyScalar(conditionType) == ScalarKind.Guid)
        {
            ValidateGuidConditionValue(recordType, param, switchFkAttrs, conditionColumn, diagnostics);
        }
    }

    private static void ValidateEnumConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        string conditionColumn,
        INamedTypeSymbol enumType,
        List<Diagnostic> diagnostics)
    {
        var memberNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsConst)
            {
                memberNames.Add(member.Name);
            }
        }

        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 2 || args[1].Value is not string conditionValue)
            {
                continue;
            }

            if (memberNames.Contains(conditionValue) || IsParsableEnumNumeric(enumType, conditionValue))
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyConditionValueInvalid,
                ParameterLocation(param),
                recordType.Name,
                conditionValue,
                conditionColumn,
                enumType.ToDisplayString()));
        }
    }

    private static void ValidateFlagsEnumConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        string conditionColumn,
        INamedTypeSymbol enumType,
        List<Diagnostic> diagnostics)
    {
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 2 || args[1].Value is not string conditionValue)
            {
                continue;
            }

            if (TryParseFlagsConditionValue(enumType, conditionValue, out _))
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyConditionValueInvalid,
                ParameterLocation(param),
                recordType.Name,
                conditionValue,
                conditionColumn,
                enumType.ToDisplayString()));
        }
    }

    private static void ValidateIntegralConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        string conditionColumn,
        ITypeSymbol conditionType,
        List<Diagnostic> diagnostics)
    {
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 2 || args[1].Value is not string conditionValue)
            {
                continue;
            }

            if (conditionType.SpecialType == SpecialType.System_UInt64)
            {
                if (ulong.TryParse(
                    conditionValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out _))
                {
                    continue;
                }
            }
            else
            {
                var parsed = long.TryParse(
                    conditionValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var numeric);
                if (parsed && FitsInIntegralType(conditionType.SpecialType, numeric))
                {
                    continue;
                }
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyIntegralConditionValueInvalid,
                ParameterLocation(param),
                recordType.Name,
                conditionValue,
                conditionColumn,
                conditionType.ToDisplayString()));
        }
    }

    private static void ValidateBoolConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        string conditionColumn,
        List<Diagnostic> diagnostics)
    {
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 2 || args[1].Value is not string conditionValue)
            {
                continue;
            }

            if (bool.TryParse(conditionValue, out _))
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyBoolConditionValueInvalid,
                ParameterLocation(param),
                recordType.Name,
                conditionValue,
                conditionColumn));
        }
    }

    private static void ValidateCharConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        string conditionColumn,
        List<Diagnostic> diagnostics)
    {
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 2 || args[1].Value is not string conditionValue)
            {
                continue;
            }

            if (conditionValue.Length == 1)
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyCharConditionValueInvalid,
                ParameterLocation(param),
                recordType.Name,
                conditionValue,
                conditionColumn));
        }
    }

    private static void ValidateGuidConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        string conditionColumn,
        List<Diagnostic> diagnostics)
    {
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 2 || args[1].Value is not string conditionValue)
            {
                continue;
            }

            if (Guid.TryParse(conditionValue, out _))
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyGuidConditionValueInvalid,
                ParameterLocation(param),
                recordType.Name,
                conditionValue,
                conditionColumn));
        }
    }

    public static bool HasFlagsAttribute(INamedTypeSymbol enumType)
    {
        foreach (var attr in enumType.GetAttributes())
        {
            if (attr.AttributeClass is not { } attributeClass)
            {
                continue;
            }

            var attributeName = attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attributeName == "global::System.FlagsAttribute")
            {
                return true;
            }
        }

        return false;
    }

    // [Flags] enum Permission: int { Read = 1, Write = 2, Admin = 4 }
    // "Read, Write": true, 3
    // "5": true, 5 (미정의 조합도 값이면 성공)
    // "Read, Execute": false
    public static bool TryParseFlagsConditionValue(
        INamedTypeSymbol enumType,
        string conditionValue,
        out string numericLiteral)
    {
        numericLiteral = string.Empty;

        var memberValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsConst)
            {
                memberValues[member.Name] = member.ConstantValue;
            }
        }

        if (enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64)
        {
            var accumulator = 0UL;
            foreach (var token in conditionValue.Split(','))
            {
                var trimmedToken = token.Trim();
                if (memberValues.TryGetValue(trimmedToken, out var memberValue))
                {
                    accumulator |= Convert.ToUInt64(memberValue, CultureInfo.InvariantCulture);

                    continue;
                }

                var parsed = ulong.TryParse(
                    trimmedToken,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var numeric);
                if (!parsed)
                {
                    return false;
                }

                accumulator |= numeric;
            }

            numericLiteral = accumulator.ToString(CultureInfo.InvariantCulture);

            return true;
        }

        var signedAccumulator = 0L;
        foreach (var token in conditionValue.Split(','))
        {
            var trimmedToken = token.Trim();
            if (memberValues.TryGetValue(trimmedToken, out var memberValue))
            {
                signedAccumulator |= Convert.ToInt64(memberValue, CultureInfo.InvariantCulture);

                continue;
            }

            var parsed = long.TryParse(
                trimmedToken,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var numeric);
            if (!parsed)
            {
                return false;
            }

            if (!FitsInIntegralType(enumType.EnumUnderlyingType!.SpecialType, numeric))
            {
                return false;
            }

            signedAccumulator |= numeric;
        }

        numericLiteral = signedAccumulator.ToString(CultureInfo.InvariantCulture);

        return true;
    }

    private static bool IsParsableEnumNumeric(INamedTypeSymbol enumType, string value)
    {
        if (enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64)
        {
            return ulong.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _);
        }

        var parsed = long.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var numeric);
        if (!parsed)
        {
            return false;
        }

        return FitsInIntegralType(enumType.EnumUnderlyingType!.SpecialType, numeric);
    }

    public static bool FitsInIntegralType(SpecialType specialType, long value)
    {
        switch (specialType)
        {
            case SpecialType.System_SByte:
                return value >= sbyte.MinValue && value <= sbyte.MaxValue;
            case SpecialType.System_Byte:
                return value >= byte.MinValue && value <= byte.MaxValue;
            case SpecialType.System_Int16:
                return value >= short.MinValue && value <= short.MaxValue;
            case SpecialType.System_UInt16:
                return value >= ushort.MinValue && value <= ushort.MaxValue;
            case SpecialType.System_Int32:
                return value >= int.MinValue && value <= int.MaxValue;
            case SpecialType.System_UInt32:
                return value >= uint.MinValue && value <= uint.MaxValue;
            case SpecialType.System_Int64:
                return true;
            case SpecialType.System_UInt64:
                return value >= 0;
            default:
                return false;
        }
    }

    private static bool IsIntegralType(SpecialType specialType)
    {
        switch (specialType)
        {
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
                return true;
            default:
                return false;
        }
    }

    private static Location ParameterLocation(IParameterSymbol param)
        => param.Locations.FirstOrDefault() ?? Location.None;
}
