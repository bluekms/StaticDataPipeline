namespace ScenarioTest.Support;

public sealed record CliResult(int ExitCode, IReadOnlyList<string> LogLines)
{
    public List<string> Errors
        => Lines("[Error]");

    public List<string> Warnings
        => Lines("[Warning]");

    public List<string> InformationLines
        => Lines("[Information]");

    public string AllText
        => string.Join(Environment.NewLine, LogLines);

    private List<string> Lines(string levelToken)
        => LogLines.Where(x => x.Contains(levelToken, StringComparison.Ordinal)).ToList();
}
