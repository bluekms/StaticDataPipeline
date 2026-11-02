using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class FkTargetResolver
{
    public static ITypeSymbol ResolveFkValueType(ITypeSymbol parameterType)
    {
        var collection = TypeClassifier.ClassifyCollection(TypeClassifier.UnwrapNullable(parameterType));
        if (collection is null || collection.Kind == CollectionKind.FrozenDictionary)
        {
            return parameterType;
        }

        if (collection.ElementKind == ScalarKind.Unsupported)
        {
            return parameterType;
        }

        return collection.ElementType;
    }

    public static bool IsCollectionFk(ITypeSymbol parameterType)
    {
        var valueType = ResolveFkValueType(parameterType);

        return !SymbolEqualityComparer.Default.Equals(valueType, parameterType);
    }

    public static bool IsValidFkTarget(
        ITypeSymbol parameterType,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        string tableSetMember,
        string columnName)
    {
        if (!membersByName.TryGetValue(tableSetMember, out var targetRecord) || targetRecord is null)
        {
            return false;
        }

        var primaryCtor = SymbolResolver.FindPrimaryConstructor(targetRecord);
        if (primaryCtor is null)
        {
            return false;
        }

        var targetParam = primaryCtor.Parameters
            .FirstOrDefault(param => param.Name == columnName);
        if (targetParam is null)
        {
            return false;
        }

        var unwrappedTarget = TypeClassifier.UnwrapNullable(targetParam.Type);
        var targetCollection = TypeClassifier.ClassifyCollection(unwrappedTarget);
        if (targetCollection is not null)
        {
            return false;
        }

        var valueType = ResolveFkValueType(parameterType);
        if (!SymbolEqualityComparer.Default.Equals(TypeClassifier.UnwrapNullable(valueType), unwrappedTarget))
        {
            return false;
        }

        return TypeClassifier.IsNullable(valueType) || !TypeClassifier.IsNullable(targetParam.Type);
    }
}
