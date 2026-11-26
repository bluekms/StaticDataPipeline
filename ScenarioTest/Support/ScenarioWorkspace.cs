using System.Globalization;

namespace ScenarioTest.Support;

// 시나리오 하나가 쓰는 작업 폴더
public sealed class ScenarioWorkspace : IDisposable
{
    private readonly TempDirectory root = new();
    private int runCount;

    public ScenarioWorkspace()
    {
        Root = root.Root;
        ExcelSchool = Path.Combine(Root, "excel", "School");
        ExcelRpg = Path.Combine(Root, "excel", "Rpg");
        ExcelScience = Path.Combine(Root, "excel", "Science");
        RecordsSchool = Path.Combine(Root, "records", "School");
        RecordsRpg = Path.Combine(Root, "records", "Rpg");
        ScienceRecord = Path.Combine(Root, "records", "Science", "Science.cs");
        CsvSchool = Path.Combine(Root, "csv", "School");
        CsvRpg = Path.Combine(Root, "csv", "Rpg");
        CsvScience = Path.Combine(Root, "csv", "Science");
        LogRoot = Path.Combine(Root, "log");

        var source = AppContext.BaseDirectory;
        CopyFiles(Path.Combine(source, "Feature", "School", "Excel"), ExcelSchool, "*.xlsx");
        CopyFiles(Path.Combine(source, "Feature", "Rpg", "Excel"), ExcelRpg, "*.xlsx");
        CopyFiles(Path.Combine(source, "Feature", "Science", "Excel"), ExcelScience, "*.xlsx");
        CopyFiles(Path.Combine(source, "Feature", "School", "Records"), RecordsSchool, "*.cs");
        CopyFiles(Path.Combine(source, "Feature", "Rpg", "Records"), RecordsRpg, "*.cs");
        var recordsScience = Path.Combine(Root, "records", "Science");
        CopyFiles(Path.Combine(source, "Feature", "Science", "Records"), recordsScience, "*.cs");
        CopyFiles(Path.Combine(source, "InvalidRecords"), Path.Combine(Root, "records", "Invalid"), "*.cs");
        Directory.CreateDirectory(LogRoot);
    }

    public string Root { get; }

    public string ExcelSchool { get; }

    public string ExcelRpg { get; }

    public string ExcelScience { get; }

    public string RecordsSchool { get; }

    public string RecordsRpg { get; }

    public string ScienceRecord { get; }

    public string CsvSchool { get; }

    public string CsvRpg { get; }

    public string CsvScience { get; }

    public string LogRoot { get; }

    public string InvalidRecord(string fileName)
    {
        return Path.Combine(Root, "records", "Invalid", fileName);
    }

    public string WorkbookPath(string workbook)
    {
        if (workbook == "Science")
        {
            return Path.Combine(ExcelScience, "Science.xlsx");
        }

        if (workbook == "Rpg")
        {
            return Path.Combine(ExcelRpg, "Rpg.xlsx");
        }

        return Path.Combine(ExcelSchool, workbook + ".xlsx");
    }

    public void UseBroken(string brokenPath)
    {
        var workbook = brokenPath.Split('/')[0];
        var feature = FeatureOf(workbook);
        var file = Path.Combine(AppContext.BaseDirectory, "Feature", feature, "Broken", brokenPath + ".xlsx");
        var exists = File.Exists(file);
        if (!exists)
        {
            throw new ArgumentException(FormattableString.Invariant($"깨진 워크북 '{brokenPath}' 이 없다: {file}"));
        }

        File.Copy(file, WorkbookPath(workbook), overwrite: true);
    }

    private static string FeatureOf(string workbook)
    {
        if (workbook == "Science")
        {
            return "Science";
        }

        if (workbook == "Rpg")
        {
            return "Rpg";
        }

        return "School";
    }

    public CliResult RunBroken(string brokenPath)
    {
        UseBroken(brokenPath);

        var workbook = brokenPath.Split('/')[0];
        if (workbook == "Science")
        {
            return RunScience();
        }

        if (workbook == "Rpg")
        {
            return RunRpg();
        }

        return RunSchool();
    }

    public CliResult RunSchool(params string[] extraArgs)
    {
        string[] args = ["-r", RecordsSchool, "-e", ExcelSchool, "-o", CsvSchool, "-s", "B3", .. extraArgs];
        return RunExtractor(args);
    }

    public CliResult RunRpg(params string[] extraArgs)
    {
        string[] args = ["-r", RecordsRpg, "-e", ExcelRpg, "-o", CsvRpg, "-s", "B3", .. extraArgs];
        return RunExtractor(args);
    }

    public CliResult RunScience(params string[] extraArgs)
    {
        string[] args = ["-r", ScienceRecord, "-e", ExcelScience, "-o", CsvScience, .. extraArgs];
        return RunExtractor(args);
    }

    public CliResult RunExtractor(params string[] args)
    {
        var logDir = NextLogDirectory();
        string[] fullArgs = [.. args, "-l", logDir];

        using var culture = CultureScope.Invariant();
        var exitCode = ExcelColumnExtractor.Program.Main(fullArgs);

        return new CliResult(exitCode, ReadLog(logDir));
    }

    public CliResult RunHeaderGenerator(params string[] args)
    {
        var logDir = NextLogDirectory();
        string[] fullArgs = [.. args, "-l", logDir];

        using var culture = CultureScope.Invariant();
        var exitCode = StaticDataHeaderGenerator.Program.Main(fullArgs);

        return new CliResult(exitCode, ReadLog(logDir));
    }

    public static List<string> ReadLog(string logDir)
    {
        var lines = new List<string>();
        var exists = Directory.Exists(logDir);
        if (!exists)
        {
            return lines;
        }

        foreach (var file in Directory.GetFiles(logDir, "log*.txt").Order(StringComparer.Ordinal))
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            var fileLines = text
                .Split('\n')
                .Select(x => x.TrimEnd('\r'))
                .Where(x => x.Length > 0);
            lines.AddRange(fileLines);
        }

        return lines;
    }

    public string NextLogDirectory()
    {
        runCount++;
        var path = Path.Combine(LogRoot, runCount.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(path);

        return path;
    }

    public void Dispose()
    {
        root.Dispose();
    }

    private static void CopyFiles(string sourceDir, string targetDir, string pattern)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir, pattern))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
        }
    }
}
