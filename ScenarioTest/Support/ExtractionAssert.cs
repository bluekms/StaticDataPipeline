namespace ScenarioTest.Support;

public static class ExtractionAssert
{
    public static void FailsWith(CliResult result, params string[] fragments)
    {
        Assert.True(result.ExitCode == 1, FormattableString.Invariant(
            $"Expected exit code 1 but was {result.ExitCode}.\n{result.AllText}"));
        Assert.NotEmpty(result.Errors);
        foreach (var fragment in fragments)
        {
            Assert.True(result.AllText.Contains(fragment, StringComparison.Ordinal), FormattableString.Invariant(
                $"Log does not contain '{fragment}'.\n{result.AllText}"));
        }
    }

    public static List<string> WarningsExceptLoansStartCell(CliResult result)
        => result.Warnings
            .Where(x => !x.Contains("LoanRecord", StringComparison.Ordinal))
            .ToList();
}
