using System.Globalization;
using ClosedXML.Excel;

namespace ScenarioTest.Support;

public sealed class ScenarioWorkspace : IDisposable
{
    private int runCount;

    public ScenarioWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "SdpScenario", Guid.NewGuid().ToString("N"));
        ExcelMain = Path.Combine(Root, "excel", "Main");
        ExcelScience = Path.Combine(Root, "excel", "Science");
        Records = Path.Combine(Root, "records");
        CsvMain = Path.Combine(Root, "csv", "Main");
        CsvScience = Path.Combine(Root, "csv", "Science");
        LogRoot = Path.Combine(Root, "log");

        var sourceExcel = Path.Combine(SourceTree.OutputDirectory, "Excel");
        CopyFiles(Path.Combine(sourceExcel, "Main"), ExcelMain, "*.xlsx");
        CopyFiles(Path.Combine(sourceExcel, "Science"), ExcelScience, "*.xlsx");

        var sourceRecords = Path.Combine(SourceTree.OutputDirectory, "Records");
        Directory.CreateDirectory(Records);
        File.Copy(Path.Combine(sourceRecords, "SchoolLibrary.cs"), Path.Combine(Records, "SchoolLibrary.cs"));
        File.Copy(Path.Combine(sourceRecords, "SchoolTypes.cs"), Path.Combine(Records, "SchoolTypes.cs"));
        File.Copy(Path.Combine(sourceRecords, "Rpg.cs"), Path.Combine(Records, "Rpg.cs"));
        Directory.CreateDirectory(Path.Combine(Records, "Science"));
        File.Copy(Path.Combine(sourceRecords, "Science.cs"), ScienceRecord);
        CopyFiles(Path.Combine(sourceRecords, "Invalid"), Path.Combine(Records, "Invalid"), "*.cs");
        Directory.CreateDirectory(LogRoot);
    }

    public string Root { get; }

    public string ExcelMain { get; }

    public string ExcelScience { get; }

    public string Records { get; }

    public string ScienceRecord
        => Path.Combine(Records, "Science", "Science.cs");

    public string CsvMain { get; }

    public string CsvScience { get; }

    public string LogRoot { get; }

    public string InvalidRecord(string fileName)
        => Path.Combine(Records, "Invalid", fileName);

    public string WorkbookPath(string workbook)
        => workbook == "Science"
            ? Path.Combine(ExcelScience, "Science.xlsx")
            : Path.Combine(ExcelMain, workbook + ".xlsx");

    public void Mutate(string workbook, string sheet, Action<IXLWorksheet> mutate)
    {
        MutateWorkbook(workbook, x => mutate(x.Worksheet(sheet)));
    }

    public void MutateWorkbook(string workbook, Action<XLWorkbook> mutate)
    {
        var path = WorkbookPath(workbook);
        using var book = new XLWorkbook(path);
        mutate(book);
        book.Save();
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
        if (!Directory.Exists(logDir))
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
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // 잠긴 파일 시나리오가 남긴 핸들이 늦게 풀리면 임시 폴더는 OS 정리에 맡긴다.
        }
        catch (UnauthorizedAccessException)
        {
        }
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
