using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class ParameterAnalyzer
{
    private const string DefaultSingleColumnSeparator = ",";

    internal static ImmutableArray<ParameterAnalysis> CollectParameters(
        IMethodSymbol ctor,
        CancellationToken cancellationToken)
    {
        var visiting = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        return AnalyzeParameters(ctor, visiting, cancellationToken);
    }

    private static ImmutableArray<ParameterAnalysis> AnalyzeParameters(
        IMethodSymbol ctor,
        HashSet<INamedTypeSymbol> visiting,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<ParameterAnalysis>(ctor.Parameters.Length);

        foreach (var param in ctor.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Add(AnalyzeParameter(param, visiting, cancellationToken));
        }

        return builder.MoveToImmutable();
    }

    private static ParameterAnalysis AnalyzeParameter(
        IParameterSymbol param,
        HashSet<INamedTypeSymbol> visiting,
        CancellationToken cancellationToken)
    {
        var columnName = param.Name;
        var isKey = false;
        string? nullString = null;
        string? dateTimeFormat = null;
        string? timeSpanFormat = null;
        RangeInfo? range = null;
        string? regexPattern = null;
        int? length = null;
        string? singleColumnSeparator = null;
        int? minCount = null;
        int? maxCount = null;
        var hasUnsupportedAttribute = false;
        var hasCountRangeAttribute = false;
        var hasLengthAttribute = false;
        var hasSingleColumnCollectionAttribute = false;
        var isIgnored = false;

        foreach (var attr in param.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
            {
                continue;
            }

            var namespaceName = attrClass.ContainingNamespace?.ToDisplayString();
            if (namespaceName != SdpAttributeNames.Namespace)
            {
                continue;
            }

            switch (attrClass.Name)
            {
                case SdpAttributeNames.ColumnName:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string name)
                    {
                        columnName = name;
                    }

                    break;
                }

                case SdpAttributeNames.Key:
                {
                    isKey = true;
                    break;
                }

                case SdpAttributeNames.NullString:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string nullStringValue)
                    {
                        nullString = nullStringValue;
                    }

                    break;
                }

                case SdpAttributeNames.DateTimeFormat:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string format)
                    {
                        dateTimeFormat = format;
                    }

                    break;
                }

                case SdpAttributeNames.TimeSpanFormat:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string format)
                    {
                        timeSpanFormat = format;
                    }

                    break;
                }

                case SdpAttributeNames.Range:
                {
                    range = ExtractRangeInfo(attr);
                    break;
                }

                case SdpAttributeNames.RegularExpression:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string pattern)
                    {
                        regexPattern = pattern;
                    }

                    break;
                }

                case SdpAttributeNames.Length:
                {
                    hasLengthAttribute = true;
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is int lengthValue)
                    {
                        length = lengthValue;
                    }

                    break;
                }

                case SdpAttributeNames.SingleColumnCollection:
                {
                    hasSingleColumnCollectionAttribute = true;
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string separator)
                    {
                        singleColumnSeparator = separator;
                    }
                    else
                    {
                        singleColumnSeparator = DefaultSingleColumnSeparator;
                    }

                    break;
                }

                case SdpAttributeNames.CountRange:
                {
                    hasCountRangeAttribute = true;
                    if (attr.ConstructorArguments.Length >= 2 &&
                        attr.ConstructorArguments[0].Value is int min &&
                        attr.ConstructorArguments[1].Value is int max)
                    {
                        minCount = min;
                        maxCount = max;
                    }

                    break;
                }

                case SdpAttributeNames.ForeignKey:
                case SdpAttributeNames.SwitchForeignKey:
                {
                    break;
                }

                case SdpAttributeNames.Ignore:
                {
                    isIgnored = true;
                    break;
                }

                default:
                {
                    hasUnsupportedAttribute = true;
                    break;
                }
            }
        }

        if (isIgnored)
        {
            return ParameterAnalysis.Ignored(param.Name, columnName, param.Type);
        }

        var kind = TypeClassifier.ClassifyScalar(param.Type);
        var isNullable = TypeClassifier.IsNullable(param.Type);

        CollectionInfo? collection = null;
        var classified = TypeClassifier.ClassifyCollection(param.Type);
        if (classified is not null)
        {
            var collectionKind = classified.Kind;
            if (singleColumnSeparator is not null)
            {
                collectionKind = collectionKind switch
                {
                    CollectionKind.ImmutableArray => CollectionKind.SingleColumnImmutableArray,
                    CollectionKind.FrozenSet => CollectionKind.SingleColumnFrozenSet,
                    _ => collectionKind,
                };
            }

            var isMultiColumnArrayOrSet = collectionKind is CollectionKind.ImmutableArray or CollectionKind.FrozenSet;
            var elementIsNonScalar = classified.ElementKind is ScalarKind.Unsupported;

            NestedRecordInfo? valueNested = null;
            NestedRecordInfo? elementNested = null;
            if (collectionKind == CollectionKind.FrozenDictionary)
            {
                valueNested = AnalyzeNestedRecord(classified.ElementType, visiting, cancellationToken);
            }
            else if (isMultiColumnArrayOrSet && elementIsNonScalar)
            {
                elementNested = AnalyzeNestedRecord(classified.ElementType, visiting, cancellationToken);
            }

            collection = classified with
            {
                Kind = collectionKind,
                Length = length ?? 0,
                Separator = singleColumnSeparator,
                MinCount = minCount,
                MaxCount = maxCount,
                ValueNested = valueNested,
                ElementNested = elementNested,
            };
        }

        NestedRecordInfo? nested = null;
        if (kind == ScalarKind.Unsupported && collection is null)
        {
            nested = AnalyzeNestedRecord(param.Type, visiting, cancellationToken);
        }

        return new ParameterAnalysis(
            param.Name,
            columnName,
            param.Type,
            kind,
            isNullable,
            isKey,
            nullString,
            dateTimeFormat,
            timeSpanFormat,
            range,
            regexPattern,
            nested,
            collection,
            hasUnsupportedAttribute,
            hasCountRangeAttribute,
            hasLengthAttribute,
            hasSingleColumnCollectionAttribute,
            isIgnored);
    }

    private static NestedRecordInfo? AnalyzeNestedRecord(
        ITypeSymbol type,
        HashSet<INamedTypeSymbol> visiting,
        CancellationToken cancellationToken)
    {
        var underlying = TypeClassifier.UnwrapNullable(type);

        if (underlying is not INamedTypeSymbol named)
        {
            return null;
        }

        if (!named.IsRecord)
        {
            return null;
        }

        if (!visiting.Add(named))
        {
            return null;
        }

        var primaryCtor = SinglePrimaryConstructorResolver.Resolve(named);
        if (primaryCtor is null)
        {
            visiting.Remove(named);
            return null;
        }

        var parameters = AnalyzeParameters(primaryCtor, visiting, cancellationToken);

        visiting.Remove(named);
        return new NestedRecordInfo(named, parameters);
    }

    private static RangeInfo? ExtractRangeInfo(AttributeData attr)
    {
        var arguments = attr.ConstructorArguments;
        if (arguments.Length == 2)
        {
            return new RangeInfo(arguments[0].Value, arguments[1].Value, RangeArgKind.Numeric);
        }

        if (arguments.Length == 3)
        {
            return new RangeInfo(arguments[1].Value, arguments[2].Value, RangeArgKind.Typed);
        }

        return null;
    }
}
