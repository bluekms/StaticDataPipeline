namespace ScenarioTest.Support;

public sealed class ExtractedCsvFixture : IDisposable
{
    private readonly ScenarioWorkspace workspace = new();

    public ExtractedCsvFixture()
    {
        CsvSchool = workspace.CsvSchool;
        CsvRpg = workspace.CsvRpg;
        CsvScience = workspace.CsvScience;
        SchoolResult = workspace.RunSchool();
        RpgResult = workspace.RunRpg();
        ScienceResult = workspace.RunScience();
        SchoolHeadersPath = Path.Combine(workspace.Root, "headers", "AllHeaders.School.md");
        SchoolHeadersResult = workspace.RunHeaderGenerator("all-header", "-r", workspace.RecordsSchool, "-o", SchoolHeadersPath);
        RpgHeadersPath = Path.Combine(workspace.Root, "headers", "AllHeaders.Rpg.md");
        RpgHeadersResult = workspace.RunHeaderGenerator("all-header", "-r", workspace.RecordsRpg, "-o", RpgHeadersPath);

        SetReadOnly(CsvSchool, true);
        SetReadOnly(CsvRpg, true);
        SetReadOnly(CsvScience, true);
    }

    public CliResult SchoolResult { get; }

    public CliResult RpgResult { get; }

    public CliResult ScienceResult { get; }

    public CliResult SchoolHeadersResult { get; }

    public CliResult RpgHeadersResult { get; }

    public string SchoolHeadersPath { get; }

    public string RpgHeadersPath { get; }

    public string CsvSchool { get; }

    public string CsvRpg { get; }

    public string CsvScience { get; }

    public string WorkbookPath(string workbook)
    {
        return workspace.WorkbookPath(workbook);
    }

    public TempDirectory CopySchoolCsv()
    {
        return Copy(CsvSchool);
    }

    public TempDirectory CopyRpgCsv()
    {
        return Copy(CsvRpg);
    }

    public TempDirectory CopyCsvFor(string csvFileName)
    {
        var isRpg = csvFileName.StartsWith("Rpg.", StringComparison.Ordinal);
        if (isRpg)
        {
            return CopyRpgCsv();
        }

        return CopySchoolCsv();
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
        SetReadOnly(CsvSchool, false);
        SetReadOnly(CsvRpg, false);
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
