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
            File.Copy(file, copy.FileOf(Path.GetFileName(file)));
        }

        return copy;
    }

    public void Dispose()
    {
        workspace.Dispose();
    }
}
