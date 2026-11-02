using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner.Collectors;

public static class EnumMemberValueResolver
{
    public static long Resolve(INamedTypeSymbol enumType, string memberName)
    {
        var members = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(x => x.IsConst)
            .ToList();

        long? nextValue = 0;
        IFieldSymbol? nonLiteralMember = null;
        foreach (var member in members)
        {
            var initializer = FindInitializer(member);
            var value = ResolveMemberValue(member, initializer, nextValue);
            if (value is null && initializer is not null)
            {
                nonLiteralMember = member;
            }

            var isTarget = string.Equals(member.Name, memberName, StringComparison.Ordinal);
            if (value is null)
            {
                if (isTarget)
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.EnumMemberValueNotLiteral,
                        nonLiteralMember?.Name ?? member.Name,
                        enumType.Name));
                }

                nextValue = null;
                continue;
            }

            if (isTarget)
            {
                return value.Value;
            }

            nextValue = value + 1;
        }

        throw new InvalidOperationException(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.EnumMemberNotFound,
            memberName,
            enumType.Name));
    }

    private static ExpressionSyntax? FindInitializer(IFieldSymbol member)
    {
        var declaration = member.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<EnumMemberDeclarationSyntax>()
            .FirstOrDefault();

        return declaration?.EqualsValue?.Value;
    }

    private static long? ResolveMemberValue(IFieldSymbol member, ExpressionSyntax? initializer, long? nextValue)
    {
        if (member.HasConstantValue)
        {
            return ToInt64(member.ConstantValue);
        }

        if (initializer is null)
        {
            return nextValue;
        }

        return FoldLiteral(initializer);
    }

    private static long? FoldLiteral(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return FoldLiteral(parenthesized.Expression);
            case LiteralExpressionSyntax literal:
                return ToInt64(literal.Token.Value);
            case PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax operand } unary:
            {
                var value = ToInt64(operand.Token.Value);
                if (value is null)
                {
                    return null;
                }

                if (unary.IsKind(SyntaxKind.UnaryPlusExpression))
                {
                    return value;
                }

                if (unary.IsKind(SyntaxKind.UnaryMinusExpression))
                {
                    return -value;
                }

                return null;
            }

            default:
                return null;
        }
    }

    private static long? ToInt64(object? value)
    {
        return value switch
        {
            sbyte signedByteValue => signedByteValue,
            byte byteValue => byteValue,
            short shortValue => shortValue,
            ushort unsignedShortValue => unsignedShortValue,
            int intValue => intValue,
            uint unsignedIntValue => unsignedIntValue,
            long longValue => longValue,
            ulong unsignedLongValue when unsignedLongValue <= long.MaxValue => (long)unsignedLongValue,
            _ => null,
        };
    }
}
