using System.Globalization;
using System.Resources;

namespace StaticDataHeaderGenerator.Resources;

internal static class Messages
{
    private static readonly ResourceManager ResourceManager =
        new("StaticDataHeaderGenerator.Resources.Messages", typeof(Messages).Assembly);

    public static string GeneratingHeaderFile()
        => Get(nameof(GeneratingHeaderFile));

    public static string HeadersGenerated(string name)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(HeadersGenerated)), name);

    public static string HeaderFileSaved(string path)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(HeaderFileSaved)), path);

    private static string Get(string key)
        => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
