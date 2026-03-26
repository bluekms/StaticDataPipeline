namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class ForeignKeyAttribute(
    string dbSetName,
    string recordColumnName)
    : Attribute
{
    public string DbSetName { get; } = dbSetName;
    public string RecordColumnName { get; } = recordColumnName;
}
