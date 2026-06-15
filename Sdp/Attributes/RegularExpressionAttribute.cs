namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class RegularExpressionAttribute(string pattern) : Attribute
{
    public string Pattern { get; } = pattern;
}
