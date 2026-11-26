namespace ScenarioTest.Support;

public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Root = Path.Combine(Path.GetTempPath(), "SdpScenario", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string FileOf(string fileName)
    {
        return Path.Combine(Root, fileName);
    }

    public void Edit(string csvFileName, Action<CsvDocument> edit)
    {
        CsvDocument.Edit(FileOf(csvFileName), edit);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // 잠긴 파일 시나리오가 남긴 핸들이 늦게 풀리면 임시 폴더는 OS 정리에 맡긴다.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
