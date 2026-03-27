namespace Sdp.Attributes;

// https://learn.microsoft.com/ko-kr/dotnet/standard/base-types/standard-date-and-time-format-strings?form=MG0AV3
[AttributeUsage(AttributeTargets.Parameter)]
public class DateTimeFormatAttribute(string format)
    : Attribute
{
    public string FormatString { get; } = format;
}
