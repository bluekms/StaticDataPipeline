using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class CsvMapperGenerator
{
    private const string StaticDataRecordAttributeMetadataName = "Sdp.Attributes.StaticDataRecordAttribute";

    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        var analyses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                StaticDataRecordAttributeMetadataName,
                predicate: static (node, _) => node is RecordDeclarationSyntax,
                transform: static (attributeContext, cancellationToken) =>
                    Analyze(attributeContext, cancellationToken));

        context.RegisterSourceOutput(analyses, static (sourceProductionContext, analysis) =>
        {
            foreach (var diagnostic in analysis.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic);
            }

            if (!analysis.CanEmitMapperShell)
            {
                return;
            }

            var source = CsvMapperEmitter.Emit(analysis);
            sourceProductionContext.AddSource(analysis.HintName, source);
        });
    }

    private static RecordAnalysis Analyze(
        GeneratorAttributeSyntaxContext attributeContext,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<Diagnostic>();

        var symbol = (INamedTypeSymbol)attributeContext.TargetSymbol;
        var syntax = (RecordDeclarationSyntax)attributeContext.TargetNode;

        // 제네릭 record(또는 제네릭 타입에 중첩된 record)는 type parameter 가 빠진 partial 선언이
        // 방출되어 생성 코드가 깨지므로 진단으로 거부한다. 매퍼 셸도 arity 불일치라 방출할 수 없다.
        if (IsGenericOrNestedInGeneric(symbol))
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordCannotBeGeneric,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));

            return new RecordAnalysis(
                symbol,
                ImmutableArray<ParameterAnalysis>.Empty,
                diagnostics,
                CanEmit: false,
                CanEmitMapperShell: false);
        }

        var isRecordPartial = syntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isRecordPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustBePartial,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));
        }

        var allContainingPartial = ContainingTypePartialChecker.Check(symbol, diagnostics);

        var canEmit = false;
        var canEmitMapperShell = false;

        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(symbol);
        if (primaryCtor is null)
        {
            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RecordMustHaveSinglePrimaryConstructor,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString()));

            return new RecordAnalysis(
                symbol,
                ImmutableArray<ParameterAnalysis>.Empty,
                diagnostics,
                canEmit,
                canEmitMapperShell);
        }

        var parameters = ParameterAnalyzer.CollectParameters(
            primaryCtor,
            cancellationToken);

        // emit(EmitHelpersRecursive)은 nested record 트리 전체에 헬퍼를 방출하므로, 검증도 같은 범위를
        // 가져야 한다. root 와 nested 의 모든 파라미터를 경로 이름과 함께 평탄화해 같은 목록으로 순회한다.
        var allParameters = FlattenParameters(parameters);

        ValidateKeyUniqueness(symbol, syntax, parameters, diagnostics);

        ValidateCountRangeUsage(symbol, syntax, allParameters, diagnostics);

        ValidateLengthAndSingleColumnCollectionUsage(symbol, syntax, allParameters, diagnostics);

        canEmitMapperShell = isRecordPartial && allContainingPartial;
        if (canEmitMapperShell)
        {
            var specificallyRejected = ValidateCollectionParameterUsage(symbol, syntax, allParameters, diagnostics);

            specificallyRejected.UnionWith(ValidateRedundantTypedRange(symbol, syntax, allParameters, diagnostics));

            specificallyRejected.UnionWith(ValidateNumericRangeBounds(symbol, syntax, allParameters, diagnostics));

            specificallyRejected.UnionWith(ValidateTypedRangeBounds(symbol, syntax, allParameters, diagnostics));

            specificallyRejected.UnionWith(ValidateAttributeApplicability(symbol, syntax, allParameters, diagnostics));

            specificallyRejected.UnionWith(ValidateLengthValue(symbol, syntax, allParameters, diagnostics));

            specificallyRejected.UnionWith(ValidateRangeOrdering(symbol, syntax, allParameters, diagnostics));

            specificallyRejected.UnionWith(ValidateFrozenDictionaryKeys(symbol, syntax, allParameters, diagnostics));

            // specificallyRejected 에는 중첩 경로("Outer.Inner")도 들어오므로 root 파라미터가 그 사유로
            // emit 불가가 된 경우 prefix 매칭으로 제외해 전용 진단과 SDP0004 의 이중 보고를 막는다.
            var unsupportedParameterNames = parameters
                .Where(param => !ParameterEmittability.IsParameterEmittable(param))
                .Where(param => !specificallyRejected.Contains(param.Name))
                .Where(param => !specificallyRejected.Any(
                    path => path.StartsWith(param.Name + ".", StringComparison.Ordinal)))
                .Select(param => param.Name)
                .ToList();

            if (unsupportedParameterNames.Count > 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.UnsupportedMapperParameter,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    string.Join(", ", unsupportedParameterNames)));
            }

            canEmit = specificallyRejected.Count == 0 && unsupportedParameterNames.Count == 0;
        }

        return new RecordAnalysis(symbol, parameters, diagnostics, canEmit, canEmitMapperShell);
    }

    private static bool IsGenericOrNestedInGeneric(INamedTypeSymbol symbol)
    {
        if (symbol.IsGenericType)
        {
            return true;
        }

        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }

    // 검증용 평탄화 항목. Path 는 진단 메시지에 그대로 들어가는 파라미터 경로다(예: "Position.X").
    private sealed record QualifiedParameter(string Path, ParameterAnalysis Parameter);

    private static List<QualifiedParameter> FlattenParameters(ImmutableArray<ParameterAnalysis> parameters)
    {
        var result = new List<QualifiedParameter>();
        var visitedNestedRecords = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        AddParameters(parameters, pathPrefix: string.Empty, visitedNestedRecords, result);
        return result;
    }

    private static void AddParameters(
        ImmutableArray<ParameterAnalysis> parameters,
        string pathPrefix,
        HashSet<INamedTypeSymbol> visitedNestedRecords,
        List<QualifiedParameter> result)
    {
        foreach (var param in parameters)
        {
            // [Ignore] 파라미터는 매핑하지 않으므로(default 주입) 검증 대상이 아니다.
            if (param.IsIgnored)
            {
                continue;
            }

            var path = pathPrefix.Length == 0 ? param.Name : pathPrefix + "." + param.Name;
            result.Add(new QualifiedParameter(path, param));

            // 같은 nested record 가 여러 곳에서 공유되면 첫 방문만 평탄화해 중복 진단을 막는다.
            if (param.Nested is { } nested && visitedNestedRecords.Add(nested.Symbol))
            {
                AddParameters(nested.Parameters, path, visitedNestedRecords, result);
            }

            if (param.Collection?.ValueNested is { } valueNested && visitedNestedRecords.Add(valueNested.Symbol))
            {
                AddParameters(valueNested.Parameters, path, visitedNestedRecords, result);
            }

            if (param.Collection?.ElementNested is { } elementNested && visitedNestedRecords.Add(elementNested.Symbol))
            {
                AddParameters(elementNested.Parameters, path, visitedNestedRecords, result);
            }
        }
    }

    private static void ValidateKeyUniqueness(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        ImmutableArray<ParameterAnalysis> parameters,
        List<Diagnostic> diagnostics)
    {
        var keyCount = parameters.Count(static param => param.IsKey);
        if (keyCount <= 1)
        {
            return;
        }

        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.MultipleKeyAttributes,
            syntax.Identifier.GetLocation(),
            symbol.ToDisplayString(),
            keyCount));
    }

    // 컬렉션 자체의 nullable 여부와 dict + [SingleColumnCollection] 조합은 전용 진단으로 명확히 거부한다.
    // 반환된 이름들은 generic UnsupportedMapperParameter 진단에서 제외해 중복 보고를 막는다.
    private static HashSet<string> ValidateCollectionParameterUsage(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;

            if (IsNullableCollection(param.Type))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.NullableCollectionNotSupported,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
                rejected.Add(qualified.Path);
                continue;
            }

            if (param.HasSingleColumnCollectionAttribute &&
                param.Collection is { Kind: CollectionKind.FrozenDictionary })
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.SingleColumnCollectionNotAllowedOnDictionary,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }

    // 컬렉션 타입이 그 자체로 nullable 인지 판정한다.
    // ImmutableArray<T>? 는 Nullable<ImmutableArray<T>> (값 타입), FrozenSet<T>?/FrozenDictionary<,>? 는 annotated 참조 타입.
    // 원소만 nullable 인 ImmutableArray<int?> 는 여기에 걸리지 않는다.
    private static bool IsNullableCollection(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nullableValue &&
            nullableValue.IsValueType &&
            nullableValue.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return TypeClassifier.ClassifyCollection(nullableValue.TypeArguments[0]) is not null;
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return TypeClassifier.ClassifyCollection(type) is not null;
        }

        return false;
    }

    // 숫자 파라미터에 그 타입으로 표현할 수 없는 [Range] 경계(예: byte 에 300, float 에 double.MaxValue)가
    // 붙으면 생성 코드의 비교식이 컴파일되지 않거나(리터럴 오버플로) 경계가 조용히 무시되므로 전용 진단으로
    // 거부한다. Infinity/NaN 경계도 리터럴로 표현할 수 없어 함께 거부한다. 반환된 이름들은 generic 진단에서
    // 제외해 중복을 막는다.
    // [Range(typeof(byte), "0", "300")] 처럼 typeof 인자 형태를 썼지만 숫자 [Range(0, 300)] 형태로
    // 그대로 표현 가능한 타입은 거부한다. int/double 생성자로 정확히 표현되는 종류만 대상이며,
    // long/ulong/decimal/날짜·시간/enum 처럼 숫자 리터럴로 표현 불가능한 종류는 typed 형태가 정당하므로 건너뛴다.
    private static HashSet<string> ValidateRedundantTypedRange(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.Range is not { ArgKind: RangeArgKind.Typed } range)
            {
                continue;
            }

            var scalarKind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
            if (!IsNumericRangeLiteralExpressible(scalarKind))
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RedundantTypedRange,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                qualified.Path,
                range.Minimum,
                range.Maximum));
            rejected.Add(qualified.Path);
        }

        return rejected;
    }

    // int 생성자(Byte~UInt32, Int32)나 double 생성자(Single, Double)로 값 전체를 정확히 표현할 수 있는 종류.
    // Int64/UInt64 는 double 정밀도(2^53)를 넘어설 수 있고 Decimal 도 정밀도 때문에 typed 형태가 필요하므로 제외한다.
    private static bool IsNumericRangeLiteralExpressible(ScalarKind kind)
    {
        return kind
            is ScalarKind.Byte
            or ScalarKind.SByte
            or ScalarKind.Int16
            or ScalarKind.UInt16
            or ScalarKind.Int32
            or ScalarKind.UInt32
            or ScalarKind.Single
            or ScalarKind.Double;
    }

    private static HashSet<string> ValidateNumericRangeBounds(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.Range is not { ArgKind: RangeArgKind.Numeric } range)
            {
                continue;
            }

            var scalarKind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
            if (!RangeBoundsExceedType(scalarKind, range.Minimum, range.Maximum))
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RangeBoundOutOfTypeRange,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                qualified.Path));
            rejected.Add(qualified.Path);
        }

        return rejected;
    }

    // long/ulong/decimal 은 SDP0011 거부 대상이 아니라 typed [Range] 가 허용된다. 그 경계 문자열이 타입 범위를
    // 벗어나거나 파싱되지 않으면(예: [Range(typeof(long), "0", "99999999999999999999")]) 런타임 로드 시점에
    // 파싱 예외로 터지므로, 생성 시점에 SDP0010 으로 미리 잡는다. 런타임과 동일한 NumberStyles/culture 로 검사해
    // 진단이 실제 실패 케이스와 정확히 일치하게 한다.
    private static HashSet<string> ValidateTypedRangeBounds(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.Range is not { ArgKind: RangeArgKind.Typed } range)
            {
                continue;
            }

            var scalarKind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
            var scalarType = param.IsCollection ? param.Collection!.ElementType : param.Type;
            if (!TypedBoundOutOfRange(param, scalarKind, scalarType, range.Minimum as string, range.Maximum as string))
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.RangeBoundOutOfTypeRange,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                qualified.Path));
            rejected.Add(qualified.Path);
        }

        return rejected;
    }

    // 경계 문자열이 대상 타입으로 파싱되지 않으면(범위 초과 또는 형식 오류) true.
    // Int64/UInt64/Decimal/Enum/DateTime/DateTimeOffset/TimeSpan 이 대상이다. 작은 숫자형은 SDP0011 로
    // typed 자체가 거부되고, string 은 '표현 범위 초과' 개념이 적용되지 않으며, DateOnly/TimeOnly 는
    // netstandard2.0 제너레이터에서 파싱할 수 없어 제외한다. 날짜·시간 경계는 emitter 가 static readonly
    // 필드의 ParseExact 초기화식으로 방출하므로 파싱 불가 경계를 통과시키면 첫 로드에서
    // TypeInitializationException 으로 터진다 — 생성 시점에 거부한다. 포맷이 없는 파라미터는 emit 자체가
    // 다른 진단으로 거부되므로 건너뛴다. 단, null 경계는 emitter 가 리터럴로 방출할 수 없으므로 종류와
    // 무관하게 모든 typed 경계에서 거부한다. enum 경계는 emitter(EnumBoundExpr)가 멤버명이 아니면 텍스트를
    // 리터럴로 그대로 방출하므로, 멤버명 또는 정수로 파싱 가능한 값만 허용한다.
    private static bool TypedBoundOutOfRange(
        ParameterAnalysis param,
        ScalarKind kind,
        ITypeSymbol type,
        string? minimum,
        string? maximum)
    {
        if (minimum is null || maximum is null)
        {
            return true;
        }

        if (kind == ScalarKind.Enum)
        {
            var enumType = (INamedTypeSymbol)TypeClassifier.UnwrapNullable(type);
            return !IsValidEnumBound(enumType, minimum) || !IsValidEnumBound(enumType, maximum);
        }

        return kind switch
        {
            ScalarKind.Int64 => !ParsesAsInt64(minimum) || !ParsesAsInt64(maximum),
            ScalarKind.UInt64 => !ParsesAsUInt64(minimum) || !ParsesAsUInt64(maximum),
            ScalarKind.Decimal => !ParsesAsDecimal(minimum) || !ParsesAsDecimal(maximum),
            ScalarKind.DateTime => param.DateTimeFormat is { } dateTimeFormat
                && (!ParsesAsDateTime(minimum, dateTimeFormat) || !ParsesAsDateTime(maximum, dateTimeFormat)),
            ScalarKind.DateTimeOffset => param.DateTimeFormat is { } dateTimeOffsetFormat
                && (!ParsesAsDateTimeOffset(minimum, dateTimeOffsetFormat)
                    || !ParsesAsDateTimeOffset(maximum, dateTimeOffsetFormat)),
            ScalarKind.TimeSpan => param.TimeSpanFormat is { } timeSpanFormat
                && (!ParsesAsTimeSpan(minimum, timeSpanFormat) || !ParsesAsTimeSpan(maximum, timeSpanFormat)),
            _ => false,
        };
    }

    // emitter 의 EnumBoundExpr 와 같은 판정을 적용한다: enum 의 const 멤버명이거나,
    // 비교 누산 타입(u64 enum 은 ulong, 그 외 long)으로 파싱 가능한 정수만 유효한 경계다.
    // 텍스트가 리터럴로 그대로 방출되므로 공백을 허용하는 NumberStyles.Integer 대신
    // 부호만 허용하는 NumberStyles.AllowLeadingSign 으로 검사해 깨진 리터럴 방출을 막는다.
    private static bool IsValidEnumBound(INamedTypeSymbol enumType, string text)
    {
        var isMember = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(field => field.IsConst && field.Name == text);
        if (isMember)
        {
            return true;
        }

        var isUnsigned64 = enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64;
        if (isUnsigned64)
        {
            // ulong.TryParse 는 값이 0이면 음수 부호("-0")도 허용하지만, 텍스트를 그대로 방출하면
            // -0UL 은 컴파일되지 않으므로(ulong 에 단항 - 없음) 음수 부호 텍스트는 경계로 거부한다.
            return !text.StartsWith("-", StringComparison.Ordinal)
                && ulong.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
        }

        return long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
    }

    private static bool ParsesAsInt64(string text)
        => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    private static bool ParsesAsUInt64(string text)
        => ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    private static bool ParsesAsDecimal(string text)
        => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private static bool ParsesAsDateTime(string text, string format)
        => DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool ParsesAsDateTimeOffset(string text, string format)
        => DateTimeOffset.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool ParsesAsTimeSpan(string text, string format)
        => TimeSpan.TryParseExact(text, format, CultureInfo.InvariantCulture, out _);

    private static bool RangeBoundsExceedType(ScalarKind kind, object? minimum, object? maximum)
    {
        if (!TryGetNumericBounds(kind, out var typeMin, out var typeMax))
        {
            return false;
        }

        return BoundExceeds(minimum, typeMin, typeMax) || BoundExceeds(maximum, typeMin, typeMax);
    }

    private static bool BoundExceeds(object? value, double typeMin, double typeMax)
    {
        if (!TryConvertToDouble(value, out var bound))
        {
            return false;
        }

        // Infinity/NaN 은 어떤 숫자 타입의 리터럴로도 방출할 수 없으므로(생성 코드 CS0103/구문 오류) 거부한다.
        if (double.IsNaN(bound) || double.IsInfinity(bound))
        {
            return true;
        }

        return bound < typeMin || bound > typeMax;
    }

    // [Range] 경계는 RangeAttribute(int,int)/(double,double) 로 들어와 박싱된 int 또는 double 이지만,
    // 다른 숫자 타입도 방어적으로 받는다. 64비트 경계의 미세 정밀도는 현실적 입력에서 문제되지 않는다.
    private static bool TryConvertToDouble(object? value, out double result)
    {
        switch (value)
        {
            case sbyte v: result = v; return true;
            case byte v: result = v; return true;
            case short v: result = v; return true;
            case ushort v: result = v; return true;
            case int v: result = v; return true;
            case uint v: result = v; return true;
            case long v: result = v; return true;
            case ulong v: result = v; return true;
            case float v: result = v; return true;
            case double v: result = v; return true;
            default: result = 0; return false;
        }
    }

    private static bool TryGetNumericBounds(ScalarKind kind, out double min, out double max)
    {
        switch (kind)
        {
            case ScalarKind.SByte: min = sbyte.MinValue; max = sbyte.MaxValue; return true;
            case ScalarKind.Byte: min = byte.MinValue; max = byte.MaxValue; return true;
            case ScalarKind.Int16: min = short.MinValue; max = short.MaxValue; return true;
            case ScalarKind.UInt16: min = ushort.MinValue; max = ushort.MaxValue; return true;
            case ScalarKind.Int32: min = int.MinValue; max = int.MaxValue; return true;
            case ScalarKind.UInt32: min = uint.MinValue; max = uint.MaxValue; return true;
            case ScalarKind.Int64: min = long.MinValue; max = long.MaxValue; return true;
            case ScalarKind.UInt64: min = ulong.MinValue; max = ulong.MaxValue; return true;
            case ScalarKind.Single: min = float.MinValue; max = float.MaxValue; return true;
            case ScalarKind.Double: min = double.MinValue; max = double.MaxValue; return true;
            case ScalarKind.Decimal: min = (double)decimal.MinValue; max = (double)decimal.MaxValue; return true;
            default: min = 0; max = 0; return false;
        }
    }

    private static void ValidateCountRangeUsage(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (!param.HasCountRangeAttribute)
            {
                continue;
            }

            if (!param.HasSingleColumnCollectionAttribute)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.CountRangeRequiresSingleColumnCollection,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
            }

            if (param.HasLengthAttribute)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.CountRangeAndLengthMutuallyExclusive,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
            }
        }
    }

    // [SingleColumnCollection] 이 붙으면 single-column 매핑 경로로 전환되어 [Length] 의 고정 길이 검증이
    // 적용되지 않는다. 조용히 무시하는 대신 [CountRange]+[Length](SDP0005)와 대칭으로 명시 거부한다.
    private static void ValidateLengthAndSingleColumnCollectionUsage(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.HasLengthAttribute && param.HasSingleColumnCollectionAttribute)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.LengthAndSingleColumnCollectionMutuallyExclusive,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
            }
        }
    }

    // 적용 불가능한 위치에 부착된 attribute 는 조용히 무시되는 대신 SDP0016 으로 거부한다.
    // 빌드는 성공하는데 기대한 매핑(컬렉션 분할, null 치환, 포맷 파싱)이 적용되지 않는 무음 실패를 막는다.
    private static HashSet<string> ValidateAttributeApplicability(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;

            if (param.HasSingleColumnCollectionAttribute && param.Collection is null)
            {
                ReportAttributeNotApplicable(symbol, syntax, qualified, "SingleColumnCollection", diagnostics, rejected);
            }

            // [Length] 는 다중 컬럼 array/set/dict 전용이다. [SingleColumnCollection] 과의 조합은 SDP0013 이 보고한다.
            if (param.HasLengthAttribute
                && !param.HasSingleColumnCollectionAttribute
                && param.Collection?.Kind is not (CollectionKind.ImmutableArray or CollectionKind.FrozenSet or CollectionKind.FrozenDictionary))
            {
                ReportAttributeNotApplicable(symbol, syntax, qualified, "Length", diagnostics, rejected);
            }

            // [NullString] 은 nullable 스칼라(지정 문자열 → null) 또는 nullable 원소 컬렉션(원소 null 치환) 전용이다.
            // non-nullable 원소 컬렉션과 FrozenDictionary 에서는 emit 경로가 attribute 를 사용하지 않아
            // 조용히 무시되므로 거부한다.
            if (param.NullString is not null && !IsNullStringApplicable(param))
            {
                ReportAttributeNotApplicable(symbol, syntax, qualified, "NullString", diagnostics, rejected);
            }

            if (param.DateTimeFormat is not null && !IsFormatApplicable(param, IsDateTimeKind))
            {
                ReportAttributeNotApplicable(symbol, syntax, qualified, "DateTimeFormat", diagnostics, rejected);
            }

            if (param.TimeSpanFormat is not null && !IsFormatApplicable(param, IsTimeSpanKind))
            {
                ReportAttributeNotApplicable(symbol, syntax, qualified, "TimeSpanFormat", diagnostics, rejected);
            }

            // [RegularExpression] 은 string 스칼라 또는 string 원소 컬렉션 전용이다.
            if (param.RegexPattern is not null && !IsValidationKindApplicable(param, IsStringKind))
            {
                ReportAttributeNotApplicable(symbol, syntax, qualified, "RegularExpression", diagnostics, rejected);
            }

            // 숫자 형태 [Range] 는 숫자 스칼라 또는 숫자 원소 컬렉션 전용이다.
            // typed 형태([Range(typeof(T), ...)]) 는 string/날짜·시간 등을 지원하므로 여기서 다루지 않는다.
            if (param.Range is { ArgKind: RangeArgKind.Numeric }
                && !IsValidationKindApplicable(param, TypeClassifier.IsNumeric))
            {
                ReportAttributeNotApplicable(symbol, syntax, qualified, "Range", diagnostics, rejected);
            }
        }

        return rejected;
    }

    private static void ReportAttributeNotApplicable(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        QualifiedParameter qualified,
        string attributeName,
        List<Diagnostic> diagnostics,
        HashSet<string> rejected)
    {
        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.AttributeNotApplicable,
            syntax.Identifier.GetLocation(),
            symbol.ToDisplayString(),
            qualified.Path,
            attributeName));
        rejected.Add(qualified.Path);
    }

    private static bool IsNullStringApplicable(ParameterAnalysis param)
    {
        if (param.Collection is { } collection)
        {
            return collection.Kind != CollectionKind.FrozenDictionary
                && TypeClassifier.IsNullable(collection.ElementType);
        }

        return !param.IsRecord && param.IsNullable;
    }

    private static bool IsFormatApplicable(ParameterAnalysis param, Func<ScalarKind, bool> isApplicableKind)
    {
        if (param.Collection is { } collection)
        {
            // dict 값의 포맷 적용 여부는 value record 의 파라미터 분석에 위임한다.
            if (collection.Kind == CollectionKind.FrozenDictionary)
            {
                return true;
            }

            return isApplicableKind(collection.ElementKind);
        }

        return isApplicableKind(param.Kind);
    }

    // [Range]/[RegularExpression] 검증이 적용될 스칼라 종류를 판정한다. record 파라미터와
    // record 원소 컬렉션(FrozenDictionary 포함)은 검증 대상 스칼라가 없으므로 적용 불가다.
    private static bool IsValidationKindApplicable(ParameterAnalysis param, Func<ScalarKind, bool> isApplicableKind)
    {
        if (param.IsRecord)
        {
            return false;
        }

        if (param.Collection is { } collection)
        {
            return isApplicableKind(collection.ElementKind);
        }

        return isApplicableKind(param.Kind);
    }

    private static bool IsStringKind(ScalarKind kind)
    {
        return kind == ScalarKind.String;
    }

    private static bool IsDateTimeKind(ScalarKind kind)
    {
        return kind
            is ScalarKind.DateTime
            or ScalarKind.DateTimeOffset
            or ScalarKind.DateOnly
            or ScalarKind.TimeOnly;
    }

    private static bool IsTimeSpanKind(ScalarKind kind)
    {
        return kind == ScalarKind.TimeSpan;
    }

    // [Length(0)]·음수는 '동적 길이'(0 sentinel)와 충돌해 조용히 동적 모드로 둔갑하므로 생성 시점에 거부한다.
    private static HashSet<string> ValidateLengthValue(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (!param.HasLengthAttribute)
            {
                continue;
            }

            if (param.Collection is not { Kind: CollectionKind.ImmutableArray or CollectionKind.FrozenSet or CollectionKind.FrozenDictionary } collection)
            {
                continue;
            }

            if (collection.Length >= 1)
            {
                continue;
            }

            diagnostics.Add(Diagnostic.Create(
                SdpDiagnostics.LengthMustBePositive,
                syntax.Identifier.GetLocation(),
                symbol.ToDisplayString(),
                qualified.Path,
                collection.Length));
            rejected.Add(qualified.Path);
        }

        return rejected;
    }

    // min > max 경계는 모든 행을 거부하는 검사로 emit 되어 런타임에서야 드러나므로 생성 시점에 거부한다.
    private static HashSet<string> ValidateRangeOrdering(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;

            if (param.Range is { ArgKind: RangeArgKind.Numeric } range
                && TryConvertToDouble(range.Minimum, out var minimum)
                && TryConvertToDouble(range.Maximum, out var maximum)
                && minimum > maximum)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RangeMinimumExceedsMaximum,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    range.Minimum,
                    range.Maximum));
                rejected.Add(qualified.Path);
            }

            if (param.Range is { ArgKind: RangeArgKind.Typed } typedRange
                && TypedRangeOrderInverted(param, typedRange))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RangeMinimumExceedsMaximum,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    typedRange.Minimum,
                    typedRange.Maximum));
                rejected.Add(qualified.Path);
            }

            if (param.Collection is { MinCount: { } minCount, MaxCount: { } maxCount })
            {
                // 음수 경계는 count(항상 0 이상)와 비교되어 상한이 음수면 모든 행을 거부하는 검사로
                // emit 되므로 생성 시점에 거부한다(SDP0015 와 같은 근거).
                if (minCount < 0 || maxCount < 0)
                {
                    diagnostics.Add(Diagnostic.Create(
                        SdpDiagnostics.CountRangeMustBeNonNegative,
                        syntax.Identifier.GetLocation(),
                        symbol.ToDisplayString(),
                        qualified.Path,
                        minCount,
                        maxCount));
                    rejected.Add(qualified.Path);
                }
                else if (minCount > maxCount)
                {
                    diagnostics.Add(Diagnostic.Create(
                        SdpDiagnostics.RangeMinimumExceedsMaximum,
                        syntax.Identifier.GetLocation(),
                        symbol.ToDisplayString(),
                        qualified.Path,
                        minCount,
                        maxCount));
                    rejected.Add(qualified.Path);
                }
            }
        }

        return rejected;
    }

    // typed [Range] 경계도 min > max 면 모든 행을 거부하는 검사로 emit 되므로, 런타임 파싱
    // (InvariantCulture, ParseExact 포맷)과 동일한 규칙으로 비교 가능한 종류는 생성 시점에 거부한다.
    // string(ordinal 순서가 사용자 의도와 어긋날 수 있음)·enum(멤버명 해석이 emitter 와 중복됨)은
    // 제외하고, DateOnly/TimeOnly 는 netstandard2.0 제너레이터에서 파싱할 수 없어 제외한다.
    // 파싱에 실패하는 경계는 SDP0010(ValidateTypedRangeBounds)이 보고하므로 여기서는 건너뛴다.
    private static bool TypedRangeOrderInverted(ParameterAnalysis param, RangeInfo range)
    {
        if (range.Minimum is not string minText || range.Maximum is not string maxText)
        {
            return false;
        }

        var kind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
        switch (kind)
        {
            case ScalarKind.Int64:
                return long.TryParse(minText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longMin)
                    && long.TryParse(maxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longMax)
                    && longMin > longMax;
            case ScalarKind.UInt64:
                return ulong.TryParse(minText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongMin)
                    && ulong.TryParse(maxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongMax)
                    && ulongMin > ulongMax;
            case ScalarKind.Decimal:
                return decimal.TryParse(minText, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalMin)
                    && decimal.TryParse(maxText, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalMax)
                    && decimalMin > decimalMax;
            case ScalarKind.DateTime:
                return param.DateTimeFormat is { } dateTimeFormat
                    && DateTime.TryParseExact(minText, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeMin)
                    && DateTime.TryParseExact(maxText, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeMax)
                    && dateTimeMin > dateTimeMax;
            case ScalarKind.DateTimeOffset:
                return param.DateTimeFormat is { } dateTimeOffsetFormat
                    && DateTimeOffset.TryParseExact(minText, dateTimeOffsetFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetMin)
                    && DateTimeOffset.TryParseExact(maxText, dateTimeOffsetFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetMax)
                    && dateTimeOffsetMin > dateTimeOffsetMax;
            case ScalarKind.TimeSpan:
                return param.TimeSpanFormat is { } timeSpanFormat
                    && TimeSpan.TryParseExact(minText, timeSpanFormat, CultureInfo.InvariantCulture, out var timeSpanMin)
                    && TimeSpan.TryParseExact(maxText, timeSpanFormat, CultureInfo.InvariantCulture, out var timeSpanMax)
                    && timeSpanMin > timeSpanMax;
            default:
                return false;
        }
    }

    // FrozenDictionary 의 [Key] 구성 오류는 generic SDP0004 로는 원인이 드러나지 않으므로 전용 진단으로
    // 거부한다. 중첩 record 안의 dict 는 컬렉션 자체가 미지원이라 키를 고쳐도 해결되지 않으므로
    // root 파라미터만 검사한다.
    private static HashSet<string> ValidateFrozenDictionaryKeys(
        INamedTypeSymbol symbol,
        RecordDeclarationSyntax syntax,
        List<QualifiedParameter> allParameters,
        List<Diagnostic> diagnostics)
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            if (qualified.Path.IndexOf('.') >= 0)
            {
                continue;
            }

            var param = qualified.Parameter;
            if (param.Collection is not { Kind: CollectionKind.FrozenDictionary, ValueNested: { } valueNested } info)
            {
                continue;
            }

            var keyParams = valueNested.Parameters.Where(nestedParameter => nestedParameter.IsKey).ToList();
            if (keyParams.Count != 1)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.FrozenDictionaryValueMustHaveSingleKey,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    valueNested.Symbol.ToDisplayString(),
                    keyParams.Count));
                rejected.Add(qualified.Path);
                continue;
            }

            var keyParam = keyParams[0];
            if (!ParameterEmittability.IsFrozenDictionaryKeyCompatible(info, keyParam))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.FrozenDictionaryKeyTypeMismatch,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    info.KeyType!.ToDisplayString(),
                    valueNested.Symbol.ToDisplayString(),
                    keyParam.Name,
                    keyParam.Type.ToDisplayString()));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }
}
