using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator.Generators;

internal static class KeyParameterResolver
{
    public static IParameterSymbol? Find(INamedTypeSymbol record)
    {
        var primaryConstructor = SymbolResolver.FindPrimaryConstructor(record);
        if (primaryConstructor is null)
        {
            return null;
        }

        foreach (var param in primaryConstructor.Parameters)
        {
            foreach (var attr in param.GetAttributes())
            {
                if (attr.AttributeClass?.Name == SdpAttributeNames.Key && SdpAttributeNames.IsSdpAttribute(attr))
                {
                    return param;
                }
            }
        }

        return null;
    }

    // 브랜딩 record 는 ToString() 이 "ItemId { Value = 3 }" 이라 안쪽 값만 찍는다.
    public static string BuildValueDisplay(IParameterSymbol keyParameter, string instanceVariable)
    {
        var access = instanceVariable + "." + EmitHelper.EscapeIdentifier(keyParameter.Name);
        var valueDisplayMember = FkTargetResolver.ResolveValueDisplayMember(keyParameter.Type);
        if (valueDisplayMember is not null)
        {
            access += "." + EmitHelper.EscapeIdentifier(valueDisplayMember);
        }

        return "{" + access + "}";
    }
}
