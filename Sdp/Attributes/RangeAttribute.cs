namespace Sdp.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class RangeAttribute : Attribute
{
    public RangeAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public RangeAttribute(int minimum, int maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public RangeAttribute(Type type, string minimum, string maximum)
    {
        OperandType = type;
        Minimum = minimum;
        Maximum = maximum;
    }

    public object Minimum { get; }

    public object Maximum { get; }

    public Type? OperandType { get; }
}
