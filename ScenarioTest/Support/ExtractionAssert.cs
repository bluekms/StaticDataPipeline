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

    // Loans 시트는 Record 속성의 시작 셀 "A1" 이 CLI 의 B3 보다 우선해 정상 추출에서도 경고 하나를 남긴다. 그 경고만 뺀다.
    public static List<string> WarningsExceptLoansStartCell(CliResult result)
    {
        return result.Warnings
            .Where(x => !IsLoansStartCellWarning(x))
            .ToList();
    }

    private static bool IsLoansStartCellWarning(string warning)
    {
        return warning.Contains("LoanRecord", StringComparison.Ordinal)
            && warning.Contains("'A1'", StringComparison.Ordinal);
    }
}
