namespace UnitTest.Utility;

internal sealed class CsvTestDirectory : IDisposable
{
    public string Path { get; }

    public CsvTestDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        Directory.CreateDirectory(Path);
    }

    public void Write(string fileName, string content)
        => File.WriteAllText(System.IO.Path.Combine(Path, fileName), content);

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
