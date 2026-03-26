namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class RegularExpressionAttribute(string pattern)
    : System.ComponentModel.DataAnnotations.RegularExpressionAttribute(pattern)
{
}
