namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class NullStringAttribute(string nullString) : Attribute
{
    public string NullString { get; } = nullString;
}
