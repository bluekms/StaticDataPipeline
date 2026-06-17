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
        if (info.ValueNested is null)
        {
            return false;
        }

        if (TypeClassifier.IsNullable(info.KeyType!))
        {
            return false;
        }

        if (TypeClassifier.IsNullable(info.ElementType))
        {
            return false;
        }

        if (!info.ValueNested.Parameters.All(IsParameterEmittable))
        {
            return false;
        }

        var keyParams = info.ValueNested.Parameters
            .Where(nestedParameter => nestedParameter.IsKey)
            .ToList();

        if (keyParams.Count != 1)
        {
            return false;
        }

        return IsFrozenDictionaryKeyCompatible(info, keyParams[0]);
    }

    public static bool IsFrozenDictionaryKeyCompatible(CollectionInfo info, ParameterAnalysis keyParam)
    {
        if (TypeClassifier.IsDirectlyParsable(info.KeyKind)
            || info.KeyKind
                is ScalarKind.Enum
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

        if (kind
            is ScalarKind.DateTime
            or ScalarKind.DateTimeOffset
            or ScalarKind.DateOnly
            or ScalarKind.TimeOnly)
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
