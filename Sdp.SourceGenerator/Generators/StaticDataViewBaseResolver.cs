using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

// ViewSet 멤버 판정(ViewSetGenerator)과 팩토리 방출 판정(StaticDataViewGenerator)이
// 같은 base 탐색을 쓰도록 단일 구현으로 공유한다. 한쪽만 바뀌면 두 판정이 어긋난다.
internal static class StaticDataViewBaseResolver
{
    private const string ViewNamespace = "Sdp.View";
    private const string ViewTypeName = "StaticDataView";

    public static INamedTypeSymbol? FindBase(INamedTypeSymbol symbol)
    {
        for (var type = symbol.BaseType; type is not null; type = type.BaseType)
        {
            if (type.ContainingNamespace?.ToDisplayString() == ViewNamespace && type.Name == ViewTypeName)
            {
                return type;
            }
        }

        return null;
    }
}
