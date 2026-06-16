using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ParameterEmittability
{
    internal static bool IsParameterEmittable(ParameterAnalysis param)
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
            return CanEmitCollection(param);
        }

        if (param.IsRecord)
        {
            return CanEmitRecord(param);
        }

        return CanEmitPrimitive(param);
    }

    private static bool CanEmitCollection(ParameterAnalysis param)
    {
        var collection = param.Collection!;

        if (collection.Kind is CollectionKind.ImmutableArray or CollectionKind.FrozenSet)
        {
            if (!AreValidationAttributesApplicable(param, collection))
            {
                return false;
            }

            if (collection.ElementNested is { } elementNested)
            {
                // nullable record 지원하지 않음
                if (TypeClassifier.IsNullable(collection.ElementType))
                {
                    return false;
                }

                return elementNested.Parameters.All(IsParameterEmittable);
            }

            if (!IsEmittableCollectionElement(collection.ElementKind, param))
            {
                return false;
            }

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

            if (!AreValidationAttributesApplicable(param, collection))
            {
                return false;
            }

            if (!IsEmittableCollectionElement(collection.ElementKind, param))
            {
                return false;
            }

            if (TypeClassifier.IsNullable(collection.ElementType) && param.NullString is null)
            {
                return false;
            }

            return true;
        }

        if (collection.Kind == CollectionKind.FrozenDictionary)
        {
            if (!AreValidationAttributesApplicable(param, collection))
            {
                return false;
            }

            return IsFrozenDictionaryEmittable(collection);
        }

        return false;
    }

    private static bool CanEmitRecord(ParameterAnalysis param)
    {
        if (param.IsNullable)
        {
            return false;
        }

        if (param.Range is not null || param.RegexPattern is not null)
        {
            return false;
        }

        return param.Nested!.Parameters.All(IsParameterEmittable);
    }

    private static bool CanEmitPrimitive(ParameterAnalysis param)
    {
        if (param.Kind == ScalarKind.Unsupported)
        {
            return false;
        }

        if (param.Kind
            is ScalarKind.DateTime
            or ScalarKind.DateTimeOffset
            or ScalarKind.DateOnly
            or ScalarKind.TimeOnly)
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

        if (param is { IsNullable: true, NullString: null })
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

    // 컬렉션에 붙은 [Range]/[RegularExpression] 검증 attribute 를 원소 타입에 적용할 수 있는지 판정한다.
    // 원소가 nullable 이거나 attribute 의 검증 종류와 원소 타입이 맞지 않으면 미지원이다.
    private static bool AreValidationAttributesApplicable(ParameterAnalysis param, CollectionInfo collection)
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

        // value record 안 모든 param이 emit 가능해야 한다(재귀).
        if (!info.ValueNested.Parameters.All(
                nestedParameter => IsParameterEmittable(nestedParameter)))
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
