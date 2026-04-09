using System.Globalization;
using FluentValidation;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.TypeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.SchemaValidators;

internal partial class SchemaRuleValidator
{
    private void RegisterNullStringAttributeRule()
    {
        // nullable 타입이면 반드시 있어야 한다.
        When(x => x.IsNullable(), () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<NullStringAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NullStringAttributeRequiredForNullable,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });

        // nullable 타입이 사용된 array 이면 반드시 있어야 한다.
        When(IsNullablePrimitiveArray, () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<NullStringAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NullStringAttributeRequiredForNullableArray,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });

        // nullable 타입이 사용된 set 이라면 반드시 있어야 한다.
        When(IsNullablePrimitiveSet, () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<NullStringAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NullStringAttributeRequiredForNullableSet,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });

        // nullable 타입이 사용된 map 이라면 반드시 있어야 한다.
        When(IsNullablePrimitiveMapValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.HasAttribute<NullStringAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NullStringAttributeRequiredForNullableMap,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });

        When(IsDisallowType, () =>
        {
            RuleFor(x => x)
                .Must(x => !x.HasAttribute<NullStringAttribute>())
                .WithMessage(x =>
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Messages.Composite.NullStringAttributeNotAllowed,
                        x.PropertyName.FullName,
                        x.GetType().FullName));
        });
    }

    private static bool IsNullablePrimitiveArray(PropertySchemaBase property)
    {
        if (!ArrayTypeChecker.IsPrimitiveArrayType(property.NamedTypeSymbol))
        {
            return false;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();
        return typeArgument.NullableAnnotation is NullableAnnotation.Annotated;
    }

    private static bool IsNullablePrimitiveSet(PropertySchemaBase property)
    {
        if (!SetTypeChecker.IsPrimitiveSetType(property.NamedTypeSymbol))
        {
            return false;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();
        return typeArgument.NullableAnnotation is NullableAnnotation.Annotated;
    }

    private static bool IsNullablePrimitiveMapValue(PropertySchemaBase property)
    {
        if (!MapTypeChecker.IsSupportedMapType(property.NamedTypeSymbol))
        {
            return false;
        }

        var valueSymbol = (INamedTypeSymbol)property.NamedTypeSymbol.TypeArguments[1];
        return valueSymbol.NullableAnnotation is NullableAnnotation.Annotated;
    }

    private static bool IsDisallowType(PropertySchemaBase property)
    {
        if (!CollectionTypeChecker.IsSupportedCollectionType(property.NamedTypeSymbol))
        {
            return !property.IsNullable();
        }

        if (MapTypeChecker.IsSupportedMapType(property.NamedTypeSymbol))
        {
            var valueSymbol = (INamedTypeSymbol)property.NamedTypeSymbol.TypeArguments[1];
            return valueSymbol.NullableAnnotation is not NullableAnnotation.Annotated;
        }

        var typeArgument = property.NamedTypeSymbol.TypeArguments.Single();
        return typeArgument.NullableAnnotation is not NullableAnnotation.Annotated;
    }
}
