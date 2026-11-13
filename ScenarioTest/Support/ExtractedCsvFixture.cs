namespace ScenarioTest.Support;

public sealed class ExtractedCsvFixture : IDisposable
{
    private readonly ScenarioWorkspace workspace = new();

    public ExtractedCsvFixture()
    {
        CsvMain = workspace.CsvMain;
        CsvScience = workspace.CsvScience;
        MainResult = workspace.RunMain();
        ScienceResult = workspace.RunScience();
        AllHeadersPath = Path.Combine(workspace.Root, "headers", "AllHeaders.md");
        AllHeadersResult = workspace.RunHeaderGenerator("all-header", "-r", workspace.Records, "-o", AllHeadersPath);

        SetReadOnly(CsvMain, true);
        SetReadOnly(CsvScience, true);
    }

    public CliResult MainResult { get; }

    public CliResult ScienceResult { get; }

    public CliResult AllHeadersResult { get; }

    public string AllHeadersPath { get; }

    public string CsvMain { get; }

    public string CsvScience { get; }

    public string WorkbookPath(string workbook)
    {
        return workspace.WorkbookPath(workbook);
    }

    public TempDirectory CopyMainCsv()
    {
        return Copy(CsvMain);
    }

    public TempDirectory CopyScienceCsv()
    {
        return Copy(CsvScience);
    }

    private static TempDirectory Copy(string csvDirectory)
    {
        var copy = new TempDirectory();
        foreach (var file in Directory.GetFiles(csvDirectory, "*.csv"))
        {
            var target = copy.FileOf(Path.GetFileName(file));
            File.Copy(file, target);
            File.SetAttributes(target, FileAttributes.Normal);
        }

        return copy;
    }

    public void Dispose()
    {
        SetReadOnly(CsvMain, false);
        SetReadOnly(CsvScience, false);
        workspace.Dispose();
    }

    private static void SetReadOnly(string directory, bool readOnly)
    {
        var exists = Directory.Exists(directory);
        if (!exists)
        {
            return;
        }

        var attributes = readOnly ? FileAttributes.ReadOnly : FileAttributes.Normal;
        foreach (var file in Directory.GetFiles(directory))
        {
            File.SetAttributes(file, attributes);
        }
    }
}
