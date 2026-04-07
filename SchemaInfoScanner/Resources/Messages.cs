using System.Globalization;
using System.Resources;

namespace SchemaInfoScanner.Resources;

internal static class Messages
{
    private static readonly ResourceManager ResourceManager =
        new("SchemaInfoScanner.Resources.Messages", typeof(Messages).Assembly);

    public static string AlreadyVisited(string name)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(AlreadyVisited)), name);

    public static string Ignored(string name)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(Ignored)), name);

    public static string RecordStarted(string name)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(RecordStarted)), name);

    public static string RecordFinished(string name)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(RecordFinished)), name);

    public static string CodeNotCompilable(string code)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(CodeNotCompilable)), code);

    private static string Get(string key)
        => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
