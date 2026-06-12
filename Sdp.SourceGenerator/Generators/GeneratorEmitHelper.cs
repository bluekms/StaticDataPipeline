using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static class GeneratorEmitHelper
{
    // 진단에 의해 emit 이 불가능한 타입의 팩토리에 방출하는 예외 메시지 꼬리말.
    // 닫는 작은따옴표(')부터 시작하므로 호출부는 이름 리터럴까지만 Append 하고 이 상수를 잇는다.
    public const string CannotEmitDiagnosticSuffix =
        "'은 SDP 진단에 의해 빌드할 수 없습니다. 빌드 출력의 SDP 진단을 확인하세요.";

    public static string GetTypeKeyword(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return "interface";
        }

        if (type.IsRecord)
        {
            return type.IsValueType ? "record struct" : "record";
        }

        if (type.IsValueType)
        {
            return "struct";
        }

        return "class";
    }

    // C# 예약어와 충돌하는 심볼 이름(@default 등)은 verbatim 식별자로 escape 해 방출한다.
    // FullyQualifiedFormat 으로 방출하는 타입 이름은 Roslyn 이 escape 를 포함하므로,
    // symbol.Name 을 직접 잇는 멤버 접근·타입 선언·named argument 지점에서만 사용한다.
    public static string EscapeIdentifier(string name)
    {
        if (SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None)
        {
            return "@" + name;
        }

        return name;
    }

    // OpenNamespaceAndContainingTypes 가 연 namespace/중첩 체인의 상태.
    // CloseContainingTypes 에 그대로 넘겨 닫는다.
    public sealed record ContainingTypeScope(string Indent, int OpenedTypeCount, bool HasNamespace);

    // namespace 선언 + 중첩 outer 타입 partial 체인 열기. 모든 emitter 가 공유한다.
    // namespace 는 block 형태로 연다 — file-scoped namespace 는 C# 10 문법이라
    // 유니티(C# 9) 소비자 컴파일레이션에서 생성 코드가 컴파일되지 않는다.
    public static ContainingTypeScope OpenNamespaceAndContainingTypes(StringBuilder sb, INamedTypeSymbol type)
    {
        var namespaceName = type.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : type.ContainingNamespace.ToDisplayString();

        var indent = string.Empty;
        var hasNamespace = namespaceName.Length > 0;
        if (hasNamespace)
        {
            sb.Append("namespace ").AppendLine(namespaceName);
            sb.AppendLine("{");
            indent = "    ";
        }

        var outerChain = new List<INamedTypeSymbol>();
        for (var outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            outerChain.Add(outer);
        }

        outerChain.Reverse();

        foreach (var outer in outerChain)
        {
            sb.Append(indent).Append("partial ").Append(GetTypeKeyword(outer)).Append(' ').AppendLine(EscapeIdentifier(outer.Name));
            sb.Append(indent).AppendLine("{");
            indent += "    ";
        }

        return new ContainingTypeScope(indent, outerChain.Count, hasNamespace);
    }

    public static void CloseContainingTypes(StringBuilder sb, ContainingTypeScope scope)
    {
        var indent = scope.Indent;
        for (var i = 0; i < scope.OpenedTypeCount; i++)
        {
            indent = indent.Substring(0, indent.Length - 4);
            sb.Append(indent).AppendLine("}");
        }

        if (scope.HasNamespace)
        {
            sb.AppendLine("}");
        }
    }
}
