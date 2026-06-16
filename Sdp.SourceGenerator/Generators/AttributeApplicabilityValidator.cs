using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal sealed class AttributeApplicabilityValidator(
    INamedTypeSymbol symbol,
    RecordDeclarationSyntax syntax,
    List<QualifiedParameter> allParameters,
    List<Diagnostic> diagnostics)
{
    public HashSet<string> Validate()
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;

            if (param.HasSingleColumnCollectionAttribute && !IsSingleColumnCollectionApplicable(param))
            {
                ReportAttributeNotApplicable(qualified, nameof(SdpAttributeNames.SingleColumnCollection), rejected);
            }

            if (param.HasLengthAttribute && !IsLengthApplicable(param))
            {
                ReportAttributeNotApplicable(qualified, nameof(SdpAttributeNames.Length), rejected);
            }

            if (param.NullString is not null && !IsNullStringApplicable(param))
            {
                ReportAttributeNotApplicable(qualified, nameof(SdpAttributeNames.NullString), rejected);
            }

            if (param.DateTimeFormat is not null && !IsDateTimeFormatApplicable(param))
            {
                ReportAttributeNotApplicable(qualified, nameof(SdpAttributeNames.DateTimeFormat), rejected);
            }

            if (param.TimeSpanFormat is not null && !IsTimeSpanFormatApplicable(param))
            {
                ReportAttributeNotApplicable(qualified, nameof(SdpAttributeNames.TimeSpanFormat), rejected);
            }

            if (param.RegexPattern is not null && !IsRegularExpressionApplicable(param))
            {
                ReportAttributeNotApplicable(qualified, nameof(SdpAttributeNames.RegularExpression), rejected);
            }

            // Numeric이 아닌 형태([Range(typeof(T), ...)]) 는
            // ParameterEmittability 에서 적용 가능한지 확인하고,
            // RangeAttributeValidator 에서 경계·순서 검증을 한다
            if (param.Range is { ArgKind: RangeArgKind.Numeric } && !IsNumericRangeApplicable(param))
            {
                ReportAttributeNotApplicable(qualified, nameof(SdpAttributeNames.Range), rejected);
            }
        }

        return rejected;
    }

    private void ReportAttributeNotApplicable(
        QualifiedParameter qualified,
        string attributeName,
        HashSet<string> rejected)
    {
        diagnostics.Add(Diagnostic.Create(
            SdpDiagnostics.AttributeNotApplicable,
            syntax.Identifier.GetLocation(),
            symbol.ToDisplayString(),
            qualified.Path,
            attributeName));
        rejected.Add(qualified.Path);
    }

    private static bool IsSingleColumnCollectionApplicable(ParameterAnalysis param)
    {
        // 컬렉션이 아니라면 [SingleColumnCollection] 적용 불가
        return param.Collection is not null;
    }

    private static bool IsLengthApplicable(ParameterAnalysis param)
    {
        if (param.HasSingleColumnCollectionAttribute)
        {
            // SDP0013 에서 보고되므로 SDP0016 은 넘어간다
            return true;
        }

        return param.Collection?.Kind
            is CollectionKind.ImmutableArray
            or CollectionKind.FrozenSet
            or CollectionKind.FrozenDictionary;
    }

    private static bool IsNullStringApplicable(ParameterAnalysis param)
    {
        if (param.Collection is { } collection)
        {
            if (collection.Kind == CollectionKind.FrozenDictionary)
            {
                return false;
            }

            return TypeClassifier.IsNullable(collection.ElementType);
        }

        if (param.IsRecord)
        {
            return false;
        }

        return param.IsNullable;
    }

    private static bool IsDateTimeFormatApplicable(ParameterAnalysis param)
    {
        if (param.Collection is { } collection)
        {
            if (collection.Kind == CollectionKind.FrozenDictionary)
            {
                return true;
            }

            return IsDateTimeKind(collection.ElementKind);
        }

        return IsDateTimeKind(param.Kind);
    }

    private static bool IsTimeSpanFormatApplicable(ParameterAnalysis param)
    {
        if (param.Collection is { } collection)
        {
            if (collection.Kind == CollectionKind.FrozenDictionary)
            {
                return true;
            }

            return collection.ElementKind == ScalarKind.TimeSpan;
        }

        return param.Kind == ScalarKind.TimeSpan;
    }

    private static bool IsRegularExpressionApplicable(ParameterAnalysis param)
    {
        if (param.IsRecord)
        {
            return false;
        }

        if (param.Collection is { } collection)
        {
            return collection.ElementKind == ScalarKind.String;
        }

        return param.Kind == ScalarKind.String;
    }

    private static bool IsNumericRangeApplicable(ParameterAnalysis param)
    {
        if (param.IsRecord)
        {
            return false;
        }

        if (param.Collection is { } collection)
        {
            return TypeClassifier.IsNumeric(collection.ElementKind);
        }

        return TypeClassifier.IsNumeric(param.Kind);
    }

    private static bool IsDateTimeKind(ScalarKind kind)
    {
        return kind
            is ScalarKind.DateTime
            or ScalarKind.DateTimeOffset
            or ScalarKind.DateOnly
            or ScalarKind.TimeOnly;
    }
}
