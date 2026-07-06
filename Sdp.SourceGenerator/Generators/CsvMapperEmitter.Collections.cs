using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

// 컬렉션 5종(다중 컬럼 배열/셋, dict, 단일 컬럼 배열/셋) 매핑 헬퍼 방출
internal static partial class CsvMapperEmitter
{
    private static string ElementTypeName(CollectionInfo info)
    {
        if (info.ElementNested is not null)
        {
            return info.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        var keyword = info.ElementKind == ScalarKind.Enum
            ? TypeClassifier.UnwrapNullable(info.ElementType).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : ScalarTypeKeyword(info.ElementKind);
        return TypeClassifier.IsNullable(info.ElementType) ? keyword + "?" : keyword;
    }

    // 고정 길이(__k0 …)와 동적 길이(__ek) 양쪽에서 헤더 키 변수/식을 받아 원소 변환식을 만든다.
    private static string EmitColumnElementConversion(
        ParameterAnalysis param,
        string keyExpression,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var info = param.Collection!;
        if (info.ElementNested is { } elementNested)
        {
            return $"__MapNested_{nestedTokens[elementNested.Symbol]}(headers, values, {keyExpression})";
        }

        var valueExpr = $"values[headers[{keyExpression}]]";
        var baseConversion = EmitElementBaseConversion(info, valueExpr, param, nestedTokens);

        // [Range]/[RegularExpression] 가 컬렉션에 붙으면 원소를 각각 검증한다. 검증 attribute 가 붙은
        // nullable 원소 컬렉션은 AreValidationAttributesApplicable 이 emit 자체를 거부하므로, nullable 원소가
        // 여기 도달하는 경우 WrapValidations 는 no-op 이다.
        var converted = WrapValidations(param, baseConversion, ownerToken);

        if (TypeClassifier.IsNullable(info.ElementType) && param.NullString is not null)
        {
            var nullLiteral = SymbolDisplay.FormatLiteral(param.NullString, quote: true);
            return $"({valueExpr} == {nullLiteral} ? {EmitElementNullCast(info)} : {converted})";
        }

        return converted;
    }

    private static string EmitElementBaseConversion(
        CollectionInfo info,
        string valueExpr,
        ParameterAnalysis param,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        if (info.ElementKind == ScalarKind.Enum)
        {
            var enumType = (INamedTypeSymbol)TypeClassifier.UnwrapNullable(info.ElementType);
            return $"__MapElementEnum_{nestedTokens[enumType]}({valueExpr})";
        }

        return EmitScalarParse(info.ElementKind, valueExpr, param.DateTimeFormat, param.TimeSpanFormat);
    }

    private static string EmitElementNullCast(CollectionInfo info)
    {
        if (info.ElementKind == ScalarKind.Enum)
        {
            var enumName = TypeClassifier.UnwrapNullable(info.ElementType).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"({enumName}?)null";
        }

        return $"({ScalarTypeKeyword(info.ElementKind)}?)null";
    }

    // 컬렉션 헬퍼는 basePath 를 받아 절대 헤더(root)와 중첩 헤더(basePath + "." + 절대키)를 모두 처리한다.
    private static void EmitKeyVar(StringBuilder sb, string indent, string keyVar, string absoluteKey)
    {
        var absoluteKeyLiteral = SymbolDisplay.FormatLiteral(absoluteKey, quote: true);
        sb.Append(indent).Append("    var ").Append(keyVar)
            .Append(" = basePath.Length == 0 ? ").Append(absoluteKeyLiteral)
            .Append(" : basePath + \".\" + ").Append(absoluteKeyLiteral).AppendLine(";");
    }

    // 동적 길이 컬렉션: 헤더에서 <ColumnName>[i] 인덱스를 모아 0..N-1 을 순회한다.
    // 인덱스가 비연속(gap)이면 throw 한다. 호출부는 이어서 루프 본문과 닫는 중괄호를 방출한다.
    private static void EmitDynamicElementLoopBegin(StringBuilder sb, ParameterAnalysis param, string indent)
    {
        var columnLiteral = SymbolDisplay.FormatLiteral(param.ColumnName, quote: true);
        sb.Append(indent).Append("    var __base = basePath.Length == 0 ? ").Append(columnLiteral)
            .Append(" : basePath + \".\" + ").Append(columnLiteral).AppendLine(";");
        sb.Append(indent).AppendLine("    var __prefix = __base + \"[\";");
        sb.Append(indent).AppendLine("    var __indices = new global::System.Collections.Generic.HashSet<int>();");
        sb.Append(indent).AppendLine("    var __max = -1;");
        sb.Append(indent).AppendLine("    foreach (var __h in headers.ColumnNames)");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        if (!__h.StartsWith(__prefix, global::System.StringComparison.Ordinal)) { continue; }");
        sb.Append(indent).AppendLine("        var __close = __h.IndexOf(']', __prefix.Length);");
        sb.Append(indent).AppendLine("        if (__close < 0) { continue; }");
        sb.Append(indent).AppendLine("        if (int.TryParse(__h.Substring(__prefix.Length, __close - __prefix.Length), global::System.Globalization.NumberStyles.None, global::System.Globalization.CultureInfo.InvariantCulture, out var __idx)) { __indices.Add(__idx); if (__idx > __max) { __max = __idx; } }");
        sb.Append(indent).AppendLine("    }");
        sb.AppendLine();
        sb.Append(indent).AppendLine("    for (var __i = 0; __i < __indices.Count; __i++)");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).Append("        if (!__indices.Contains(__i)) throw new global::System.ArgumentException(global::System.FormattableString.Invariant($\"Non-contiguous index {__i} (max found index {__max}) in collection '")
            .Append(param.Name).AppendLine("'.\"));");
        sb.Append(indent).AppendLine("        var __ek = __base + \"[\" + __i.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + \"]\";");
        sb.AppendLine();
    }

    private static void EmitArrayHelper(StringBuilder sb, ParameterAnalysis param, string indent, string ownerToken, Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var info = param.Collection!;
        var elementKeyword = ElementTypeName(info);

        sb.Append(indent).Append("private static global::System.Collections.Immutable.ImmutableArray<")
            .Append(elementKeyword)
            .Append("> ").Append(HelperName("__MapArray_", ownerToken, param.Name)).AppendLine("(");
        sb.Append(indent).AppendLine("    global::Sdp.Csv.CsvHeaderIndex headers,");
        sb.Append(indent).AppendLine("    string[] values,");
        sb.Append(indent).AppendLine("    string basePath)");
        sb.Append(indent).AppendLine("{");

        if (info.Length > 0)
        {
            sb.Append(indent).Append("    var builder = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<")
                .Append(elementKeyword).Append(">(").Append(info.Length).AppendLine(");");

            for (var i = 0; i < info.Length; i++)
            {
                var keyVar = "__k" + i.ToString(CultureInfo.InvariantCulture);
                EmitKeyVar(sb, indent, keyVar, param.ColumnName + "[" + i.ToString(CultureInfo.InvariantCulture) + "]");
                var conversion = EmitColumnElementConversion(param, keyVar, ownerToken, nestedTokens);
                sb.Append(indent).Append("    builder.Add(").Append(conversion).AppendLine(");");
            }

            sb.Append(indent).AppendLine("    return builder.MoveToImmutable();");
        }
        else
        {
            sb.Append(indent).Append("    var builder = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<")
                .Append(elementKeyword).AppendLine(">();");
            EmitDynamicElementLoopBegin(sb, param, indent);
            sb.Append(indent).Append("        builder.Add(")
                .Append(EmitColumnElementConversion(param, "__ek", ownerToken, nestedTokens)).AppendLine(");");
            sb.Append(indent).AppendLine("    }");
            sb.Append(indent).AppendLine("    return builder.ToImmutable();");
        }

        sb.Append(indent).AppendLine("}");
    }

    private static void EmitSetHelper(StringBuilder sb, ParameterAnalysis param, string indent, string ownerToken, Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var info = param.Collection!;
        var elementKeyword = ElementTypeName(info);

        sb.Append(indent).Append("private static global::System.Collections.Frozen.FrozenSet<")
            .Append(elementKeyword)
            .Append("> ").Append(HelperName("__MapSet_", ownerToken, param.Name)).AppendLine("(");
        sb.Append(indent).AppendLine("    global::Sdp.Csv.CsvHeaderIndex headers,");
        sb.Append(indent).AppendLine("    string[] values,");
        sb.Append(indent).AppendLine("    string basePath)");
        sb.Append(indent).AppendLine("{");

        if (info.Length > 0)
        {
            sb.Append(indent).Append("    var set = new global::System.Collections.Generic.HashSet<")
                .Append(elementKeyword).Append(">(").Append(info.Length).AppendLine(");");

            for (var i = 0; i < info.Length; i++)
            {
                var keyVar = "__k" + i.ToString(CultureInfo.InvariantCulture);
                EmitKeyVar(sb, indent, keyVar, param.ColumnName + "[" + i.ToString(CultureInfo.InvariantCulture) + "]");
                var conversion = EmitColumnElementConversion(param, keyVar, ownerToken, nestedTokens);
                var elementVar = "__e" + i.ToString(CultureInfo.InvariantCulture);
                sb.Append(indent).Append("    var ").Append(elementVar).Append(" = ").Append(conversion).AppendLine(";");
                sb.Append(indent).Append("    if (!set.Add(").Append(elementVar)
                    .Append(")) throw new global::System.ArgumentException(global::System.FormattableString.Invariant($\"Duplicate value {")
                    .Append(elementVar).Append("} in set '")
                    .Append(param.Name).AppendLine("'.\"));");
            }
        }
        else
        {
            sb.Append(indent).Append("    var set = new global::System.Collections.Generic.HashSet<")
                .Append(elementKeyword).AppendLine(">();");
            EmitDynamicElementLoopBegin(sb, param, indent);
            sb.Append(indent).Append("        var __e = ")
                .Append(EmitColumnElementConversion(param, "__ek", ownerToken, nestedTokens)).AppendLine(";");
            sb.Append(indent).Append("        if (!set.Add(__e")
                .Append(")) throw new global::System.ArgumentException(global::System.FormattableString.Invariant($\"Duplicate value {__e} in set '")
                .Append(param.Name).AppendLine("'.\"));");
            sb.Append(indent).AppendLine("    }");
        }

        sb.Append(indent).AppendLine("    return global::System.Collections.Frozen.FrozenSet.ToFrozenSet(set);");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitDictHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        string indent,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var info = param.Collection!;
        var valueNested = info.ValueNested!;

        // enum/record 키는 키워드가 없으므로 FQN 으로 쓴다.
        var keyKeyword = info.KeyKind is ScalarKind.Enum or ScalarKind.Unsupported
            ? info.KeyType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : ScalarTypeKeyword(info.KeyKind);
        var valueFullName = valueNested.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var keyParam = valueNested.Parameters.Single(nestedParameter => nestedParameter.IsKey);
        var keyMemberAccess = GeneratorEmitHelper.EscapeIdentifier(keyParam.Name);
        var valueToken = nestedTokens[valueNested.Symbol];

        sb.Append(indent).Append("private static global::System.Collections.Frozen.FrozenDictionary<")
            .Append(keyKeyword).Append(", ").Append(valueFullName)
            .Append("> ").Append(HelperName("__MapDict_", ownerToken, param.Name)).AppendLine("(");
        sb.Append(indent).AppendLine("    global::Sdp.Csv.CsvHeaderIndex headers,");
        sb.Append(indent).AppendLine("    string[] values,");
        sb.Append(indent).AppendLine("    string basePath)");
        sb.Append(indent).AppendLine("{");
        if (info.Length > 0)
        {
            sb.Append(indent).Append("    var dict = new global::System.Collections.Generic.Dictionary<")
                .Append(keyKeyword).Append(", ").Append(valueFullName).Append(">(").Append(info.Length).AppendLine(");");

            for (var i = 0; i < info.Length; i++)
            {
                var keyVar = "__k" + i.ToString(CultureInfo.InvariantCulture);
                EmitKeyVar(sb, indent, keyVar, param.ColumnName + "[" + i.ToString(CultureInfo.InvariantCulture) + "]");
                var varName = "__v" + i.ToString(CultureInfo.InvariantCulture);

                sb.Append(indent).Append("    var ").Append(varName).Append(" = __MapNested_")
                    .Append(valueToken).Append("(headers, values, ").Append(keyVar).AppendLine(");");
                sb.Append(indent).Append("    if (!dict.TryAdd(").Append(varName).Append('.').Append(keyMemberAccess).Append(", ").Append(varName)
                    .Append(")) throw new global::System.ArgumentException(global::System.FormattableString.Invariant($\"Duplicate key {")
                    .Append(varName).Append('.').Append(keyMemberAccess).Append("} in dict '").Append(param.Name).AppendLine("'.\"));");
            }
        }
        else
        {
            sb.Append(indent).Append("    var dict = new global::System.Collections.Generic.Dictionary<")
                .Append(keyKeyword).Append(", ").Append(valueFullName).AppendLine(">();");
            EmitDynamicElementLoopBegin(sb, param, indent);
            sb.Append(indent).Append("        var __v = __MapNested_")
                .Append(valueToken).AppendLine("(headers, values, __ek);");
            sb.Append(indent).Append("        if (!dict.TryAdd(__v.").Append(keyMemberAccess)
                .Append(", __v)) throw new global::System.ArgumentException(global::System.FormattableString.Invariant($\"Duplicate key {__v.")
                .Append(keyMemberAccess).Append("} in dict '")
                .Append(param.Name).AppendLine("'.\"));");
            sb.Append(indent).AppendLine("    }");
        }

        sb.Append(indent).AppendLine("    return global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(dict);");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitSingleColumnArrayHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        string indent,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var info = param.Collection!;
        var elementKeyword = ElementTypeName(info);
        var separatorLiteral = SymbolDisplay.FormatLiteral(info.Separator!, quote: true);

        sb.Append(indent).Append("private static global::System.Collections.Immutable.ImmutableArray<")
            .Append(elementKeyword)
            .Append("> ").Append(HelperName("__MapSingleArray_", ownerToken, param.Name)).AppendLine("(");
        sb.Append(indent).AppendLine("    global::Sdp.Csv.CsvHeaderIndex headers,");
        sb.Append(indent).AppendLine("    string[] values,");
        sb.Append(indent).AppendLine("    string basePath)");
        sb.Append(indent).AppendLine("{");
        EmitKeyVar(sb, indent, "__k", param.ColumnName);
        sb.Append(indent).AppendLine("    var __cell = values[headers[__k]];");
        EmitSingleColumnSplit(sb, param, separatorLiteral, indent);

        EmitCountRangeCheck(sb, param, "parts.Length", indent);

        sb.Append(indent).Append("    var builder = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<")
            .Append(elementKeyword).AppendLine(">(parts.Length);");
        sb.Append(indent).AppendLine("    for (var i = 0; i < parts.Length; i++)");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        var __raw = parts[i].Trim();");
        var conversion = EmitElementConversion(param, "__raw", ownerToken, nestedTokens);
        sb.Append(indent).Append("        builder.Add(").Append(conversion).AppendLine(");");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("    return builder.MoveToImmutable();");
        sb.Append(indent).AppendLine("}");
    }

    private static void EmitSingleColumnSetHelper(
        StringBuilder sb,
        ParameterAnalysis param,
        string indent,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var info = param.Collection!;
        var elementKeyword = ElementTypeName(info);
        var separatorLiteral = SymbolDisplay.FormatLiteral(info.Separator!, quote: true);

        sb.Append(indent).Append("private static global::System.Collections.Frozen.FrozenSet<")
            .Append(elementKeyword)
            .Append("> ").Append(HelperName("__MapSingleSet_", ownerToken, param.Name)).AppendLine("(");
        sb.Append(indent).AppendLine("    global::Sdp.Csv.CsvHeaderIndex headers,");
        sb.Append(indent).AppendLine("    string[] values,");
        sb.Append(indent).AppendLine("    string basePath)");
        sb.Append(indent).AppendLine("{");
        EmitKeyVar(sb, indent, "__k", param.ColumnName);
        sb.Append(indent).AppendLine("    var __cell = values[headers[__k]];");
        EmitSingleColumnSplit(sb, param, separatorLiteral, indent);
        sb.Append(indent).Append("    var set = new global::System.Collections.Generic.HashSet<").Append(elementKeyword).AppendLine(">(parts.Length);");
        sb.Append(indent).AppendLine("    for (var i = 0; i < parts.Length; i++)");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).AppendLine("        var __raw = parts[i].Trim();");
        var conversion = EmitElementConversion(param, "__raw", ownerToken, nestedTokens);
        sb.Append(indent).Append("        var __e = ").Append(conversion).AppendLine(";");
        sb.Append(indent).Append("        if (!set.Add(__e)) throw new global::System.ArgumentException(global::System.FormattableString.Invariant($\"Duplicate value {__e} in set '")
            .Append(param.Name).AppendLine("'.\"));");
        sb.Append(indent).AppendLine("    }");

        EmitCountRangeCheck(sb, param, "set.Count", indent);

        sb.Append(indent).AppendLine("    return global::System.Collections.Frozen.FrozenSet.ToFrozenSet(set);");
        sb.Append(indent).AppendLine("}");
    }

    // 빈 셀은 기본적으로 빈 컬렉션으로 매핑한다. 단 [NullString("")]이 부착된 nullable 원소 컬렉션이면
    // 빈 셀이 "null 원소 하나"를 의미하므로 단축을 적용하지 않는다 — "".Split 은 빈 문자열 한 조각을
    // 돌려주고, 그 조각이 NullString 과 일치해 null 로 매핑된다.
    private static void EmitSingleColumnSplit(StringBuilder sb, ParameterAnalysis param, string separatorLiteral, string indent)
    {
        var emptyCellMeansNullElement = TypeClassifier.IsNullable(param.Collection!.ElementType)
            && param.NullString is { Length: 0 };
        if (emptyCellMeansNullElement)
        {
            sb.Append(indent).Append("    var parts = __cell.Split(").Append(separatorLiteral).AppendLine(");");
            return;
        }

        sb.Append(indent).Append("    var parts = __cell.Length == 0 ? global::System.Array.Empty<string>() : __cell.Split(").Append(separatorLiteral).AppendLine(");");
    }

    private static string EmitElementConversion(
        ParameterAnalysis param,
        string rawVar,
        string ownerToken,
        Dictionary<INamedTypeSymbol, string> nestedTokens)
    {
        var info = param.Collection!;
        var baseConversion = EmitElementBaseConversion(info, rawVar, param, nestedTokens);
        var converted = WrapValidations(param, baseConversion, ownerToken);

        if (TypeClassifier.IsNullable(info.ElementType) && param.NullString is not null)
        {
            var nullLiteral = SymbolDisplay.FormatLiteral(param.NullString, quote: true);
            return $"({rawVar} == {nullLiteral} ? {EmitElementNullCast(info)} : {converted})";
        }

        return converted;
    }

    private static void EmitCountRangeCheck(StringBuilder sb, ParameterAnalysis param, string countExpr, string indent)
    {
        var info = param.Collection!;
        if (info.MinCount is null || info.MaxCount is null)
        {
            return;
        }

        var minCount = info.MinCount.Value.ToString(CultureInfo.InvariantCulture);
        var maxCount = info.MaxCount.Value.ToString(CultureInfo.InvariantCulture);

        sb.Append(indent).Append("    if (").Append(countExpr).Append(" < ").Append(minCount).Append(" || ").Append(countExpr).Append(" > ").Append(maxCount).AppendLine(")");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).Append("        throw new global::System.ArgumentException(global::System.FormattableString.Invariant($\"Count of '").Append(param.Name)
            .Append("' must be within [").Append(minCount).Append(", ").Append(maxCount)
            .Append("] but was {").Append(countExpr).AppendLine("}.\"));");
        sb.Append(indent).AppendLine("    }");
    }
}
