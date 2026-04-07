using System.Globalization;
using System.Resources;

namespace ExcelColumnExtractor.Resources;

internal static class Messages
{
    private static readonly ResourceManager ResourceManager =
        new("ExcelColumnExtractor.Resources.Messages", typeof(Messages).Assembly);

    public static string FileAlreadyOpen(string name, DateTime lastSaved)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(FileAlreadyOpen)), name, lastSaved);

    public static string FileAdded(string path)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(FileAdded)), path);

    public static string FileRemoved(string path)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(FileRemoved)), path);

    public static string FileUpdated(string file)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(FileUpdated)), file);

    public static string SheetScanned(string sheetName, int count)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(SheetScanned)), sheetName, count);

    private static string Get(string key)
        => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
