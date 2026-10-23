using System.Reflection;

namespace UnitTest.Utility;

internal sealed partial class TestDataDirectory : IDisposable
{
    private const string ResourceNamespace = "UnitTest.TestData";

    public string Path { get; }

    public TestDataDirectory(params string[] resourceFileNames)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        Directory.CreateDirectory(Path);

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var fileName in resourceFileNames)
        {
            var resourceName = FormattableString.Invariant($"{ResourceNamespace}.{fileName}");
            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is null)
            {
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"Embedded resource not found: {resourceName}"));
            }

            var filePath = System.IO.Path.Combine(Path, fileName);
            using var fileStream = File.Create(filePath);
            resourceStream.CopyTo(fileStream);
        }
    }

    public string GetFilePath(string fileName)
        => System.IO.Path.Combine(Path, fileName);

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
