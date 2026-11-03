namespace ScenarioTest.Support;

public static class Golden
{
    public static bool IsUpdating
        => Environment.GetEnvironmentVariable("SDP_UPDATE_GOLDEN") == "1";

    public static void AssertEquals(string goldenRelativePath, string actualPath)
    {
        Assert.True(File.Exists(actualPath), FormattableString.Invariant($"Actual file not found: {actualPath}"));

        if (IsUpdating)
        {
            var target = Path.Combine(SourceTree.ProjectDirectory, "Golden", goldenRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(actualPath, target, overwrite: true);
            return;
        }

        var expectedPath = Path.Combine(SourceTree.OutputDirectory, "Golden", goldenRelativePath);
        Assert.True(File.Exists(expectedPath), FormattableString.Invariant($"Golden file not found: {expectedPath}"));

        var expected = File.ReadAllLines(expectedPath);
        var actual = File.ReadAllLines(actualPath);
        var count = Math.Max(expected.Length, actual.Length);
        for (var i = 0; i < count; i++)
        {
            var expectedLine = i < expected.Length ? expected[i] : "<end of file>";
            var actualLine = i < actual.Length ? actual[i] : "<end of file>";
            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                Assert.Fail(FormattableString.Invariant(
                    $"{goldenRelativePath} line {i + 1} differs.\nexpected: {expectedLine}\nactual:   {actualLine}"));
            }
        }
    }
}
