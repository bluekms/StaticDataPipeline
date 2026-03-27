namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class ColumnNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    public override bool Match(object? obj)
    {
        return obj is not (List<object> or HashSet<object> or Dictionary<object, object>);
    }
}
