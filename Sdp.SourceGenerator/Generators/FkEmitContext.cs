using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sdp.SourceGenerator.Generators;

// DisplayBody 는 오류 메시지에 찍는 인스턴스 경로의 보간 문자열 본문이고(MonsterRecord[Id={__r.Id}].Drops[{__i1}]),
// StaticPath 는 인스턴스와 무관한 선언 경로다(MonsterRecord.Drops).
internal sealed record FkEmitContext(
    string InstanceVariable,
    string DisplayBody,
    bool DisplayHasInterpolation,
    string StaticPath,
    int Depth)
{
    public static FkEmitContext Root(INamedTypeSymbol recordSymbol)
    {
        var keyClause = BuildKeyClause(recordSymbol, "__r");

        return new FkEmitContext(
            "__r",
            recordSymbol.Name + keyClause,
            keyClause.Length > 0,
            recordSymbol.Name,
            Depth: 0);
    }

    public static string BuildKeyClause(INamedTypeSymbol record, string instanceVariable)
    {
        var keyParameter = KeyParameterResolver.Find(record);
        if (keyParameter is null)
        {
            return string.Empty;
        }

        return "[" + keyParameter.Name + "=" + KeyParameterResolver.BuildValueDisplay(keyParameter, instanceVariable) + "]";
    }

    public FkEmitContext Enter(string propertyName, string instanceVariable)
    {
        return new FkEmitContext(
            instanceVariable,
            DisplayBody + "." + propertyName,
            DisplayHasInterpolation,
            StaticPath + "." + propertyName,
            Depth + 1);
    }

    public FkEmitContext WithElement(string elementClause)
    {
        return this with
        {
            DisplayBody = DisplayBody + elementClause,
            DisplayHasInterpolation = DisplayHasInterpolation || elementClause.Length > 0,
        };
    }

    public string BuildDisplayExpression()
    {
        return DisplayHasInterpolation
            ? "global::System.FormattableString.Invariant($\"" + DisplayBody + "\")"
            : SymbolDisplay.FormatLiteral(DisplayBody, quote: true);
    }
}
