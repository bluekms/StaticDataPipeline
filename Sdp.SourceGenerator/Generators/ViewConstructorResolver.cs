using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

// SDP0304 진단(StaticDataViewGenerator)과 ViewSetGenerator 의 deferred-error 경로 선택이
// 같은 판정을 쓰도록 단일 구현으로 공유한다. 한쪽만 바뀌면 진단과 생성 코드가 어긋난다.
internal static class ViewConstructorResolver
{
    public static bool HasSingleTableSetConstructor(INamedTypeSymbol viewSymbol, INamedTypeSymbol tableSetType)
    {
        var declaredConstructors = viewSymbol.InstanceConstructors
            .Where(ctor => !ctor.IsImplicitlyDeclared)
            .ToList();

        return declaredConstructors.Count == 1
            && declaredConstructors[0].Parameters.Length == 1
            && SymbolEqualityComparer.Default.Equals(declaredConstructors[0].Parameters[0].Type, tableSetType);
    }
}
