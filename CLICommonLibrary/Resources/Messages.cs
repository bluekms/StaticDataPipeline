using System.Globalization;
using System.Resources;

namespace CLICommonLibrary.Resources;

internal static class Messages
{
    private static readonly ResourceManager ResourceManager =
        new("CLICommonLibrary.Resources.Messages", typeof(Messages).Assembly);

    public static string ExceptionCount(int count)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(ExceptionCount)), count);

    private static string Get(string key)
        => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
