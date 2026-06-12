using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal static class SinglePrimaryConstructorResolver
{
    public static IMethodSymbol? Resolve(INamedTypeSymbol type)
    {
        // record 는 primary constructor 가 최대 1개이므로 첫 매칭을 그대로 반환한다.
        return type.InstanceConstructors
            .Where(ctor => !ctor.IsImplicitlyDeclared)
            .FirstOrDefault(IsPrimaryConstructor);
    }

    // primary constructor 는 record 선언 자체의 ParameterList 로 선언되므로, 선언 구문이
    // ParameterList 를 가진 RecordDeclarationSyntax 인지로 판별한다. 일반(positional 이 아닌)
    // 생성자는 ConstructorDeclarationSyntax 로 선언되어 여기서 제외된다.
    // 이 resolver 의 호출자는 전부 record 대상이며, TypeDeclarationSyntax.ParameterList(클래스
    // primary ctor)는 Roslyn 4.6+ API 라 유니티 호환 기준(CodeAnalysis 4.3.1)에서 쓸 수 없다.
    private static bool IsPrimaryConstructor(IMethodSymbol constructor)
    {
        foreach (var reference in constructor.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is RecordDeclarationSyntax { ParameterList: not null })
            {
                return true;
            }
        }

        return false;
    }
}
