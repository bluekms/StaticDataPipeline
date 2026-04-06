using CommandLine;
using Serilog.Events;

namespace ExcelColumnExtractor;

public sealed class ProgramOptions
{
    [Option('r', "record-path", Required = true, HelpText = "C# 레코드 파일 경로")]
    public string RecordCsPath { get; set; } = null!;

    [Option('e', "excel-path", Required = true, HelpText = "액셀 파일 경로")]
    public string ExcelPath { get; set; } = null!;

    [Option('o', "output-path", Required = true, HelpText = "출력 파일 경로")]
    public string OutputPath { get; set; } = null!;

    [Option('v', "version", Required = false, HelpText = "출력 버전")]
    public string? Version { get; set; }

    [Option('c', "encoding", Required = false, Default = "UTF-8", HelpText = "UTF-8 has no bom. (e.g., UTF-8, UTF-16, etc.).")]
    public string? Encoding { get; set; }

    [Option('l', "log-path", Required = false, HelpText = "로그 파일 경로")]
    public string? LogPath { get; set; }

    [Option('m', "min-log-level", Required = false, Default = LogEventLevel.Information, HelpText = "최소 로그 레벨 (Verbose, Debug, Information, Warning, Error, Fatal)")]
    public LogEventLevel MinLogLevel { get; set; }

    [Option('f', "force", Required = false, Default = false, HelpText = "출력 디렉터리에 파일이 있어도 강제로 덮어씀")]
    public bool Force { get; set; }
}
