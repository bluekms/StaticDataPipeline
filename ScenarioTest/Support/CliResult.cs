namespace ScenarioTest.Support;

public sealed record CliResult(int ExitCode, IReadOnlyList<string> LogLines)
{
    public List<string> Errors { get; } = Lines(LogLines, "[Error]");

    public List<string> Warnings { get; } = Lines(LogLines, "[Warning]");

    public List<string> InformationLines { get; } = Lines(LogLines, "[Information]");

    public string AllText { get; } = string.Join(Environment.NewLine, LogLines);

    private static List<string> Lines(IReadOnlyList<string> logLines, string levelToken)
    {
        return logLines.Where(x => x.Contains(levelToken, StringComparison.Ordinal)).ToList();
    }
}
