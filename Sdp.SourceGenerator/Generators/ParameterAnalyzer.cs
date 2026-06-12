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
            if (namespaceName != AttributeNames.Namespace)
            {
                continue;
            }

            switch (attrClass.Name)
            {
                case AttributeNames.ColumnName:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string name)
                    {
                        columnName = name;
                    }

                    break;
                }

                case AttributeNames.Key:
                {
                    isKey = true;
                    break;
                }

                case AttributeNames.NullString:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string nullStringValue)
                    {
                        nullString = nullStringValue;
                    }

                    break;
                }

                case AttributeNames.DateTimeFormat:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string format)
                    {
                        dateTimeFormat = format;
                    }

                    break;
                }

                case AttributeNames.TimeSpanFormat:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string format)
                    {
                        timeSpanFormat = format;
                    }

                    break;
                }

                case AttributeNames.Range:
                {
                    range = ExtractRangeInfo(attr);
                    break;
                }

                case AttributeNames.RegularExpression:
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string pattern)
                    {
                        regexPattern = pattern;
                    }

                    break;
                }

                case AttributeNames.Length:
                {
                    hasLengthAttribute = true;
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is int lengthValue)
                    {
                        length = lengthValue;
                    }

                    break;
                }

                case AttributeNames.SingleColumnCollection:
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

                case AttributeNames.CountRange:
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

                case AttributeNames.ForeignKey:
                case AttributeNames.SwitchForeignKey:
                {
                    break;
                }

                case AttributeNames.Ignore:
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

        // [Ignore] 파라미터는 매핑 대상이 아니므로 타입 분석 없이 default 주입 대상으로만 표시한다.
        if (isIgnored)
        {
            return new ParameterAnalysis(
                param.Name,
                columnName,
                param.Type,
                ScalarKind.Unsupported,
                IsNullable: false,
                IsKey: false,
                NullString: null,
                DateTimeFormat: null,
                TimeSpanFormat: null,
                Range: null,
                RegexPattern: null,
                Nested: null,
                Collection: null,
                HasUnsupportedAttribute: false,
                HasCountRangeAttribute: false,
                HasLengthAttribute: false,
                HasSingleColumnCollectionAttribute: false,
                IsIgnored: true);
        }

        var kind = TypeClassifier.ClassifyScalar(param.Type);
        var isNullable = TypeClassifier.IsNullable(param.Type);

        // 컬렉션 타입이면 항상 분류한다. [Length] 가 있으면 고정 길이 다중 컬럼,
        // [SingleColumnCollection] 이면 단일 컬럼, 둘 다 없으면 다중 컬럼 동적 길이(Length 0)로 매핑한다.
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

            collection = classified with
            {
                Kind = collectionKind,
                Length = length ?? 0,
                Separator = singleColumnSeparator,
                MinCount = minCount,
                MaxCount = maxCount,
            };

            if (collectionKind == CollectionKind.FrozenDictionary)
            {
                var valueNested = AnalyzeNestedRecord(collection.ElementType, visiting, cancellationToken);
                collection = collection with { ValueNested = valueNested };
            }
            else if (collectionKind is CollectionKind.ImmutableArray or CollectionKind.FrozenSet && collection.ElementKind == ScalarKind.Unsupported)
            {
                // 다중 컬럼 컬렉션의 원소가 record 인 경우 nested 로 분석한다.
                var elementNested = AnalyzeNestedRecord(collection.ElementType, visiting, cancellationToken);
                collection = collection with { ElementNested = elementNested };
            }
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

        if (underlying is not INamedTypeSymbol named || !named.IsRecord)
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

    private static class AttributeNames
    {
        public const string Namespace = "Sdp.Attributes";
        public const string ColumnName = "ColumnNameAttribute";
        public const string Key = "KeyAttribute";
        public const string NullString = "NullStringAttribute";
        public const string DateTimeFormat = "DateTimeFormatAttribute";
        public const string TimeSpanFormat = "TimeSpanFormatAttribute";
        public const string Range = "RangeAttribute";
        public const string RegularExpression = "RegularExpressionAttribute";
        public const string Length = "LengthAttribute";
        public const string SingleColumnCollection = "SingleColumnCollectionAttribute";
        public const string CountRange = "CountRangeAttribute";
        public const string ForeignKey = "ForeignKeyAttribute";
        public const string SwitchForeignKey = "SwitchForeignKeyAttribute";
        public const string Ignore = "IgnoreAttribute";
    }
}
