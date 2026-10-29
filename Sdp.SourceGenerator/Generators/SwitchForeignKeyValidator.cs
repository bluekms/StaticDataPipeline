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
        SwitchForeignKeyConditionLiteralBuilder.ReadFormats(recordType, conditionColumn, out var dateTimeFormat, out var timeSpanFormat);
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
            var literal = SwitchForeignKeyConditionLiteralBuilder.TryBuild(conditionType, conditionValue, dateTimeFormat, timeSpanFormat);
            if (literal is null)
            {
                continue;
            }

            var normalizedValue = literal.Normalized;

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
