using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SchemaInfoScanner.Schemata;

namespace SchemaInfoScanner.Collectors;

public static class AttributeInfoReader
{
    public static IReadOnlyList<AttributeInfo> Read(SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
    {
        var attributes = new List<AttributeInfo>();
        foreach (var attribute in attributeLists.SelectMany(x => x.Attributes))
        {
            attributes.Add(Read(attribute, semanticModel));
        }

        return attributes;
    }

    public static AttributeInfo Read(AttributeSyntax attribute, SemanticModel semanticModel)
    {
        var arguments = new List<AttributeArgumentInfo>();
        if (attribute.ArgumentList is not null)
        {
            foreach (var argument in attribute.ArgumentList.Arguments)
            {
                arguments.Add(ReadArgument(argument, semanticModel));
            }
        }

        return new AttributeInfo(NormalizeName(attribute.Name), arguments);
    }

    private static string NormalizeName(NameSyntax name)
    {
        var identifier = name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => name.ToString(),
        };

        const string suffix = "Attribute";
        if (identifier.Length > suffix.Length && identifier.EndsWith(suffix, StringComparison.Ordinal))
        {
            return identifier[..^suffix.Length];
        }

        return identifier;
    }

    private static AttributeArgumentInfo ReadArgument(AttributeArgumentSyntax argument, SemanticModel semanticModel)
    {
        var parameterName = argument.NameColon?.Name.Identifier.ValueText ?? argument.NameEquals?.Name.Identifier.ValueText;
        var expression = argument.Expression;

        if (expression is TypeOfExpressionSyntax typeOf)
        {
            return new AttributeArgumentInfo(typeOf.Type.ToString(), null, false, parameterName);
        }

        var model = semanticModel.SyntaxTree == expression.SyntaxTree
            ? semanticModel
            : semanticModel.Compilation.GetSemanticModel(expression.SyntaxTree);

        var constant = model.GetConstantValue(expression);
        if (!constant.HasValue)
        {
            constant = FoldLiteral(expression, model);
        }

        if (!constant.HasValue)
        {
            return new AttributeArgumentInfo(expression.ToString(), null, false, parameterName);
        }

        var isEnumMember = expression is MemberAccessExpressionSyntax
            && model.GetTypeInfo(expression).Type is { TypeKind: TypeKind.Enum };
        var text = isEnumMember && expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.Identifier.ValueText
            : FormatConstant(constant.Value);

        return new AttributeArgumentInfo(text, constant.Value, true, parameterName);
    }

    private static Optional<object?> FoldLiteral(ExpressionSyntax expression, SemanticModel model)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return FoldLiteral(parenthesized.Expression, model);
            case MemberAccessExpressionSyntax or IdentifierNameSyntax:
                return FoldConstField(expression, model);
            case LiteralExpressionSyntax literal:
                return literal.Token.Value;
            case PrefixUnaryExpressionSyntax unary when unary.Operand is LiteralExpressionSyntax operand:
            {
                var value = operand.Token.Value;
                if (unary.IsKind(SyntaxKind.UnaryPlusExpression))
                {
                    return value;
                }

                if (!unary.IsKind(SyntaxKind.UnaryMinusExpression))
                {
                    return default;
                }

                return value switch
                {
                    int i => -i,
                    long l => -l,
                    double d => -d,
                    float f => -f,
                    decimal m => -m,
                    _ => default(Optional<object?>),
                };
            }

            default:
                return default;
        }
    }

    private static Optional<object?> FoldConstField(ExpressionSyntax expression, SemanticModel model)
    {
        var symbol = model.GetSymbolInfo(expression).Symbol;
        if (symbol is not IFieldSymbol { IsConst: true } field)
        {
            return default;
        }

        if (field.HasConstantValue)
        {
            return field.ConstantValue;
        }

        var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntax is VariableDeclaratorSyntax { Initializer: { } initializer })
        {
            return FoldLiteral(initializer.Value, model);
        }

        return default;
    }

    private static string FormatConstant(object? value)
    {
        return value switch
        {
            null => "null",
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
