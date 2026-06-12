using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class TableSetGenerator
{
    private const string ManagerNamespace = "Sdp.Manager";
    private const string ManagerTypeName = "StaticDataManager";
    private const string TableNamespace = "Sdp.Table";
    private const string TableTypeName = "StaticDataTable";

    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var collected = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateManagerClass(node),
                transform: static (syntaxContext, cancellationToken) =>
                    Analyze(syntaxContext, cancellationToken))
            .Where(static analysis => analysis is not null)
            .Collect();

        var tableSets = collected
            .SelectMany(static (analyses, _) =>
            {
                var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var list = new List<ManagerAnalysis>(analyses.Length);
                foreach (var analysis in analyses)
                {
                    // 같은 TableSet 을 공유하는 매니저가 둘 이상이면 소스는 첫 매니저로 한 번만 방출한다.
                    // 진단은 모두 TableSet 심볼 기준(동일 id·위치·메시지)이라 첫 매니저가 이미 보고하므로,
                    // 두 번째 이후 매니저에서는 비워 중복 보고를 막는다(ViewSetGenerator 와 동일 정책).
                    list.Add(seen.Add(analysis!.TableSetSymbol)
                        ? analysis
                        : analysis with { CanEmit = false, Diagnostics = Array.Empty<Diagnostic>() });
                }

                return list;
            });

        context.RegisterSourceOutput(tableSets, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmit)
            {
                return;
            }

            var source = TableSetEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.HintName, source);
        });

        // 매니저 브리지는 TableSet 과 달리 매니저마다 하나씩 방출한다 — 같은 TableSet 을
        // 공유하는 매니저들도 각자 추상 훅 override 가 필요하다.
        var bridges = collected
            .SelectMany(static (analyses, _) =>
            {
                var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var list = new List<ManagerAnalysis>(analyses.Length);
                foreach (var analysis in analyses)
                {
                    if (seen.Add(analysis!.ManagerSymbol))
                    {
                        list.Add(analysis);
                    }
                }

                return list;
            });

        context.RegisterSourceOutput(bridges, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.ManagerDiagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmitBridge)
            {
                return;
            }

            var source = ManagerBridgeEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.BridgeHintName, source);
        });
    }

    private static bool IsCandidateManagerClass(SyntaxNode node)
        => node is ClassDeclarationSyntax classDecl && classDecl.BaseList is not null;

    private static ManagerAnalysis? Analyze(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
        if (declaredSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        if (baseType is null)
        {
            return null;
        }

        if (baseType.ContainingNamespace?.ToDisplayString() != ManagerNamespace)
        {
            return null;
        }

        if (baseType.Name != ManagerTypeName)
        {
            return null;
        }

        if (baseType.TypeArguments.Length is not (1 or 2))
        {
            return null;
        }

        if (baseType.TypeArguments[0] is not INamedTypeSymbol tableSetType)
        {
            // 타입 파라미터 패스스루(class MidManager<TS> : StaticDataManager<TS>)는 지원하지 않는다.
            // 조용히 탈락하면 로더 미생성 + 미구현 추상 멤버(CS0534)만 남으므로 전용 진단으로 알린다.
            // ViewSet 쪽 타입 인자는 ViewSetGenerator 가 같은 진단을 단일 소유한다.
            if (baseType.TypeArguments[0] is ITypeParameterSymbol typeParameter)
            {
                var diagnostic = Diagnostic.Create(
                    SdpDiagnostics.ManagerTypeArgumentMustBeClosed,
                    classDecl.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    typeParameter.Name);

                // TableSetSymbol 은 SelectMany 의 dedup 키와 HintName(CanEmit=false 라 미사용)에만
                // 쓰이므로, TableSet 이 해석되지 않는 이 경로에서는 매니저 자신을 넣는다.
                return new ManagerAnalysis(
                    symbol,
                    null,
                    symbol,
                    ImmutableArray<TableInfo>.Empty,
                    ImmutableArray<RecordFkInfo>.Empty,
                    Array.Empty<Diagnostic>(),
                    false,
                    new[] { diagnostic },
                    false);
            }

            return null;
        }

        // 매니저 partial 여부는 브리지(추상 훅 override) 방출 가능성을 가른다. partial 이 아니면
        // CS0534 만 남아 원인이 드러나지 않으므로 전용 진단으로 알린다.
        var managerDiagnostics = new List<Diagnostic>();
        var managerIsPartial = classDecl.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (!managerIsPartial)
        {
            managerDiagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ManagerMustBePartial,
                classDecl.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var managerContainingPartial = ContainingTypePartialChecker.Check(symbol, managerDiagnostics);

        // WithView 매니저(타입 인자 2개)는 BuildViewSet 훅도 연결해야 한다. TViewSet 이 타입
        // 파라미터면 SDP0218 을 ViewSetGenerator 가 보고하므로 여기서는 브리지만 막는다.
        INamedTypeSymbol? viewSetType = null;
        var viewSetArgumentValid = true;
        if (baseType.TypeArguments.Length == 2)
        {
            viewSetType = baseType.TypeArguments[1] as INamedTypeSymbol;
            viewSetArgumentValid = viewSetType is not null;
        }

        var diagnostics = new List<Diagnostic>();

        var tableSetPartial = CollectTableSetPartialDiagnostics(tableSetType, diagnostics, out var tableSetIsRecord);

        // record syntax 가 없는 TableSet(SDP0216)은 syntax 기반의 primary ctor 해석이 항상 실패해
        // 허위 SDP0006 이 따라붙으므로 멤버 수집을 건너뛴다.
        var tableInfos = ImmutableArray<TableInfo>.Empty;
        var allParametersValid = false;
        if (tableSetIsRecord)
        {
            tableInfos = CollectTableInfos(tableSetType, diagnostics, out allParametersValid, cancellationToken);
        }

        var tableSetOuterPartial = ContainingTypePartialChecker.Check(tableSetType, diagnostics);

        var membersByName = new Dictionary<string, INamedTypeSymbol?>(System.StringComparer.Ordinal);
        foreach (var tableInfo in tableInfos)
        {
            membersByName[tableInfo.ParameterName] = tableInfo.RecordSymbol;
        }

        var hasForeignKeySwitchConflict = ValidateForeignKeys(tableInfos, membersByName, diagnostics, cancellationToken);

        var recordFkInfos = CollectRecordFkInfos(tableInfos, membersByName, cancellationToken);

        var canEmit = tableSetPartial
            && tableSetOuterPartial
            && allParametersValid
            && !hasForeignKeySwitchConflict
            && tableInfos.All(tableInfo => tableInfo.IsPartial);

        // TableSet 로더가 방출되지 않으면 브리지가 존재하지 않는 정적 진입점을 참조해 연쇄
        // 오류(CS0117)가 나므로, 그 경우 브리지를 막아 CS0534 + SDP 진단으로 원인을 한 곳에 모은다.
        var canEmitBridge = managerIsPartial
            && managerContainingPartial
            && viewSetArgumentValid
            && canEmit;

        return new ManagerAnalysis(
            symbol,
            viewSetType,
            tableSetType,
            tableInfos,
            recordFkInfos,
            diagnostics,
            canEmit,
            managerDiagnostics,
            canEmitBridge);
    }

    private static ImmutableArray<RecordFkInfo> CollectRecordFkInfos(
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

            // FK 파라미터 분석은 레코드 타입당 한 번만 수행(dedup)하되, 검증 코드는 그 레코드를
            // 사용하는 모든 테이블 멤버마다 방출해야 한다. 레코드로 dedup 하면 같은 레코드를 쓰는
            // 두 번째 이후 테이블의 FK 검증이 누락된다.
            if (!cache.TryGetValue(table.RecordSymbol, out var paramInfos))
            {
                paramInfos = CollectFkParameters(table.RecordSymbol, membersByName);
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

    private static ImmutableArray<FkParameter> CollectFkParameters(
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

                if (attr.AttributeClass!.Name == "ForeignKeyAttribute" && args.Length >= 2 &&
                    args[0].Value is string tableSetName && args[1].Value is string columnName)
                {
                    var targetType = ResolveTargetColumnType(membersByName, tableSetName, columnName);
                    if (!IsValidFkTarget(param.Type, targetType))
                    {
                        continue;
                    }

                    branchBuilder.Add(new FkBranch(false, null, null, tableSetName, columnName, null));
                }
                else if (attr.AttributeClass.Name == "SwitchForeignKeyAttribute" && args.Length >= 4 &&
                         args[0].Value is string conditionColumn && args[1].Value is string conditionValue &&
                         args[2].Value is string switchTableSetName && args[3].Value is string switchColumnName)
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

                    branchBuilder.Add(new FkBranch(
                        true, conditionColumn, conditionValue, switchTableSetName, switchColumnName, conditionProperty.Type));
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

    // FK 파라미터가 타깃 컬럼을 검증 키로 쓸 수 있는지 판정한다. 부적합 사유별 진단은
    // ValidateFkTarget 가 따로 보고하므로 여기서는 emit 가능 여부만 가른다.
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

    // 매퍼가 지원하는 컬렉션 3종(ImmutableArray/FrozenSet/FrozenDictionary)이면 스칼라 타깃이 아니다.
    // 판정은 TypeClassifier 의 메타데이터 이름 기반 분류를 그대로 공유한다.
    private static bool IsScalarTargetType(ITypeSymbol type)
    {
        var collection = TypeClassifier.ClassifyCollection(type);
        return collection is null;
    }

    private static bool CollectTableSetPartialDiagnostics(
        INamedTypeSymbol tableSetType,
        List<Diagnostic> diagnostics,
        out bool isRecordInCurrentCompilation)
    {
        var tableSetSyntax = tableSetType.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<RecordDeclarationSyntax>()
            .FirstOrDefault();

        // record 선언 syntax 가 없으면(클래스로 선언했거나 참조 어셈블리 타입) partial 방출 자체가
        // 불가능하므로, CS 오류만 남기고 침묵하지 않도록 명시 진단을 보고한다.
        if (tableSetSyntax is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.TableSetMustBeRecordInCurrentCompilation,
                tableSetType.Locations.FirstOrDefault() ?? Location.None,
                tableSetType.ToDisplayString()));
            isRecordInCurrentCompilation = false;
            return false;
        }

        isRecordInCurrentCompilation = true;

        if (tableSetSyntax.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
        {
            return true;
        }

        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.TableSetRecordMustBePartial,
            tableSetSyntax.Identifier.GetLocation(),
            tableSetType.ToDisplayString()));
        return false;
    }

    private static ImmutableArray<TableInfo> CollectTableInfos(
        INamedTypeSymbol tableSetType,
        List<Diagnostic> diagnostics,
        out bool allParametersValid,
        CancellationToken cancellationToken)
    {
        allParametersValid = true;

        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(tableSetType);
        if (primaryCtor is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                tableSetType.Locations.FirstOrDefault() ?? Location.None,
                tableSetType.ToDisplayString()));
            return ImmutableArray<TableInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<TableInfo>(primaryCtor.Parameters.Length);

        foreach (var param in primaryCtor.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableType = TypeClassifier.UnwrapNullable(param.Type);
            if (tableType is not INamedTypeSymbol tableSymbol || !IsStaticDataTableSubclass(tableSymbol))
            {
                allParametersValid = false;
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.TableSetMemberMustBeStaticDataTable,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    param.Type.ToDisplayString()));
                continue;
            }

            var tableSyntax = tableSymbol.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            var isPartial = tableSyntax?.Modifiers
                .Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) ?? false;

            // syntax 가 없는 테이블(참조 어셈블리 타입)은 팩토리 partial 을 방출할 수 없다.
            // isPartial=false 로 canEmit 이 막히는 이유를 사용자가 알 수 있도록 명시 진단을 보고한다.
            if (tableSyntax is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.TableMustBeDeclaredInCurrentCompilation,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    tableSymbol.ToDisplayString()));
            }
            else if (!isPartial)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.StaticDataTableMustBePartial,
                    tableSyntax.Identifier.GetLocation(),
                    tableSymbol.ToDisplayString()));
            }

            var isNullable = param.NullableAnnotation == NullableAnnotation.Annotated;
            var recordType = ExtractRecordType(tableSymbol);
            builder.Add(new TableInfo(param.Name, tableSymbol, recordType, isPartial, isNullable));
        }

        return builder.ToImmutable();
    }

    // FK+SwitchFK 동시 부착(SDP0204) 또는 SwitchFK 조건 컬럼 불일치(SDP0210)가 하나라도 있으면 true 를 반환한다.
    // 호출부는 이 경우 canEmit 을 false 로 막아 모순된 검증 코드 방출을 차단한다.
    private static bool ValidateForeignKeys(
        ImmutableArray<TableInfo> tables,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var hasConflict = false;

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (table.RecordSymbol is null || !visited.Add(table.RecordSymbol))
            {
                continue;
            }

            hasConflict |= ValidateRecordForeignKeys(table.RecordSymbol, membersByName, diagnostics);
        }

        return hasConflict;
    }

    private static bool ValidateRecordForeignKeys(
        INamedTypeSymbol recordType,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(recordType);
        if (primaryCtor is null)
        {
            return false;
        }

        var hasConflict = false;

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
                hasConflict = true;
            }

            foreach (var attr in fkAttrs)
            {
                ValidateFkTarget(attr, isSwitch: false, param, recordType, membersByName, diagnostics);
            }

            foreach (var attr in switchFkAttrs)
            {
                ValidateFkTarget(attr, isSwitch: true, param, recordType, membersByName, diagnostics);
            }

            ValidateSwitchFkConditionUniqueness(recordType, param, switchFkAttrs, diagnostics);

            ValidateSwitchFkConditionColumnExists(recordType, param, switchFkAttrs, diagnostics);

            var conditionColumnConflict =
                ValidateSwitchFkConditionColumnConsistency(recordType, param, switchFkAttrs, diagnostics);
            hasConflict |= conditionColumnConflict;

            // 조건 컬럼이 attr 간 불일치(SDP0210)면 첫 attr 의 컬럼을 기준으로 한 값 검증이
            // 다른 컬럼용 값을 엉뚱한 enum 에 대해 오진(SDP0212)하므로 건너뛴다.
            if (!conditionColumnConflict)
            {
                ValidateSwitchFkConditionValue(recordType, param, switchFkAttrs, diagnostics);
            }
        }

        return hasConflict;
    }

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

        // 런타임은 enum 조건 컬럼을 enumValue.ToString()(멤버명)으로 비교한다(TableSetEmitter.IsFormattable=false).
        // 따라서 ConditionValue는 멤버명, 또는 미정의 값의 숫자 문자열과만 매칭될 수 있다. 비-enum은 포맷
        // 차이로 인한 오탐 위험이 커 검증하지 않는다.
        var conditionType = TypeClassifier.UnwrapNullable(conditionProperty.Type);
        if (conditionType.TypeKind != TypeKind.Enum || conditionType is not INamedTypeSymbol enumType)
        {
            return;
        }

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

    internal static bool HasFlagsAttribute(INamedTypeSymbol enumType)
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
    internal static bool FitsInEnumUnderlyingType(INamedTypeSymbol enumType, long value)
        => FitsInIntegralType(enumType.EnumUnderlyingType!.SpecialType, value);

    // long 으로 파싱된 값이 해당 정수 타입의 표현 범위 안에 있는지 판정한다.
    // TableSetEmitter.BuildTypedConditionLiteral 이 같은 판정으로 비-enum 정수 조건 컬럼의
    // 범위 밖 상수 비교(CS0652) 방출을 막는다.
    internal static bool FitsInIntegralType(SpecialType specialType, long value)
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

    // 조건 컬럼 불일치(SDP0210)를 보고했으면 true 를 반환한다. emitter 는 첫 branch 의 조건 컬럼으로
    // 모든 branch 를 비교하므로, 이 상태에서 방출된 코드는 잘못된 검증이 된다 — canEmit 차단 대상.
    private static bool ValidateSwitchFkConditionColumnConsistency(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var conditionColumns = new HashSet<string>(System.StringComparer.Ordinal);
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
            return false;
        }

        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.SwitchForeignKeyConditionColumnMismatch,
            ParameterLocation(param),
            recordType.Name,
            param.Name));
        return true;
    }

    private static void ValidateSwitchFkConditionColumnExists(
        INamedTypeSymbol recordType,
        IParameterSymbol param,
        List<AttributeData> switchFkAttrs,
        List<Diagnostic> diagnostics)
    {
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
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
        bool isSwitch,
        IParameterSymbol param,
        INamedTypeSymbol recordType,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        List<Diagnostic> diagnostics)
    {
        var args = attr.ConstructorArguments;
        string? tableSetName;
        string? columnName;

        if (isSwitch)
        {
            if (args.Length < 4)
            {
                return;
            }

            tableSetName = args[2].Value as string;
            columnName = args[3].Value as string;
        }
        else
        {
            if (args.Length < 2)
            {
                return;
            }

            tableSetName = args[0].Value as string;
            columnName = args[1].Value as string;
        }

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

        var targetParam = targetCtor.Parameters.FirstOrDefault(candidate => candidate.Name == columnName);
        if (targetParam is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.ForeignKeyTargetColumnNotFound,
                ParameterLocation(param),
                columnName,
                targetRecord.Name));
            return;
        }

        if (targetParam.GetAttributes().Any(targetAttribute =>
                IsSdpAttribute(targetAttribute)
                && targetAttribute.AttributeClass!.Name == "SingleColumnCollectionAttribute"))
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

        // emit 측(CollectFkParameters)이 컬렉션 타입 타깃 branch 를 버리는 것과 같은 기준으로 진단한다.
        // 진단 없이 branch 만 탈락하면 [ForeignKey]가 아무 검증도 하지 않는 무음 구멍이 된다.
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

        // non-nullable FK 가 nullable 타깃 컬럼을 참조하면 생성 코드(HashSet<T>.Add(T?))가 컴파일되지
        // 않으므로 emit 측(CollectFkParameters)과 같은 기준으로 거부한다. 반대 방향(nullable FK →
        // non-nullable 타깃)은 암시적 변환으로 안전해 허용한다.
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
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
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

            var key = conditionColumn + "\0" + conditionValue;
            if (!seen.Add(key))
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

    private static INamedTypeSymbol? ExtractRecordType(INamedTypeSymbol tableSymbol)
    {
        for (var type = tableSymbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == TableNamespace
                && type.Name == TableTypeName
                && type.TypeArguments.Length >= 2)
            {
                return type.TypeArguments[1] as INamedTypeSymbol;
            }
        }

        return null;
    }

    private static bool IsStaticDataTableSubclass(INamedTypeSymbol symbol)
    {
        for (var type = symbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == TableNamespace && type.Name == TableTypeName)
            {
                return true;
            }
        }

        return false;
    }

    internal sealed record ManagerAnalysis(
        INamedTypeSymbol ManagerSymbol,
        INamedTypeSymbol? ViewSetSymbol,
        INamedTypeSymbol TableSetSymbol,
        ImmutableArray<TableInfo> Tables,
        ImmutableArray<RecordFkInfo> RecordFkInfos,
        IReadOnlyList<Diagnostic> Diagnostics,
        bool CanEmit,
        IReadOnlyList<Diagnostic> ManagerDiagnostics,
        bool CanEmitBridge)
    {
        public string HintName
        {
            get
            {
                var qualified = TableSetSymbol.ToDisplayString(new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
                return qualified + ".TableSetLoader.g.cs";
            }
        }

        public string BridgeHintName
        {
            get
            {
                var qualified = ManagerSymbol.ToDisplayString(new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
                return qualified + ".Manager.g.cs";
            }
        }
    }

    internal sealed record TableInfo(
        string ParameterName,
        INamedTypeSymbol TableSymbol,
        INamedTypeSymbol? RecordSymbol,
        bool IsPartial,
        bool IsNullable);

    // ConditionType 은 switch branch 의 조건 프로퍼티 타입(분석 시점에 해석 완료). emit 측이 재조회하지 않게 한다.
    internal sealed record FkBranch(
        bool IsSwitch,
        string? ConditionColumn,
        string? ConditionValue,
        string TableSetMember,
        string TargetColumn,
        ITypeSymbol? ConditionType);

    internal sealed record FkParameter(
        string PropertyName,
        ITypeSymbol PropertyType,
        ImmutableArray<FkBranch> Branches);

    internal sealed record RecordFkInfo(
        string TableSetMember,
        INamedTypeSymbol RecordSymbol,
        ImmutableArray<FkParameter> Parameters);
}
