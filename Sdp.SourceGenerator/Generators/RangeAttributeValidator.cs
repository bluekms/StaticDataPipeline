using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sdp.SourceGenerator.Generators;

internal sealed class RangeAttributeValidator(
    INamedTypeSymbol symbol,
    RecordDeclarationSyntax syntax,
    List<QualifiedParameter> allParameters,
    List<Diagnostic> diagnostics)
{
    public HashSet<string> ValidateRedundantTypedRange()
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.Range is not { ArgKind: RangeArgKind.Typed } range)
            {
                continue;
            }

            var scalarKind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
            if (IsNumericRangeLiteralExpressible(scalarKind))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RedundantTypedRange,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    range.Minimum,
                    range.Maximum));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }

    public HashSet<string> ValidateNumericRangeBounds()
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.Range is not { ArgKind: RangeArgKind.Numeric } range)
            {
                continue;
            }

            var scalarKind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
            if (RangeBoundsExceedType(scalarKind, range.Minimum, range.Maximum))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RangeBoundOutOfTypeRange,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }

    // ex) [Range(typeof(long), "0", "99999999999999999999")]
    public HashSet<string> ValidateTypedRangeBounds()
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;
            if (param.Range is not { ArgKind: RangeArgKind.Typed } range)
            {
                continue;
            }

            var scalarKind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
            var scalarType = param.IsCollection ? param.Collection!.ElementType : param.Type;
            if (TypedBoundOutOfRange(param, scalarKind, scalarType, range.Minimum as string, range.Maximum as string))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RangeBoundOutOfTypeRange,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path));
                rejected.Add(qualified.Path);
            }
        }

        return rejected;
    }

    public HashSet<string> ValidateRangeOrdering()
    {
        var rejected = new HashSet<string>();

        foreach (var qualified in allParameters)
        {
            var param = qualified.Parameter;

            if (param.Range is { ArgKind: RangeArgKind.Numeric } range
                && TryConvertToDouble(range.Minimum, out var minimum)
                && TryConvertToDouble(range.Maximum, out var maximum)
                && minimum > maximum)
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RangeMinimumExceedsMaximum,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    range.Minimum,
                    range.Maximum));
                rejected.Add(qualified.Path);
            }

            if (param.Range is { ArgKind: RangeArgKind.Typed } typedRange
                && TypedRangeOrderInverted(param, typedRange))
            {
                diagnostics.Add(Diagnostic.Create(
                    SdpDiagnostics.RangeMinimumExceedsMaximum,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    qualified.Path,
                    typedRange.Minimum,
                    typedRange.Maximum));
                rejected.Add(qualified.Path);
            }

            if (param.Collection is { MinCount: { } minCount, MaxCount: { } maxCount })
            {
                if (minCount < 0 || maxCount < 0)
                {
                    diagnostics.Add(Diagnostic.Create(
                        SdpDiagnostics.CountRangeMustBeNonNegative,
                        syntax.Identifier.GetLocation(),
                        symbol.ToDisplayString(),
                        qualified.Path,
                        minCount,
                        maxCount));
                    rejected.Add(qualified.Path);
                }
                else if (minCount > maxCount)
                {
                    diagnostics.Add(Diagnostic.Create(
                        SdpDiagnostics.RangeMinimumExceedsMaximum,
                        syntax.Identifier.GetLocation(),
                        symbol.ToDisplayString(),
                        qualified.Path,
                        minCount,
                        maxCount));
                    rejected.Add(qualified.Path);
                }
            }
        }

        return rejected;
    }

    // long, Decimal은 정말도 문제로 typed 형태가 필요
    private static bool IsNumericRangeLiteralExpressible(ScalarKind kind)
    {
        return kind
            is ScalarKind.Byte
            or ScalarKind.SByte
            or ScalarKind.Int16
            or ScalarKind.UInt16
            or ScalarKind.Int32
            or ScalarKind.UInt32
            or ScalarKind.Single
            or ScalarKind.Double;
    }

    private static bool TypedBoundOutOfRange(
        ParameterAnalysis param,
        ScalarKind kind,
        ITypeSymbol type,
        string? minimum,
        string? maximum)
    {
        if (minimum is null || maximum is null)
        {
            return true;
        }

        if (kind == ScalarKind.Enum)
        {
            var enumType = (INamedTypeSymbol)TypeClassifier.UnwrapNullable(type);
            return !IsValidEnumBound(enumType, minimum) || !IsValidEnumBound(enumType, maximum);
        }

        return kind switch
        {
            ScalarKind.Int64 => !ParsesAsInt64(minimum) || !ParsesAsInt64(maximum),
            ScalarKind.UInt64 => !ParsesAsUInt64(minimum) || !ParsesAsUInt64(maximum),
            ScalarKind.Decimal => !ParsesAsDecimal(minimum) || !ParsesAsDecimal(maximum),
            ScalarKind.DateTime => param.DateTimeFormat is { } dateTimeFormat
                && (!ParsesAsDateTime(minimum, dateTimeFormat) || !ParsesAsDateTime(maximum, dateTimeFormat)),
            ScalarKind.DateTimeOffset => param.DateTimeFormat is { } dateTimeOffsetFormat
                && (!ParsesAsDateTimeOffset(minimum, dateTimeOffsetFormat)
                    || !ParsesAsDateTimeOffset(maximum, dateTimeOffsetFormat)),
            ScalarKind.TimeSpan => param.TimeSpanFormat is { } timeSpanFormat
                && (!ParsesAsTimeSpan(minimum, timeSpanFormat) || !ParsesAsTimeSpan(maximum, timeSpanFormat)),
            _ => false,
        };
    }

    private static bool IsValidEnumBound(INamedTypeSymbol enumType, string text)
    {
        var isMember = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(field => field.IsConst && field.Name == text);
        if (isMember)
        {
            return true;
        }

        var isUnsigned64 = enumType.EnumUnderlyingType!.SpecialType == SpecialType.System_UInt64;
        if (isUnsigned64)
        {
            return !text.StartsWith("-", StringComparison.Ordinal)
                && ulong.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
        }

        return long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
    }

    private static bool ParsesAsInt64(string text)
    {
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static bool ParsesAsUInt64(string text)
    {
        return ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static bool ParsesAsDecimal(string text)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
    }

    private static bool ParsesAsDateTime(string text, string format)
    {
        return DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool ParsesAsDateTimeOffset(string text, string format)
    {
        return DateTimeOffset.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool ParsesAsTimeSpan(string text, string format)
    {
        return TimeSpan.TryParseExact(text, format, CultureInfo.InvariantCulture, out _);
    }

    private static bool RangeBoundsExceedType(ScalarKind kind, object? minimum, object? maximum)
    {
        if (!TryGetNumericBounds(kind, out var typeMin, out var typeMax))
        {
            return false;
        }

        return BoundExceeds(minimum, typeMin, typeMax) || BoundExceeds(maximum, typeMin, typeMax);
    }

    private static bool BoundExceeds(object? value, double typeMin, double typeMax)
    {
        if (!TryConvertToDouble(value, out var bound))
        {
            return false;
        }

        if (double.IsNaN(bound) || double.IsInfinity(bound))
        {
            return true;
        }

        return bound < typeMin || bound > typeMax;
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        switch (value)
        {
            case sbyte v: result = v; return true;
            case byte v: result = v; return true;
            case short v: result = v; return true;
            case ushort v: result = v; return true;
            case int v: result = v; return true;
            case uint v: result = v; return true;
            case long v: result = v; return true;
            case ulong v: result = v; return true;
            case float v: result = v; return true;
            case double v: result = v; return true;
            default: result = 0; return false;
        }
    }

    private static bool TryGetNumericBounds(ScalarKind kind, out double min, out double max)
    {
        switch (kind)
        {
            case ScalarKind.SByte: min = sbyte.MinValue; max = sbyte.MaxValue; return true;
            case ScalarKind.Byte: min = byte.MinValue; max = byte.MaxValue; return true;
            case ScalarKind.Int16: min = short.MinValue; max = short.MaxValue; return true;
            case ScalarKind.UInt16: min = ushort.MinValue; max = ushort.MaxValue; return true;
            case ScalarKind.Int32: min = int.MinValue; max = int.MaxValue; return true;
            case ScalarKind.UInt32: min = uint.MinValue; max = uint.MaxValue; return true;
            case ScalarKind.Int64: min = long.MinValue; max = long.MaxValue; return true;
            case ScalarKind.UInt64: min = ulong.MinValue; max = ulong.MaxValue; return true;
            case ScalarKind.Single: min = float.MinValue; max = float.MaxValue; return true;
            case ScalarKind.Double: min = double.MinValue; max = double.MaxValue; return true;
            case ScalarKind.Decimal: min = (double)decimal.MinValue; max = (double)decimal.MaxValue; return true;
            default: min = 0; max = 0; return false;
        }
    }

    private static bool TypedRangeOrderInverted(ParameterAnalysis param, RangeInfo range)
    {
        if (range.Minimum is not string minText || range.Maximum is not string maxText)
        {
            return false;
        }

        var kind = param.IsCollection ? param.Collection!.ElementKind : param.Kind;
        switch (kind)
        {
            case ScalarKind.Int64:
                return long.TryParse(minText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longMin)
                    && long.TryParse(maxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longMax)
                    && longMin > longMax;
            case ScalarKind.UInt64:
                return ulong.TryParse(minText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongMin)
                    && ulong.TryParse(maxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongMax)
                    && ulongMin > ulongMax;
            case ScalarKind.Decimal:
                return decimal.TryParse(minText, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalMin)
                    && decimal.TryParse(maxText, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalMax)
                    && decimalMin > decimalMax;
            case ScalarKind.DateTime:
                return param.DateTimeFormat is { } dateTimeFormat
                    && DateTime.TryParseExact(minText, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeMin)
                    && DateTime.TryParseExact(maxText, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeMax)
                    && dateTimeMin > dateTimeMax;
            case ScalarKind.DateTimeOffset:
                return param.DateTimeFormat is { } dateTimeOffsetFormat
                    && DateTimeOffset.TryParseExact(minText, dateTimeOffsetFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetMin)
                    && DateTimeOffset.TryParseExact(maxText, dateTimeOffsetFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetMax)
                    && dateTimeOffsetMin > dateTimeOffsetMax;
            case ScalarKind.TimeSpan:
                return param.TimeSpanFormat is { } timeSpanFormat
                    && TimeSpan.TryParseExact(minText, timeSpanFormat, CultureInfo.InvariantCulture, out var timeSpanMin)
                    && TimeSpan.TryParseExact(maxText, timeSpanFormat, CultureInfo.InvariantCulture, out var timeSpanMax)
                    && timeSpanMin > timeSpanMax;
            default:
                return false;
        }
    }
}
