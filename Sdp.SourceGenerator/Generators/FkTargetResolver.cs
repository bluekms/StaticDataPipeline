using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record FkTargetResolution(string? TargetUnwrapMember);

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

    private static IParameterSymbol? FindTargetParameter(
        Dictionary<string, INamedTypeSymbol?> membersByName,
        string tableSetMember,
        string columnName)
    {
        if (!membersByName.TryGetValue(tableSetMember, out var targetRecord) || targetRecord is null)
        {
            return null;
        }

        var primaryCtor = SymbolResolver.FindPrimaryConstructor(targetRecord);
        if (primaryCtor is null)
        {
            return null;
        }

        return primaryCtor.Parameters
            .FirstOrDefault(param => param.Name == columnName);
    }

    private static IParameterSymbol? FindBrandingValueParameter(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return null;
        }

        var primaryCtor = SymbolResolver.FindPrimaryConstructor(namedType);
        if (primaryCtor is null || primaryCtor.Parameters.Length != 1)
        {
            return null;
        }

        var valueParam = primaryCtor.Parameters[0];
        var valueType = TypeClassifier.UnwrapNullable(valueParam.Type);
        var valueCollection = TypeClassifier.ClassifyCollection(valueType);
        if (valueCollection is not null)
        {
            return null;
        }

        var valueKind = TypeClassifier.ClassifyScalar(valueType);
        if (valueKind == ScalarKind.Unsupported)
        {
            return null;
        }

        return valueParam;
    }

    public static IParameterSymbol? ResolveTargetBrandingValue(ITypeSymbol fkValueType, ITypeSymbol targetType)
    {
        var unwrappedFk = TypeClassifier.UnwrapNullable(fkValueType);
        var unwrappedTarget = TypeClassifier.UnwrapNullable(targetType);
        if (SymbolEqualityComparer.Default.Equals(unwrappedFk, unwrappedTarget))
        {
            return null;
        }

        var brandingValue = FindBrandingValueParameter(unwrappedTarget);
        if (brandingValue is null)
        {
            return null;
        }

        var unwrappedBrandingValue = TypeClassifier.UnwrapNullable(brandingValue.Type);
        if (!SymbolEqualityComparer.Default.Equals(unwrappedFk, unwrappedBrandingValue))
        {
            return null;
        }

        return brandingValue;
    }

    public static bool IsNullabilityCompatible(
        ITypeSymbol fkValueType,
        ITypeSymbol targetType,
        IParameterSymbol? brandingValue)
    {
        if (TypeClassifier.IsNullable(fkValueType))
        {
            return true;
        }

        if (TypeClassifier.IsNullable(targetType))
        {
            return false;
        }

        return brandingValue is null || !TypeClassifier.IsNullable(brandingValue.Type);
    }

    public static FkTargetResolution? ResolveFkTarget(
        ITypeSymbol parameterType,
        Dictionary<string, INamedTypeSymbol?> membersByName,
        string tableSetMember,
        string columnName)
    {
        var targetParam = FindTargetParameter(membersByName, tableSetMember, columnName);
        if (targetParam is null)
        {
            return null;
        }

        var unwrappedTarget = TypeClassifier.UnwrapNullable(targetParam.Type);
        var targetCollection = TypeClassifier.ClassifyCollection(unwrappedTarget);
        if (targetCollection is not null)
        {
            return null;
        }

        var valueType = ResolveFkValueType(parameterType);
        var brandingValue = ResolveTargetBrandingValue(valueType, targetParam.Type);
        var unwrappedValue = TypeClassifier.UnwrapNullable(valueType);
        var isSameType = SymbolEqualityComparer.Default.Equals(unwrappedValue, unwrappedTarget);
        if (!isSameType && brandingValue is null)
        {
            return null;
        }

        var isNullabilityCompatible = IsNullabilityCompatible(valueType, targetParam.Type, brandingValue);
        if (!isNullabilityCompatible)
        {
            return null;
        }

        return new FkTargetResolution(brandingValue?.Name);
    }
}
