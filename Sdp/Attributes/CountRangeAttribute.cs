namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class CountRangeAttribute(int minCount, int maxCount) : Attribute
{
    public int MinCount { get; } = minCount;
    public int MaxCount { get; } = maxCount;
}
