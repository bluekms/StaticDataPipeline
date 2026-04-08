using System.Collections;
using System.Globalization;
using System.Resources;

namespace UnitTest;

public class MessagesLocalizationTests
{
    private static readonly string[] SupportedCultures = ["ko"];

    public static IEnumerable<object[]> MessageTypes =>
    [
        ["Sdp.Resources.Messages", typeof(Sdp.Resources.Messages)],
        ["CLICommonLibrary.Resources.Messages", typeof(CLICommonLibrary.Logger)],
        ["ExcelColumnExtractor.Resources.Messages", typeof(ExcelColumnExtractor.Resources.Messages)],
        ["SchemaInfoScanner.Resources.Messages", typeof(SchemaInfoScanner.Resources.Messages)],
        ["StaticDataHeaderGenerator.Resources.Messages", typeof(StaticDataHeaderGenerator.Resources.Messages)],
    ];

    [Theory]
    [MemberData(nameof(MessageTypes))]
    public void AllDefaultKeysExist_InAllCultures(string resourceName, Type anchorType)
    {
        var rm = new ResourceManager(resourceName, anchorType.Assembly);
        var defaultSet = rm.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException(FormattableString.Invariant(
                $"Default resource set not found for {resourceName}."));

        foreach (var culture in SupportedCultures)
        {
            var cultureSet = rm.GetResourceSet(CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: false)
                ?? throw new InvalidOperationException(FormattableString.Invariant(
                    $"Resource set for '{culture}' not found in {resourceName}."));

            var missingKeys = new List<string>();
            foreach (DictionaryEntry entry in defaultSet)
            {
                var key = (string)entry.Key;
                if (cultureSet.GetString(key) is null)
                {
                    missingKeys.Add(key);
                }
            }

            Assert.Empty(missingKeys);
        }
    }

    [Theory]
    [MemberData(nameof(MessageTypes))]
    public void NoCultureOnlyKeys_InAllCultures(string resourceName, Type anchorType)
    {
        var rm = new ResourceManager(resourceName, anchorType.Assembly);
        var defaultSet = rm.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException(FormattableString.Invariant(
                $"Default resource set not found for {resourceName}."));

        var defaultKeys = new HashSet<string>(defaultSet.Cast<DictionaryEntry>().Select(e => (string)e.Key));

        foreach (var culture in SupportedCultures)
        {
            var cultureSet = rm.GetResourceSet(CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: false)
                ?? throw new InvalidOperationException(FormattableString.Invariant(
                    $"Resource set for '{culture}' not found in {resourceName}."));

            var extraKeys = cultureSet.Cast<DictionaryEntry>()
                .Select(e => (string)e.Key)
                .Where(k => !defaultKeys.Contains(k))
                .ToList();

            Assert.Empty(extraKeys);
        }
    }
}
