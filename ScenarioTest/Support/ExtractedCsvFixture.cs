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

        // 추출 결과는 컬렉션의 모든 테스트가 함께 읽는다. 누가 제자리에서 고치면 다른 테스트가 오염되므로 잠가 둔다.
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

    // 추출이 실패해 폴더가 없으면 그대로 둔다. 실패는 추출 결과를 보는 테스트가 드러낸다.
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
