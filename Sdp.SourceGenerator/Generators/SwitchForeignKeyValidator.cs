using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class SwitchForeignKeyValidator
{
    public static void ValidateTarget(
        AttributeData attr,
        IParameterSymbol param,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var hasTableSetName = TryGetTargetTableSetName(attr, out var tableSetName);
        var hasColumnName = TryGetTargetColumnName(attr, out var columnName);
        if (!hasTableSetName || !hasColumnName)
        {
            return;
        }

        ForeignKeyTargetValidator.ValidateColumn(tableSetName, columnName, param, membersByName, diagnostics);
    }

    public static void ValidateConditionUniqueness(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var seen = new HashSet<(string, string)>();
        foreach (var attr in switchFkAttrs)
        {
            var hasConditionColumn = TryGetConditionColumn(attr, out var conditionColumn);
            if (!hasConditionColumn)
            {
                continue;
            }

            var hasConditionValue = TryGetConditionValue(attr, out var conditionValue);
            if (!hasConditionValue)
            {
                continue;
            }

            if (!seen.Add((conditionColumn, conditionValue)))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.SwitchForeignKeyDuplicateConditionValue,
                    ParameterLocation(param),
                    recordType.Name,
                    param.Name,
                    conditionColumn,
                    conditionValue));
            }
        }
    }

    public static void ValidateConditionValueEquivalence(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var hasConditionColumn = TryGetConditionColumn(switchFkAttrs[0], out var conditionColumn);
        if (!hasConditionColumn)
        {
            return;
        }

        var conditionProperty = recordType.GetMembers(conditionColumn).OfType<IPropertySymbol>().FirstOrDefault();
        if (conditionProperty is null)
        {
            return;
        }

        var conditionType = TypeClassifier.UnwrapNullable(conditionProperty.Type);
        var seenValues = new HashSet<string>(StringComparer.Ordinal);
        var firstValueByNormalized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attr in switchFkAttrs)
        {
            var hasConditionValue = TryGetConditionValue(attr, out var conditionValue);
            if (!hasConditionValue)
            {
                continue;
            }

            // 파싱 불가한 값은 조건값 유효성 진단(SDP0212 등)이 담당한다.
            var normalized = TryNormalizeConditionValue(conditionType, conditionValue, out var normalizedValue);
            if (!normalized)
            {
                continue;
            }

            // 표기까지 동일한 중복은 SDP0208이 담당한다.
            var isNewValue = seenValues.Add(conditionValue);
            if (!isNewValue)
            {
                continue;
            }

            var hasEarlierValue = firstValueByNormalized.TryGetValue(normalizedValue, out var earlierValue);
            if (!hasEarlierValue)
            {
                firstValueByNormalized[normalizedValue] = conditionValue;

                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyEquivalentConditionValue,
                ParameterLocation(param),
                recordType.Name,
                param.Name,
                conditionColumn,
                conditionValue,
                earlierValue));
        }
    }

    public static void ValidateConditionColumnExists(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in switchFkAttrs)
        {
            var hasConditionColumn = TryGetConditionColumn(attr, out var conditionColumn);
            if (!hasConditionColumn)
            {
                continue;
            }

            if (!seen.Add(conditionColumn))
            {
                continue;
            }

            var conditionProperty = recordType.GetMembers(conditionColumn).OfType<IPropertySymbol>().FirstOrDefault();
            if (conditionProperty is not null)
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.SwitchForeignKeyConditionColumnNotFound,
                ParameterLocation(param),
                conditionColumn,
                recordType.Name));
        }
    }

    public static bool ValidateConditionColumnConsistency(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var conditionColumns = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in switchFkAttrs)
        {
            var hasConditionColumn = TryGetConditionColumn(attr, out var conditionColumn);
            if (!hasConditionColumn)
            {
                continue;
            }

            conditionColumns.Add(conditionColumn);
        }

        if (conditionColumns.Count <= 1)
        {
            return true;
        }

        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.SwitchForeignKeyConditionColumnMismatch,
            ParameterLocation(param),
            recordType.Name,
            param.Name));

        return false;
    }

    private static bool TryNormalizeConditionValue(
        ITypeSymbol conditionType,
        string conditionValue,
        out string normalizedValue)
    {
        normalizedValue = conditionValue;

        if (conditionType.TypeKind == TypeKind.Enum && conditionType is INamedTypeSymbol enumType)
        {
            return TryNormalizeEnumConditionValue(enumType, conditionValue, out normalizedValue);
        }

        switch (conditionType.SpecialType)
        {
            case SpecialType.System_Boolean:
            {
                var parsedBool = bool.TryParse(conditionValue, out var boolValue);
                if (!parsedBool)
                {
                    return false;
                }

                normalizedValue = boolValue ? "true" : "false";

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

                normalizedValue = signed.ToString(CultureInfo.InvariantCulture);

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

                normalizedValue = unsigned.ToString(CultureInfo.InvariantCulture);

                return true;
            }
        }

        if (TypeClassifier.ClassifyScalar(conditionType) == ScalarKind.Guid)
        {
            var parsedGuid = Guid.TryParse(conditionValue, out var guid);
            if (!parsedGuid)
            {
                return false;
            }

            normalizedValue = guid.ToString("D", CultureInfo.InvariantCulture);

            return true;
        }

        // string·char 등 나머지 타입은 표기가 곧 값이라 그대로 비교한다.
        return true;
    }

    private static bool TryNormalizeEnumConditionValue(
        INamedTypeSymbol enumType,
        string conditionValue,
        out string normalizedValue)
    {
        normalizedValue = string.Empty;

        if (SwitchForeignKeyConditionValueValidator.HasFlagsAttribute(enumType))
        {
            return SwitchForeignKeyConditionValueValidator.TryParseFlagsConditionValue(
                enumType, conditionValue, out normalizedValue);
        }

        var underlyingSpecialType = enumType.EnumUnderlyingType!.SpecialType;
        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsConst && string.Equals(member.Name, conditionValue, StringComparison.Ordinal))
            {
                if (underlyingSpecialType == SpecialType.System_UInt64)
                {
                    var unsignedMemberValue = Convert.ToUInt64(member.ConstantValue, CultureInfo.InvariantCulture);
                    normalizedValue = unsignedMemberValue.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    var signedMemberValue = Convert.ToInt64(member.ConstantValue, CultureInfo.InvariantCulture);
                    normalizedValue = signedMemberValue.ToString(CultureInfo.InvariantCulture);
                }

                return true;
            }
        }

        if (underlyingSpecialType == SpecialType.System_UInt64)
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

            normalizedValue = unsigned.ToString(CultureInfo.InvariantCulture);

            return true;
        }

        var parsedSigned = long.TryParse(
            conditionValue,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var signed);
        if (!parsedSigned)
        {
            return false;
        }

        if (!SwitchForeignKeyConditionValueValidator.FitsInIntegralType(underlyingSpecialType, signed))
        {
            return false;
        }

        normalizedValue = signed.ToString(CultureInfo.InvariantCulture);

        return true;
    }

    private static bool TryGetConditionColumn(AttributeData attr, out string conditionColumn)
    {
        var args = attr.ConstructorArguments;
        if (args.Length > 0 && args[0].Value is string value)
        {
            conditionColumn = value;

            return true;
        }

        conditionColumn = string.Empty;

        return false;
    }

    private static bool TryGetConditionValue(AttributeData attr, out string conditionValue)
    {
        var args = attr.ConstructorArguments;
        if (args.Length > 1 && args[1].Value is string value)
        {
            conditionValue = value;

            return true;
        }

        conditionValue = string.Empty;

        return false;
    }

    private static bool TryGetTargetTableSetName(AttributeData attr, out string tableSetName)
    {
        var args = attr.ConstructorArguments;
        if (args.Length > 2 && args[2].Value is string value)
        {
            tableSetName = value;

            return true;
        }

        tableSetName = string.Empty;

        return false;
    }

    private static bool TryGetTargetColumnName(AttributeData attr, out string columnName)
    {
        var args = attr.ConstructorArguments;
        if (args.Length > 3 && args[3].Value is string value)
        {
            columnName = value;

            return true;
        }

        columnName = string.Empty;

        return false;
    }

    private static Location ParameterLocation(IParameterSymbol param)
        => param.Locations.FirstOrDefault() ?? Location.None;
}
