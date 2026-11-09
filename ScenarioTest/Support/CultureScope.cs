using System.Globalization;

namespace ScenarioTest.Support;

public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo previousCulture;
    private readonly CultureInfo previousUICulture;

    public CultureScope(CultureInfo culture)
    {
        previousCulture = CultureInfo.CurrentCulture;
        previousUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static CultureScope Invariant()
    {
        return new(CultureInfo.InvariantCulture);
    }

    public static CultureScope Of(string name)
    {
        return new(CultureInfo.GetCultureInfo(name));
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = previousCulture;
        CultureInfo.CurrentUICulture = previousUICulture;
    }
}
