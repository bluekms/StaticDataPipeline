using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ForeignKeyAnalyzer
{
    public static ImmutableArray<RecordFkInfo> CollectRecordFkInfos(
        ImmutableArray<TableInfo> tables,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<RecordFkInfo>();
        var cache = new Dictionary<INamedTypeSymbol, ImmutableArray<FkParameter>>(SymbolEqualityComparer.Default);

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (table.RecordSymbol is null)
            {
                continue;
            }

            if (!cache.TryGetValue(table.RecordSymbol, out var paramInfos))
            {
                paramInfos = AnalyzeFkParameters(table.RecordSymbol, membersByName);
                cache[table.RecordSymbol] = paramInfos;
            }

            if (paramInfos.Length == 0)
            {
                continue;
            }

            builder.Add(new RecordFkInfo(table.ParameterName, table.RecordSymbol, paramInfos));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<FkParameter> AnalyzeFkParameters(
        INamedTypeSymbol recordType,
        Dictionary<string, INamedTypeSymbol?> membersByName)
    {
        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(recordType);
        if (primaryCtor is null)
        {
            return ImmutableArray<FkParameter>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<FkParameter>();

        foreach (var param in primaryCtor.Parameters)
        {
            var branchBuilder = ImmutableArray.CreateBuilder<FkBranch>();
            foreach (var attr in param.GetAttributes())
            {
                if (!IsSdpAttribute(attr))
                {
                    continue;
                }

                var args = attr.ConstructorArguments;

                if (attr.AttributeClass!.Name == "ForeignKeyAttribute" &&
                    args.Length >= 2 &&
                    args[0].Value is string tableSetName &&
                    args[1].Value is string columnName)
                {
                    var targetType = ResolveTargetColumnType(membersByName, tableSetName, columnName);
                    if (!IsValidFkTarget(param.Type, targetType))
                    {
                        continue;
                    }

                    branchBuilder.Add(FkBranch.ForeignKey(tableSetName, columnName));
                }
                else if (attr.AttributeClass.Name == "SwitchForeignKeyAttribute" &&
                         args.Length >= 4 &&
                         args[0].Value is string conditionColumn &&
                         args[1].Value is string conditionValue &&
                         args[2].Value is string switchTableSetName &&
                         args[3].Value is string switchColumnName)
                {
                    var conditionProperty = recordType.GetMembers(conditionColumn).OfType<IPropertySymbol>().FirstOrDefault();
                    if (conditionProperty is null)
                    {
                        continue;
                    }

                    var targetType = ResolveTargetColumnType(membersByName, switchTableSetName, switchColumnName);
                    if (!IsValidFkTarget(param.Type, targetType))
                    {
                        continue;
                    }

                    branchBuilder.Add(FkBranch.SwitchForeignKey(
                        switchTableSetName,
                        switchColumnName,
                        conditionColumn,
                        conditionValue,
                        conditionProperty.Type));
                }
            }

            if (branchBuilder.Count == 0)
            {
                continue;
            }

            builder.Add(new FkParameter(param.Name, param.Type, branchBuilder.ToImmutable()));
        }

        return builder.ToImmutable();
    }

    private static bool IsValidFkTarget(ITypeSymbol parameterType, ITypeSymbol? targetType)
    {
        if (targetType is null)
        {
            return false;
        }

        var unwrappedTarget = TypeClassifier.UnwrapNullable(targetType);
        if (!IsScalarTargetType(unwrappedTarget))
        {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(TypeClassifier.UnwrapNullable(parameterType), unwrappedTarget))
        {
            return false;
        }

        return TypeClassifier.IsNullable(parameterType) || !TypeClassifier.IsNullable(targetType);
    }

    private static ITypeSymbol? ResolveTargetColumnType(
        Dictionary<string, INamedTypeSymbol?> membersByName,
        string tableSetMember,
        string columnName)
    {
        if (!membersByName.TryGetValue(tableSetMember, out var record) || record is null)
        {
            return null;
        }

        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(record);
        return primaryCtor?.Parameters.FirstOrDefault(param => param.Name == columnName)?.Type;
    }

    private static bool IsScalarTargetType(ITypeSymbol type)
    {
        var collection = TypeClassifier.ClassifyCollection(type);
        return collection is null;
    }

    public static bool Validate(
        ImmutableArray<TableInfo> tables,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var allValid = true;

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (table.RecordSymbol is null || !visited.Add(table.RecordSymbol))
            {
                continue;
            }

            allValid &= ValidateRecordForeignKeys(table.RecordSymbol, membersByName, diagnostics);
        }

        return allValid;
    }

    private static bool ValidateRecordForeignKeys(
        INamedTypeSymbol recordType,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(recordType);
        if (primaryCtor is null)
        {
            return true;
        }

        var valid = true;

        foreach (var param in primaryCtor.Parameters)
        {
            var fkAttrs = new List<AttributeData>();
            var switchFkAttrs = new List<AttributeData>();

            foreach (var attr in param.GetAttributes())
            {
                if (!IsSdpAttribute(attr))
                {
                    continue;
                }

                if (attr.AttributeClass!.Name == "ForeignKeyAttribute")
                {
                    fkAttrs.Add(attr);
                }
                else if (attr.AttributeClass.Name == "SwitchForeignKeyAttribute")
                {
                    switchFkAttrs.Add(attr);
                }
            }

            if (fkAttrs.Count > 0 && switchFkAttrs.Count > 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.ForeignKeySwitchConflict,
                    ParameterLocation(param),
                    recordType.Name,
                    param.Name));
                valid = false;
            }

            foreach (var attr in fkAttrs)
            {
                ValidateFkTarget(attr, param, membersByName, diagnostics);
            }

            if (switchFkAttrs.Count > 0)
            {
                foreach (var attr in switchFkAttrs)
                {
                    ValidateSwitchFkTarget(attr, param, membersByName, diagnostics);
                }

                ValidateSwitchFkConditionUniqueness(recordType, param, switchFkAttrs, diagnostics);

                ValidateSwitchFkConditionColumnExists(recordType, param, switchFkAttrs, diagnostics);

                var consistent = ValidateSwitchFkConditionColumnConsistency(recordType, param, switchFkAttrs, diagnostics);
                valid &= consistent;

                if (consistent)
                {
                    ValidateSwitchFkConditionValue(recordType, param, switchFkAttrs, diagnostics);
                }
            }
        }

        return valid;
    }

    // 조건 프로퍼티를 한 번만 해석해 타입별 값 검증으로 라우팅한다. enum/정수는 유효한 ConditionValue
    // 형태가 닫혀 있어 정확히 판정할 수 있고, 그 외 타입(string/double 등)은 포맷 차이로 인한 오탐
    // 위험이 커 검증하지 않는다. 조건 프로퍼티 미존재는 SDP0209 소유자가 보고하므로 여기서는 물러난다.
    private static void ValidateSwitchFkConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        if (switchFkAttrs.Count == 0)
        {
            return;
        }

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

        if (conditionType.TypeKind == TypeKind.Enum && conditionType is INamedTypeSymbol enumType)
        {
            ValidateSwitchFkEnumConditionValue(
                recordType, param, switchFkAttrs, conditionColumn, enumType, diagnostics);
        }
        else if (IsIntegralType(conditionType.SpecialType))
        {
            ValidateSwitchFkIntegralConditionValue(
                recordType, param, switchFkAttrs, conditionColumn, conditionType, diagnostics);
        }
    }

    // 런타임은 enum 조건 컬럼을 enumValue.ToString()(멤버명)으로 비교한다(TableSetEmitter.IsFormattable=false).
    // 따라서 ConditionValue는 멤버명, 또는 미정의 값의 숫자 문자열과만 매칭될 수 있다.
    private static void ValidateSwitchFkEnumConditionValue(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        string conditionColumn,
        INamedTypeSymbol enumType,
        List<Diagnostic> diagnostics)
    {
        // [Flags] enum 은 'A, B' 조합 등 유효한 ConditionValue 형태가 많아 오탐 위험이 커 제외한다.
        if (HasFlagsAttribute(enumType))
        {
            return;
        }

        var memberNames = new HashSet<string>(System.StringComparer.Ordinal);
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

    // 정수 조건 컬럼도 enum 처럼 유효한 ConditionValue 형태가 닫혀 있다 — 정수 값의 ToString 은 항상
    // 정수 파싱 가능한 정규 표기이므로, 파싱 불가/범위 밖 값은 타입 비교로도 문자열 비교 폴백으로도
    // 매칭될 수 없는 죽은 branch 다. 판정 기준은 emitter(TableSetEmitter.BuildTypedConditionLiteral)와 같다.
    private static void ValidateSwitchFkIntegralConditionValue(
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

            if (IsParsableIntegral(conditionType.SpecialType, conditionValue))
            {
                continue;
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

    public static bool HasFlagsAttribute(INamedTypeSymbol enumType)
        => enumType.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                == "global::System.FlagsAttribute");

    // 미정의 숫자 ConditionValue 는 enum underlying 타입 범위 안일 때만 유효하다. 범위를 벗어난 값은
    // 어떤 행과도 매칭될 수 없는 죽은 branch 이고, emitter 도 캐스트 리터럴로 방출할 수 없다(CS0221).
    private static bool IsParsableEnumNumeric(INamedTypeSymbol enumType, string value)
    {
        if (enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64)
        {
            return ulong.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out _);
        }

        var parsed = long.TryParse(
            value,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var numeric);
        if (!parsed)
        {
            return false;
        }

        return FitsInEnumUnderlyingType(enumType, numeric);
    }

    // long 으로 파싱된 값이 enum underlying 타입의 표현 범위 안에 있는지 판정한다.
    // TableSetEmitter.BuildTypedConditionLiteral 이 같은 판정으로 CS0221 캐스트 방출을 막는다.
    public static bool FitsInEnumUnderlyingType(INamedTypeSymbol enumType, long value)
        => FitsInIntegralType(enumType.EnumUnderlyingType!.SpecialType, value);

    // long 으로 파싱된 값이 해당 정수 타입의 표현 범위 안에 있는지 판정한다.
    // TableSetEmitter.BuildTypedConditionLiteral 이 같은 판정으로 비-enum 정수 조건 컬럼의
    // 범위 밖 상수 비교(CS0652) 방출을 막는다.
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

    // ulong 은 long 파싱 범위를 벗어나므로 IsParsableEnumNumeric 과 같은 방식으로 별도 처리한다.
    private static bool IsParsableIntegral(SpecialType specialType, string value)
    {
        if (specialType == SpecialType.System_UInt64)
        {
            return ulong.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out _);
        }

        var parsed = long.TryParse(
            value,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var numeric);
        if (!parsed)
        {
            return false;
        }

        return FitsInIntegralType(specialType, numeric);
    }

    private static bool ValidateSwitchFkConditionColumnConsistency(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var conditionColumns = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 1 || args[0].Value is not string conditionColumn)
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

    private static void ValidateSwitchFkConditionColumnExists(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 1 || args[0].Value is not string conditionColumn)
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

    private static void ValidateFkTarget(
        AttributeData attr,
        IParameterSymbol param,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var args = attr.ConstructorArguments;
        if (args.Length < 2)
        {
            return;
        }

        ValidateFkTargetColumn(
            args[0].Value as string,
            args[1].Value as string,
            param,
            membersByName,
            diagnostics);
    }

    private static void ValidateSwitchFkTarget(
        AttributeData attr,
        IParameterSymbol param,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var args = attr.ConstructorArguments;
        if (args.Length < 4)
        {
            return;
        }

        ValidateFkTargetColumn(
            args[2].Value as string,
            args[3].Value as string,
            param,
            membersByName,
            diagnostics);
    }

    private static void ValidateFkTargetColumn(
        string? tableSetName,
        string? columnName,
        IParameterSymbol param,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        if (tableSetName is null || columnName is null)
        {
            return;
        }

        if (!membersByName.TryGetValue(tableSetName, out var targetRecord) || targetRecord is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetNotFound,
                ParameterLocation(param),
                tableSetName));
            return;
        }

        var targetCtor = SinglePrimaryConstructorResolver.Resolve(targetRecord);
        if (targetCtor is null)
        {
            return;
        }

        var targetParam = targetCtor.Parameters
            .FirstOrDefault(candidate => candidate.Name == columnName);
        if (targetParam is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetColumnNotFound,
                ParameterLocation(param),
                columnName,
                targetRecord.Name));
            return;
        }

        if (targetParam.GetAttributes()
            .Any(targetAttribute => IsSdpAttribute(targetAttribute) &&
                                    targetAttribute.AttributeClass!.Name == "SingleColumnCollectionAttribute"))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetIsSingleColumnCollection,
                ParameterLocation(param),
                columnName,
                targetRecord.Name));
            return;
        }

        var fkType = TypeClassifier.UnwrapNullable(param.Type);
        var targetType = TypeClassifier.UnwrapNullable(targetParam.Type);

        if (!IsScalarTargetType(targetType))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetColumnIsCollection,
                ParameterLocation(param),
                columnName,
                targetRecord.Name));
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(fkType, targetType))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyColumnTypeMismatch,
                ParameterLocation(param),
                param.Name,
                fkType.ToDisplayString(),
                columnName,
                targetRecord.Name,
                targetType.ToDisplayString()));
            return;
        }

        if (!TypeClassifier.IsNullable(param.Type) && TypeClassifier.IsNullable(targetParam.Type))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyColumnTypeMismatch,
                ParameterLocation(param),
                param.Name,
                param.Type.ToDisplayString(),
                columnName,
                targetRecord.Name,
                targetParam.Type.ToDisplayString()));
        }
    }

    private static void ValidateSwitchFkConditionUniqueness(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var seen = new HashSet<(string, string)>();
        foreach (var attr in switchFkAttrs)
        {
            var args = attr.ConstructorArguments;
            if (args.Length < 2)
            {
                continue;
            }

            var conditionColumn = args[0].Value as string;
            var conditionValue = args[1].Value as string;
            if (conditionColumn is null || conditionValue is null)
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

    private static bool IsSdpAttribute(AttributeData attr)
        => attr.AttributeClass?.ContainingNamespace?.ToDisplayString() == "Sdp.Attributes";

    private static Location ParameterLocation(IParameterSymbol param)
        => param.Locations.FirstOrDefault() ?? Location.None;
}

internal sealed record FkBranch(
    string TableSetMember,
    string TargetColumn,
    bool IsSwitch,
    string? ConditionColumn,
    string? ConditionValue,
    ITypeSymbol? ConditionType)
{
    public static FkBranch ForeignKey(string tableSetMember, string targetColumn)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: false,
            ConditionColumn: null,
            ConditionValue: null,
            ConditionType: null);
    }

    public static FkBranch SwitchForeignKey(
        string tableSetMember,
        string targetColumn,
        string conditionColumn,
        string conditionValue,
        ITypeSymbol conditionType)
    {
        return new FkBranch(
            tableSetMember,
            targetColumn,
            IsSwitch: true,
            conditionColumn,
            conditionValue,
            conditionType);
    }
}

internal sealed record FkParameter(
    string PropertyName,
    ITypeSymbol PropertyType,
    ImmutableArray<FkBranch> Branches);

internal sealed record RecordFkInfo(
    string TableSetMember,
    INamedTypeSymbol RecordSymbol,
    ImmutableArray<FkParameter> Parameters);
