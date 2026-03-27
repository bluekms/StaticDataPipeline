namespace Sdp.Attributes;

// https://learn.microsoft.com/ko-kr/dotnet/standard/base-types/standard-timespan-format-strings
[AttributeUsage(AttributeTargets.Parameter)]
public class TimeSpanFormatAttribute(string format)
    : Attribute
{
    public string FormatString { get; } = format;
}
