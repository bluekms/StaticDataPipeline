using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class TypeClassifier
{
    private static readonly string[] SystemNamespaceChain = ["System"];
    private static readonly string[] SystemCollectionsImmutableChain = ["System", "Collections", "Immutable"];
    private static readonly string[] SystemCollectionsFrozenChain = ["System", "Collections", "Frozen"];

    public static ScalarKind ClassifyScalar(ITypeSymbol type)
    {
        var underlying = UnwrapNullable(type);

        if (underlying.TypeKind == TypeKind.Enum)
        {
            return ScalarKind.Enum;
        }

        return underlying.SpecialType switch
        {
            SpecialType.System_Boolean => ScalarKind.Bool,
            SpecialType.System_Byte => ScalarKind.Byte,
            SpecialType.System_SByte => ScalarKind.SByte,
            SpecialType.System_Int16 => ScalarKind.Int16,
            SpecialType.System_UInt16 => ScalarKind.UInt16,
            SpecialType.System_Int32 => ScalarKind.Int32,
            SpecialType.System_UInt32 => ScalarKind.UInt32,
            SpecialType.System_Int64 => ScalarKind.Int64,
            SpecialType.System_UInt64 => ScalarKind.UInt64,
            SpecialType.System_Single => ScalarKind.Single,
            SpecialType.System_Double => ScalarKind.Double,
            SpecialType.System_Decimal => ScalarKind.Decimal,
            SpecialType.System_String => ScalarKind.String,
            SpecialType.System_Char => ScalarKind.Char,
            SpecialType.System_DateTime => ScalarKind.DateTime,
            _ => ClassifyByMetadataName(underlying),
        };
    }

    public static bool IsNullable(ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType &&
            namedType.IsValueType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }

    public static CollectionInfo? ClassifyCollection(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return null;
        }

        var definition = namedType.OriginalDefinition;

        if (IsBclType(definition, "ImmutableArray`1", SystemCollectionsImmutableChain))
        {
            var elementType = namedType.TypeArguments[0];
            return new CollectionInfo(
                Kind: CollectionKind.ImmutableArray,
                Length: 0,
                ElementType: elementType,
                ElementKind: ClassifyScalar(elementType),
                ValueNested: null,
                KeyType: null,
                KeyKind: ScalarKind.Unsupported,
                Separator: null,
                MinCount: null,
                MaxCount: null);
        }

        if (IsBclType(definition, "FrozenSet`1", SystemCollectionsFrozenChain))
        {
            var elementType = namedType.TypeArguments[0];
            return new CollectionInfo(
                Kind: CollectionKind.FrozenSet,
                Length: 0,
                ElementType: elementType,
                ElementKind: ClassifyScalar(elementType),
                ValueNested: null,
                KeyType: null,
                KeyKind: ScalarKind.Unsupported,
                Separator: null,
                MinCount: null,
                MaxCount: null);
        }

        if (IsBclType(definition, "FrozenDictionary`2", SystemCollectionsFrozenChain))
        {
            var keyType = namedType.TypeArguments[0];
            var valueType = namedType.TypeArguments[1];
            return new CollectionInfo(
                Kind: CollectionKind.FrozenDictionary,
                Length: 0,
                ElementType: valueType,
                ElementKind: ClassifyScalar(valueType),
                ValueNested: null,
                KeyType: keyType,
                KeyKind: ClassifyScalar(keyType),
                Separator: null,
                MinCount: null,
                MaxCount: null);
        }

        return null;
    }

    public static bool IsTypedRangeSupported(ScalarKind kind)
    {
        return IsNumeric(kind) || kind
            is ScalarKind.String
            or ScalarKind.Enum
            or ScalarKind.DateTime
            or ScalarKind.DateTimeOffset
            or ScalarKind.DateOnly
            or ScalarKind.TimeOnly
            or ScalarKind.TimeSpan;
    }

    public static bool IsNumeric(ScalarKind kind)
    {
        return kind
            is ScalarKind.Byte
            or ScalarKind.SByte
            or ScalarKind.Int16
            or ScalarKind.UInt16
            or ScalarKind.Int32
            or ScalarKind.UInt32
            or ScalarKind.Int64
            or ScalarKind.UInt64
            or ScalarKind.Single
            or ScalarKind.Double
            or ScalarKind.Decimal;
    }

    public static bool IsDirectlyParsable(ScalarKind kind)
    {
        return kind
            is ScalarKind.Bool
            or ScalarKind.Byte
            or ScalarKind.SByte
            or ScalarKind.Int16
            or ScalarKind.UInt16
            or ScalarKind.Int32
            or ScalarKind.UInt32
            or ScalarKind.Int64
            or ScalarKind.UInt64
            or ScalarKind.Single
            or ScalarKind.Double
            or ScalarKind.Decimal
            or ScalarKind.String
            or ScalarKind.Char
            or ScalarKind.Guid;
    }

    private static ScalarKind ClassifyByMetadataName(ITypeSymbol type)
    {
        if (type.ContainingType is not null || !IsInNamespace(type.ContainingNamespace, SystemNamespaceChain))
        {
            return ScalarKind.Unsupported;
        }

        return type.MetadataName switch
        {
            "Guid" => ScalarKind.Guid,
            "DateTimeOffset" => ScalarKind.DateTimeOffset,
            "DateOnly" => ScalarKind.DateOnly,
            "TimeOnly" => ScalarKind.TimeOnly,
            "TimeSpan" => ScalarKind.TimeSpan,
            _ => ScalarKind.Unsupported,
        };
    }

    // 표시 문자열 비교는 타입 파라미터 선언명("T", "TKey, TValue")에 결합되고 동명 사용자 타입을 구분하지
    // 못하므로, 메타데이터 이름과 global 루트까지의 네임스페이스 체인으로 BCL 타입을 판정한다.
    private static bool IsBclType(ITypeSymbol type, string metadataName, string[] namespaceChain)
    {
        if (type.ContainingType is not null || type.MetadataName != metadataName)
        {
            return false;
        }

        return IsInNamespace(type.ContainingNamespace, namespaceChain);
    }

    private static bool IsInNamespace(INamespaceSymbol? containingNamespace, string[] namespaceChain)
    {
        var current = containingNamespace;
        for (var i = namespaceChain.Length - 1; i >= 0; i--)
        {
            if (current is null || current.IsGlobalNamespace || current.Name != namespaceChain[i])
            {
                return false;
            }

            current = current.ContainingNamespace;
        }

        return current is { IsGlobalNamespace: true };
    }

    // Nullable<T> 면 T 를, 아니면 입력 그대로를 돌려준다. 진단(Generator)과 생성 코드(Emitter)가
    // 같은 언랩 의미를 공유해야 하므로 단일 구현을 여기서만 유지한다.
    public static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.IsValueType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }

        return type;
    }
}
