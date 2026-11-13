using System.Globalization;

namespace ScenarioTest.Support;

public sealed class ScenarioWorkspace : IDisposable
{
    private readonly TempDirectory root = new();
    private int runCount;

    public ScenarioWorkspace()
    {
        Root = root.Root;
        ExcelMain = Path.Combine(Root, "excel", "Main");
        ExcelScience = Path.Combine(Root, "excel", "Science");
        Records = Path.Combine(Root, "records", "Main");
        ScienceRecord = Path.Combine(Root, "records", "Science", "Science.cs");
        CsvMain = Path.Combine(Root, "csv", "Main");
        CsvScience = Path.Combine(Root, "csv", "Science");
        LogRoot = Path.Combine(Root, "log");

        var sourceExcel = Path.Combine(AppContext.BaseDirectory, "Excel");
        CopyFiles(Path.Combine(sourceExcel, "Main"), ExcelMain, "*.xlsx");
        CopyFiles(Path.Combine(sourceExcel, "Science"), ExcelScience, "*.xlsx");

        var sourceRecords = Path.Combine(AppContext.BaseDirectory, "Records");
        CopyFiles(Path.Combine(sourceRecords, "Main"), Records, "*.cs");
        CopyFiles(Path.Combine(sourceRecords, "Science"), Path.Combine(Root, "records", "Science"), "*.cs");
        CopyFiles(Path.Combine(sourceRecords, "Invalid"), Path.Combine(Root, "records", "Invalid"), "*.cs");
        Directory.CreateDirectory(LogRoot);
    }

    public string Root { get; }

    public string ExcelMain { get; }

    public string ExcelScience { get; }

    public string Records { get; }

    public string ScienceRecord { get; }

    public string CsvMain { get; }

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

        return Path.Combine(ExcelMain, workbook + ".xlsx");
    }

    // Excel/Broken/<워크북>/<시트>/<사례>.xlsx 로 같은 이름의 정상 워크북을 바꿔 끼운다. 폴더가 Excel/Main 의 워크북·시트 구조를 따른다.
    public void UseBroken(string brokenPath)
    {
        var workbook = brokenPath.Split('/')[0];
        var file = Path.Combine(AppContext.BaseDirectory, "Excel", "Broken", brokenPath + ".xlsx");
        var exists = File.Exists(file);
        if (!exists)
        {
            throw new ArgumentException(FormattableString.Invariant($"깨진 워크북 '{brokenPath}' 이 없다: {file}"));
        }

        File.Copy(file, WorkbookPath(workbook), overwrite: true);
    }

    public CliResult RunMain(params string[] extraArgs)
    {
        string[] args = ["-r", Records, "-e", ExcelMain, "-o", CsvMain, "-s", "B3", .. extraArgs];
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
