using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal sealed record SwitchForeignKeyConditionLiteral(string Code, string Normalized);

internal static class SwitchForeignKeyConditionLiteralBuilder
{
    public static bool IsScalarConditionType(ITypeSymbol conditionType)
    {
        if (conditionType.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        return TypeClassifier.ClassifyScalar(conditionType) != ScalarKind.Unsupported;
    }

    public static void ReadFormats(
        INamedTypeSymbol recordType,
        string conditionColumn,
        out string? dateTimeFormat,
        out string? timeSpanFormat)
    {
        dateTimeFormat = null;
        timeSpanFormat = null;

        var primaryConstructor = SymbolResolver.FindPrimaryConstructor(recordType);
        if (primaryConstructor is null)
        {
            return;
        }

        foreach (var param in primaryConstructor.Parameters)
        {
            if (!string.Equals(param.Name, conditionColumn, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var attr in param.GetAttributes())
            {
                if (!SdpAttributeNames.IsSdpAttribute(attr))
                {
                    continue;
                }

                var args = attr.ConstructorArguments;
                if (args.Length == 0 || args[0].Value is not string format)
                {
                    continue;
                }

                switch (attr.AttributeClass!.Name)
                {
                    case SdpAttributeNames.DateTimeFormat:
                        dateTimeFormat = format;
                        break;
                    case SdpAttributeNames.TimeSpanFormat:
                        timeSpanFormat = format;
                        break;
                }
            }

            return;
        }
    }

    public static SwitchForeignKeyConditionLiteral? TryBuild(
        ITypeSymbol conditionType,
        string conditionValue,
        string? dateTimeFormat,
        string? timeSpanFormat)
    {
        if (conditionType is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
        {
            return TryBuildEnum(enumType, conditionValue);
        }

        switch (TypeClassifier.ClassifyScalar(conditionType))
        {
            case ScalarKind.String:
                return new SwitchForeignKeyConditionLiteral(SymbolDisplay.FormatLiteral(conditionValue, quote: true), conditionValue);

            case ScalarKind.Char:
                if (conditionValue.Length != 1)
                {
                    return null;
                }

                return new SwitchForeignKeyConditionLiteral(SymbolDisplay.FormatLiteral(conditionValue[0], quote: true), conditionValue);

            case ScalarKind.Bool:
                return TryBuildBool(conditionValue);

            case ScalarKind.Byte:
            case ScalarKind.SByte:
            case ScalarKind.Int16:
            case ScalarKind.UInt16:
            case ScalarKind.Int32:
            case ScalarKind.UInt32:
            case ScalarKind.Int64:
                return TryBuildSignedIntegral(conditionType.SpecialType, conditionValue);

            case ScalarKind.UInt64:
                return TryBuildUnsignedIntegral(conditionValue);

            case ScalarKind.Single:
                return TryBuildSingle(conditionValue);

            case ScalarKind.Double:
                return TryBuildDouble(conditionValue);

            case ScalarKind.Decimal:
                return TryBuildDecimal(conditionValue);

            case ScalarKind.Guid:
                return TryBuildGuid(conditionValue);

            case ScalarKind.DateTime:
                return TryBuildDateTime(conditionValue, dateTimeFormat);

            case ScalarKind.DateTimeOffset:
                return TryBuildDateTimeOffset(conditionValue, dateTimeFormat);

            case ScalarKind.DateOnly:
                return TryBuildDateOnly(conditionValue, dateTimeFormat);

            case ScalarKind.TimeOnly:
                return TryBuildTimeOnly(conditionValue, dateTimeFormat);

            case ScalarKind.TimeSpan:
                return TryBuildTimeSpan(conditionValue, timeSpanFormat);

            default:
                return null;
        }
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildEnum(INamedTypeSymbol enumType, string conditionValue)
    {
        var enumFullyQualifiedName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var underlyingSpecialType = enumType.EnumUnderlyingType!.SpecialType;

        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.IsConst || !string.Equals(member.Name, conditionValue, StringComparison.Ordinal))
            {
                continue;
            }

            var normalized = underlyingSpecialType == SpecialType.System_UInt64
                ? Convert.ToUInt64(member.ConstantValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                : Convert.ToInt64(member.ConstantValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

            return new SwitchForeignKeyConditionLiteral(enumFullyQualifiedName + "." + EmitHelper.EscapeIdentifier(member.Name), normalized);
        }

        if (SwitchForeignKeyConditionValueValidator.HasFlagsAttribute(enumType))
        {
            var parsedFlags = SwitchForeignKeyConditionValueValidator.TryParseFlagsConditionValue(enumType, conditionValue, out var flagsNumericLiteral);
            if (!parsedFlags)
            {
                return null;
            }

            return new SwitchForeignKeyConditionLiteral("(" + enumFullyQualifiedName + ")(" + flagsNumericLiteral + ")", flagsNumericLiteral);
        }

        if (underlyingSpecialType == SpecialType.System_UInt64)
        {
            var parsedUnsigned = ulong.TryParse(conditionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsigned);
            if (!parsedUnsigned)
            {
                return null;
            }

            var unsignedText = unsigned.ToString(CultureInfo.InvariantCulture);

            return new SwitchForeignKeyConditionLiteral("(" + enumFullyQualifiedName + ")(" + unsignedText + ")", unsignedText);
        }

        var parsedSigned = long.TryParse(conditionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var signed);
        if (!parsedSigned || !SwitchForeignKeyConditionValueValidator.FitsInIntegralType(underlyingSpecialType, signed))
        {
            return null;
        }

        var signedText = signed.ToString(CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral("(" + enumFullyQualifiedName + ")(" + signedText + ")", signedText);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildBool(string conditionValue)
    {
        var parsed = bool.TryParse(conditionValue, out var value);
        if (!parsed)
        {
            return null;
        }

        var text = value ? "true" : "false";

        return new SwitchForeignKeyConditionLiteral(text, text);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildSignedIntegral(SpecialType specialType, string conditionValue)
    {
        var parsed = long.TryParse(conditionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
        if (!parsed || !SwitchForeignKeyConditionValueValidator.FitsInIntegralType(specialType, value))
        {
            return null;
        }

        var text = value.ToString(CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral(text, text);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildUnsignedIntegral(string conditionValue)
    {
        var parsed = ulong.TryParse(conditionValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
        if (!parsed)
        {
            return null;
        }

        var text = value.ToString(CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral(text + "UL", text);
    }

    // 실수의 파싱 스타일은 런타임 CSV 파싱(EmitScalarParse 가 내는 코드)과 같아야 한다.
    private static SwitchForeignKeyConditionLiteral? TryBuildSingle(string conditionValue)
    {
        var parsed = float.TryParse(conditionValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value);
        if (!parsed || float.IsNaN(value) || float.IsInfinity(value))
        {
            return null;
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral(text + "f", text);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildDouble(string conditionValue)
    {
        var parsed = double.TryParse(conditionValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value);
        if (!parsed || double.IsNaN(value) || double.IsInfinity(value))
        {
            return null;
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral(text + "d", text);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildDecimal(string conditionValue)
    {
        var parsed = decimal.TryParse(conditionValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value);
        if (!parsed)
        {
            return null;
        }

        // 1.50m 과 1.5m 은 같은 값이지만 ToString 이 다르다. 1.000…m 으로 나누면 뒤따르는 0 이 정리된다. (decimal 의 scale 보존이라는 BCL 동작과 관용구)
        var normalized = (value / 1.000000000000000000000000000000000m).ToString(CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral(value.ToString(CultureInfo.InvariantCulture) + "m", normalized);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildGuid(string conditionValue)
    {
        var parsed = Guid.TryParse(conditionValue, out var value);
        if (!parsed)
        {
            return null;
        }

        var text = value.ToString("D", CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral("new global::System.Guid(\"" + text + "\")", text);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildDateTime(string conditionValue, string? dateTimeFormat)
    {
        if (dateTimeFormat is null)
        {
            return null;
        }

        var parsed = DateTime.TryParseExact(conditionValue, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value);
        if (!parsed)
        {
            return null;
        }

        var ticks = value.Ticks.ToString(CultureInfo.InvariantCulture);
        var code = "new global::System.DateTime(" + ticks + "L, global::System.DateTimeKind." + value.Kind + ")";

        return new SwitchForeignKeyConditionLiteral(code, ticks);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildDateTimeOffset(string conditionValue, string? dateTimeFormat)
    {
        if (dateTimeFormat is null)
        {
            return null;
        }

        var parsed = DateTimeOffset.TryParseExact(conditionValue, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value);
        if (!parsed)
        {
            return null;
        }

        var ticks = value.Ticks.ToString(CultureInfo.InvariantCulture);
        var offsetTicks = value.Offset.Ticks.ToString(CultureInfo.InvariantCulture);
        var code = "new global::System.DateTimeOffset(" + ticks + "L, new global::System.TimeSpan(" + offsetTicks + "L))";

        return new SwitchForeignKeyConditionLiteral(code, value.UtcTicks.ToString(CultureInfo.InvariantCulture));
    }

    // DateOnly/TimeOnly 는 생성기(netstandard2.0)에 없는 타입이라 DateTime 으로 해석한 뒤 리터럴만 만든다.
    private static SwitchForeignKeyConditionLiteral? TryBuildDateOnly(string conditionValue, string? dateTimeFormat)
    {
        if (dateTimeFormat is null)
        {
            return null;
        }

        var parsed = DateTime.TryParseExact(conditionValue, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value);
        if (!parsed)
        {
            return null;
        }

        var code = FormattableString.Invariant($"new global::System.DateOnly({value.Year}, {value.Month}, {value.Day})");

        return new SwitchForeignKeyConditionLiteral(code, value.Date.Ticks.ToString(CultureInfo.InvariantCulture));
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildTimeOnly(string conditionValue, string? dateTimeFormat)
    {
        if (dateTimeFormat is null)
        {
            return null;
        }

        var parsed = DateTime.TryParseExact(conditionValue, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value);
        if (!parsed)
        {
            return null;
        }

        var ticks = value.TimeOfDay.Ticks.ToString(CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral("new global::System.TimeOnly(" + ticks + "L)", ticks);
    }

    private static SwitchForeignKeyConditionLiteral? TryBuildTimeSpan(string conditionValue, string? timeSpanFormat)
    {
        if (timeSpanFormat is null)
        {
            return null;
        }

        var parsed = TimeSpan.TryParseExact(conditionValue, timeSpanFormat, CultureInfo.InvariantCulture, out var value);
        if (!parsed)
        {
            return null;
        }

        var ticks = value.Ticks.ToString(CultureInfo.InvariantCulture);

        return new SwitchForeignKeyConditionLiteral("new global::System.TimeSpan(" + ticks + "L)", ticks);
    }
}
