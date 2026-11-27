using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record KeyIndexInfo(
    string KeyParameterName,
    string FullyQualifiedKeyTypeName,
    bool EmitsGet,
    bool EmitsTryGet);

internal static class KeyIndexAnalyzer
{
    private const string GetMethod = "Get";
    private const string TryGetMethod = "TryGet";

    public static KeyIndexInfo? Analyze(INamedTypeSymbol table, INamedTypeSymbol staticDataTableBase, INamedTypeSymbol record)
    {
        // 사용자 base 를 거쳐 상속한 Table 은 어느 쪽이 인덱스를 가질지 정할 수 없어 만들지 않는다.
        if (!SymbolEqualityComparer.Default.Equals(table.BaseType, staticDataTableBase))
        {
            return null;
        }

        if (HasNoKeyIndexAttribute(table))
        {
            return null;
        }

        var keyParameter = KeyParameterResolver.Find(record);
        if (keyParameter is null)
        {
            return null;
        }

        if (TypeClassifier.IsNullable(keyParameter.Type))
        {
            return null;
        }

        var emitsGet = !DeclaresLookup(table, GetMethod, keyParameter.Type, parameterCount: 1);
        var emitsTryGet = !DeclaresLookup(table, TryGetMethod, keyParameter.Type, parameterCount: 2);
        if (!emitsGet && !emitsTryGet)
        {
            return null;
        }

        return new KeyIndexInfo(
            keyParameter.Name,
            keyParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            emitsGet,
            emitsTryGet);
    }

    private static bool HasNoKeyIndexAttribute(INamedTypeSymbol table)
    {
        return table.GetAttributes()
            .Where(static x => SdpAttributeNames.IsSdpAttribute(x))
            .Any(static x => x.AttributeClass!.Name == SdpAttributeNames.NoKeyIndex);
    }

    private static bool DeclaresLookup(INamedTypeSymbol table, string methodName, ITypeSymbol keyType, int parameterCount)
    {
        return table.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(x => x.Parameters.Length == parameterCount)
            .Any(x => SymbolEqualityComparer.Default.Equals(x.Parameters[0].Type, keyType));
    }
}
