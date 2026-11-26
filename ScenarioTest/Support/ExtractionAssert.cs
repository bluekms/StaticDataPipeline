namespace ScenarioTest.Support;

public static class ExtractionAssert
{
    public static void Succeeded(CliResult result)
    {
        Assert.True(result.ExitCode == 0, FormattableString.Invariant(
            $"종료 코드가 0 이어야 하는데 {result.ExitCode} 이다.\n{result.AllText}"));
    }

    public static void FailsWith(CliResult result, params string[] fragments)
    {
        Assert.True(result.ExitCode == 1, FormattableString.Invariant(
            $"종료 코드가 1 이어야 하는데 {result.ExitCode} 이다.\n{result.AllText}"));
        Assert.True(result.Errors.Count > 0, FormattableString.Invariant(
            $"종료 코드는 1 인데 로그에 [Error] 줄이 없다.\n{result.AllText}"));
        foreach (var fragment in fragments)
        {
            Assert.True(result.AllText.Contains(fragment, StringComparison.Ordinal), FormattableString.Invariant(
                $"로그에 '{fragment}' 가 없다.\n{result.AllText}"));
        }
    }
}
