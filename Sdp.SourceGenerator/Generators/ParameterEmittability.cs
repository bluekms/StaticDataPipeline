using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ParameterEmittability
{
    internal static bool IsParameterEmittable(ParameterAnalysis param)
    {
        return IsParameterEmittableCore(param, isRoot: true);
    }

    private static bool IsParameterEmittableCore(ParameterAnalysis param, bool isRoot)
    {
        if (param.IsIgnored)
        {
            return true;
        }

        if (param.HasUnsupportedAttribute)
        {
            return false;
        }

        if (param.IsCollection)
        {
            return CanEmitCollection(param, isRoot);
        }

        if (param.IsRecord)
        {
            return CanEmitRecord(param);
        }

        return CanEmitPrimitive(param);
    }

    private static bool CanEmitCollection(ParameterAnalysis param, bool isRoot)
    {
        var collection = param.Collection!;

        var isMultiColumnArrayOrSet = collection.Kind is CollectionKind.ImmutableArray or CollectionKind.FrozenSet;

        // nested record 안에서는 array/set 만 지원한다(single-column/dict 는 root 만).
        if (!isRoot && !isMultiColumnArrayOrSet)
        {
            return false;
        }

        if (isMultiColumnArrayOrSet)
        {
            if (!IsElementValidationCompatible(param, collection))
            {
                return false;
            }

            if (collection.ElementNested is { } elementNested)
            {
                // nullable record 원소는 매퍼가 null 분기를 만들지 못하므로 미지원 유지.
                if (TypeClassifier.IsNullable(collection.ElementType))
                {
                    return false;
                }

                return elementNested.Parameters.All(
                    nestedParameter => IsParameterEmittableCore(nestedParameter, isRoot: false));
            }

            if (!IsEmittableCollectionElement(collection.ElementKind, param))
            {
                return false;
            }

            // nullable scalar 원소는 [NullString] 이 함께 있어야 null 로 매핑된다(single-column 경로와 동일).
            // 없으면 빈 셀에서 파싱 예외가 나므로 미지원.
            if (TypeClassifier.IsNullable(collection.ElementType) && param.NullString is null)
            {
                return false;
            }

            return true;
        }

        if (collection.Kind is CollectionKind.SingleColumnImmutableArray or CollectionKind.SingleColumnFrozenSet)
        {
            if (collection.Separator is not { Length: > 0 })
            {
                return false;
            }

            if (!IsElementValidationCompatible(param, collection))
            {
                return false;
            }

            if (!IsEmittableCollectionElement(collection.ElementKind, param))
            {
                return false;
            }

            // nullable element 는 [NullString] 이 함께 있어야 emit
            if (TypeClassifier.IsNullable(collection.ElementType) && param.NullString is null)
            {
                return false;
            }

            return true;
        }

        if (collection.Kind == CollectionKind.FrozenDictionary)
        {
            // dict 의 ElementKind 는 Unsupported 라 [Range]/[RegularExpression] 가 붙으면 여기서 거부된다.
            if (!IsElementValidationCompatible(param, collection))
            {
                return false;
            }

            return IsFrozenDictionaryEmittable(collection);
        }

        return false;
    }

    private static bool CanEmitRecord(ParameterAnalysis param)
    {
        // nullable record 파라미터는 매퍼가 null 분기를 만들지 못하므로 미지원(컬렉션 record 원소와 동일 정책).
        if (param.IsNullable)
        {
            return false;
        }

        // record 파라미터에 붙은 [Range]/[RegularExpression] 은 스칼라 전용이라 의미가 없고,
        // emit 시 object 비교 헬퍼가 방출되어 생성 코드가 컴파일되지 않으므로 거부한다(SDP0004).
        if (param.Range is not null || param.RegexPattern is not null)
        {
            return false;
        }

        return param.Nested!.Parameters.All(
            nestedParameter => IsParameterEmittableCore(nestedParameter, isRoot: false));
    }

    private static bool CanEmitPrimitive(ParameterAnalysis param)
    {
        if (param.Kind == ScalarKind.Unsupported)
        {
            return false;
        }

        if (param.Kind is ScalarKind.DateTime or ScalarKind.DateTimeOffset
            or ScalarKind.DateOnly or ScalarKind.TimeOnly)
        {
            if (param.DateTimeFormat is null)
            {
                return false;
            }
        }

        if (param.Kind == ScalarKind.TimeSpan)
        {
            if (param.TimeSpanFormat is null)
            {
                return false;
            }
        }

        if (param.IsNullable && param.NullString is null)
        {
            return false;
        }

        // typed range([Range(typeof(T), ...)]) 는 string/enum/숫자/DateTime/DateTimeOffset/TimeSpan 을 지원한다.
        if (param.Range is { ArgKind: RangeArgKind.Typed } && !TypeClassifier.IsTypedRangeSupported(param.Kind))
        {
            return false;
        }

        if (param.Range is { ArgKind: RangeArgKind.Numeric } && !TypeClassifier.IsNumeric(param.Kind))
        {
            return false;
        }

        if (param.RegexPattern is not null && param.Kind != ScalarKind.String)
        {
            return false;
        }

        return true;
    }

    // 컬렉션 파라미터에 [Range]/[RegularExpression] 가 붙으면 각 원소에 검증을 적용한다.
    // 원소 타입이 검증 대상과 맞지 않거나 nullable 이면 미지원 처리한다.
    private static bool IsElementValidationCompatible(ParameterAnalysis param, CollectionInfo collection)
    {
        if (param.Range is null && param.RegexPattern is null)
        {
            return true;
        }

        if (TypeClassifier.IsNullable(collection.ElementType))
        {
            return false;
        }

        if (param.Range is { ArgKind: RangeArgKind.Typed }
            && !TypeClassifier.IsTypedRangeSupported(collection.ElementKind))
        {
            return false;
        }

        if (param.Range is { ArgKind: RangeArgKind.Numeric }
            && !TypeClassifier.IsNumeric(collection.ElementKind))
        {
            return false;
        }

        if (param.RegexPattern is not null && collection.ElementKind != ScalarKind.String)
        {
            return false;
        }

        return true;
    }

    private static bool IsFrozenDictionaryEmittable(CollectionInfo info)
    {
        // value는 nested record, key는 scalar
        if (info.ValueNested is null)
        {
            return false;
        }

        if (TypeClassifier.IsNullable(info.KeyType!))
        {
            return false;
        }

        // nullable value record 는 매퍼가 null 분기를 만들지 못하므로 미지원(array/set 원소와 동일 정책).
        if (TypeClassifier.IsNullable(info.ElementType))
        {
            return false;
        }

        // nested 안 모든 param이 emit 가능해야 (재귀, isRoot=false: nested 안 컬렉션 차단)
        if (!info.ValueNested.Parameters.All(
                nestedParameter => IsParameterEmittableCore(nestedParameter, isRoot: false)))
        {
            return false;
        }

        // [Key] 표시 param이 정확히 1개
        var keyParams = info.ValueNested.Parameters.Where(nestedParameter => nestedParameter.IsKey).ToList();
        if (keyParams.Count != 1)
        {
            return false;
        }

        return IsFrozenDictionaryKeyCompatible(info, keyParams[0]);
    }

    // 키는 scalar(enum·DateTime·TimeSpan 계열 포함) 또는 KeyType 과 동일한 record 여야 한다.
    // scalar 키는 별도 파싱 없이 매핑된 value record 의 [Key] 속성에서 그대로 꺼내므로,
    // 그 [Key] 파라미터가 emit 가능(DateTime/TimeSpan 이면 포맷 보유)하면 키로도 안전하다.
    // kind 만 비교하면 서로 다른 enum 이나 nullable [Key] 가 통과해 생성 코드가 깨지므로
    // 타입 동일성과 non-nullable 여부까지 함께 검사한다.
    // CsvMapperGenerator.ValidateFrozenDictionaryKeys(SDP0018)가 같은 판정을 공유한다.
    public static bool IsFrozenDictionaryKeyCompatible(CollectionInfo info, ParameterAnalysis keyParam)
    {
        if (TypeClassifier.IsDirectlyParsable(info.KeyKind)
            || info.KeyKind is ScalarKind.Enum
            or ScalarKind.DateTime
            or ScalarKind.DateTimeOffset
            or ScalarKind.DateOnly
            or ScalarKind.TimeOnly
            or ScalarKind.TimeSpan)
        {
            return keyParam.Kind == info.KeyKind
                && !keyParam.IsNullable
                && SymbolEqualityComparer.Default.Equals(keyParam.Type, info.KeyType);
        }

        return keyParam.Nested is not null
               && !keyParam.IsNullable
               && SymbolEqualityComparer.Default.Equals(keyParam.Type, info.KeyType);
    }

    private static bool IsEmittableCollectionElement(ScalarKind kind, ParameterAnalysis param)
    {
        if (TypeClassifier.IsDirectlyParsable(kind))
        {
            return true;
        }

        if (kind == ScalarKind.Enum)
        {
            return true;
        }

        if (kind is ScalarKind.DateTime or ScalarKind.DateTimeOffset or ScalarKind.DateOnly or ScalarKind.TimeOnly)
        {
            return param.DateTimeFormat is not null;
        }

        if (kind == ScalarKind.TimeSpan)
        {
            return param.TimeSpanFormat is not null;
        }

        return false;
    }
}
