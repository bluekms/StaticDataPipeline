using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

internal static class GeneratorEmitHelper
{
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

    public static string EscapeIdentifier(string name)
    {
        if (SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None)
        {
            return "@" + name;
        }

        return name;
    }

    public sealed record ContainingTypeScope(string Indent, int OpenedTypeCount, bool HasNamespace);

    // namespace는 유니티의 C# 9를 위해 block 형태로 연다
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
