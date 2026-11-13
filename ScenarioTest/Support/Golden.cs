namespace ScenarioTest.Support;

public static class Golden
{
    public static void AssertEquals(string goldenRelativePath, string actualPath)
    {
        Assert.True(File.Exists(actualPath), FormattableString.Invariant($"실제 파일이 없다: {actualPath}"));

        var expectedPath = Path.Combine(SourceTree.OutputDirectory, "Golden", goldenRelativePath);
        var difference = FindDifference(goldenRelativePath, expectedPath, actualPath);
        if (difference is null)
        {
            return;
        }

        // 골든을 일부러 갱신할 때만 아래 줄을 주석으로 막고 돌린다. 돌린 뒤에는 반드시 되돌린다.
        Assert.Fail(difference);

        var target = Path.Combine(SourceTree.ProjectDirectory, "Golden", goldenRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(actualPath, target, overwrite: true);
        Assert.Fail(FormattableString.Invariant(
            $"골든 '{goldenRelativePath}' 을 실제 출력으로 갱신했다. git diff 로 바뀐 줄을 확인하고, Golden.cs 의 주석을 되돌린 뒤 다시 빌드해 돌려야 통과한다.\n{difference}"));
    }

    private static string? FindDifference(string goldenRelativePath, string expectedPath, string actualPath)
    {
        var exists = File.Exists(expectedPath);
        if (!exists)
        {
            return FormattableString.Invariant($"골든 파일이 없다: {expectedPath}");
        }

        var expected = File.ReadAllLines(expectedPath);
        var actual = File.ReadAllLines(actualPath);
        var count = Math.Max(expected.Length, actual.Length);
        for (var i = 0; i < count; i++)
        {
            var expectedLine = i < expected.Length ? expected[i] : "<end of file>";
            var actualLine = i < actual.Length ? actual[i] : "<end of file>";
            var same = string.Equals(expectedLine, actualLine, StringComparison.Ordinal);
            if (!same)
            {
                return FormattableString.Invariant(
                    $"{goldenRelativePath} 의 {i + 1} 번째 줄이 다르다.\n기대: {expectedLine}\n실제: {actualLine}");
            }
        }

        return null;
    }
}
