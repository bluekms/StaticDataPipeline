namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class ForeignKeyAttribute(
    string tableSetName,
    string recordColumnName)
    : Attribute
{
    public string TableSetName { get; } = tableSetName;
    public string RecordColumnName { get; } = recordColumnName;
}
