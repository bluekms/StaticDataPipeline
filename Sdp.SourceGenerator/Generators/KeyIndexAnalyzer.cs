using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal sealed record KeyIndexInfo(string KeyParameterName, string FullyQualifiedKeyTypeName);

internal static class KeyIndexAnalyzer
{
    private static readonly string[] LookupNames = ["Get", "TryGet"];

    public static KeyIndexInfo? Analyze(
        INamedTypeSymbol table,
        INamedTypeSymbol staticDataTableBase,
        INamedTypeSymbol record)
    {
        // 사용자 base 를 거쳐 상속한 Table 은 base 와 어느 쪽이 인덱스를 가질지 정할 수 없어 만들지 않는다.
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

        if (DeclaresLookup(table, keyParameter.Type))
        {
            return null;
        }

        return new KeyIndexInfo(
            keyParameter.Name,
            keyParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static bool HasNoKeyIndexAttribute(INamedTypeSymbol table)
    {
        return table.GetAttributes()
            .Where(static x => SdpAttributeNames.IsSdpAttribute(x))
            .Any(static x => x.AttributeClass!.Name == SdpAttributeNames.NoKeyIndex);
    }

    // Get 이나 TryGet 을 키 타입으로 이미 선언한 Table 은 자기 인덱스를 갖고 있다고 보고 아무것도 만들지 않는다.
    // 메서드가 아닌 같은 이름의 멤버가 있으면 생성한 메서드가 이름 충돌로 컴파일되지 않으므로 그때도 만들지 않는다.
    private static bool DeclaresLookup(INamedTypeSymbol table, ITypeSymbol keyType)
    {
        foreach (var member in LookupNames.SelectMany(name => table.GetMembers(name)))
        {
            if (member is not IMethodSymbol method)
            {
                return true;
            }

            if (method.Parameters.Length == 0)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, keyType))
            {
                return true;
            }
        }

        return false;
    }
}
