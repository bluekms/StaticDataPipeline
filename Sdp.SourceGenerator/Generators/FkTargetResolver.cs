using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class FkTargetResolver
{
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

        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(targetRecord);
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

        if (!SymbolEqualityComparer.Default.Equals(TypeClassifier.UnwrapNullable(parameterType), unwrappedTarget))
        {
            return false;
        }

        return TypeClassifier.IsNullable(parameterType) || !TypeClassifier.IsNullable(targetParam.Type);
    }
}
