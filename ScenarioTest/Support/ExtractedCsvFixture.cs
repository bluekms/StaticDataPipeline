namespace ScenarioTest.Support;

public sealed class ExtractedCsvFixture : IDisposable
{
    private readonly ScenarioWorkspace workspace = new();

    public ExtractedCsvFixture()
    {
        MainResult = workspace.RunMain();
        ScienceResult = workspace.RunScience();
    }

    public CliResult MainResult { get; }

    public CliResult ScienceResult { get; }

    public string CsvMain
        => workspace.CsvMain;

    public string CsvScience
        => workspace.CsvScience;

    public TempDirectory CopyMainCsv()
        => Copy(CsvMain);

    public TempDirectory CopyScienceCsv()
        => Copy(CsvScience);

    private static TempDirectory Copy(string csvDirectory)
    {
        var copy = new TempDirectory();
        foreach (var file in Directory.GetFiles(csvDirectory, "*.csv"))
        {
            File.Copy(file, copy.File(Path.GetFileName(file)));
        }

        return copy;
    }

    public void Dispose()
    {
        workspace.Dispose();
    }
}
