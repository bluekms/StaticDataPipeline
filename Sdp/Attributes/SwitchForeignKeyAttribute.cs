namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public class SwitchForeignKeyAttribute(
    string conditionColumnName,
    string conditionValue,
    string tableSetName,
    string recordColumnName)
    : Attribute
{
    public string ConditionColumnName { get; } = conditionColumnName;
    public string ConditionValue { get; } = conditionValue;
    public string TableSetName { get; } = tableSetName;
    public string RecordColumnName { get; } = recordColumnName;
}
