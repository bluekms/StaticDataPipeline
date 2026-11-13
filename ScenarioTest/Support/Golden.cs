namespace ScenarioTest.Support;

public static class Golden
{
    private const string UpdateHint = "의도한 변경이면 ScenarioTest/update-golden.bat 로 골든을 다시 만든다.";

    public static void AssertEquals(string goldenRelativePath, string actualPath)
    {
        Assert.True(File.Exists(actualPath), FormattableString.Invariant($"실제 파일이 없다: {actualPath}"));

        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Golden", goldenRelativePath);
        Assert.True(File.Exists(expectedPath), FormattableString.Invariant(
            $"골든 파일이 없다: {expectedPath}\n{UpdateHint}"));

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
                Assert.Fail(FormattableString.Invariant(
                    $"{goldenRelativePath} 의 {i + 1} 번째 줄이 다르다.\n기대: {expectedLine}\n실제: {actualLine}\n{UpdateHint}"));
            }
        }
    }
}
