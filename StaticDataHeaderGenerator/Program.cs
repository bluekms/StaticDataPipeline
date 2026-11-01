using System.Globalization;
using CommandLine;
using StaticDataHeaderGenerator.OptionHandlers;
using StaticDataHeaderGenerator.ProgramOptions;
using StaticDataHeaderGenerator.Resources;

namespace StaticDataHeaderGenerator;

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Parser.Default.ParseArguments<
                    GenerateHeaderOptions,
                    GenerateAllHeaderOptions>(args)
                .MapResult(
                    (GenerateHeaderOptions options) => GenerateHeaderHandler.Generate(options),
                    (GenerateAllHeaderOptions options) => GenerateAllHeaderHandler.Generate(options),
                    HandleParseError);
        }
        finally
        {
            Serilog.Log.CloseAndFlush();
        }
    }

    private static int HandleParseError(IEnumerable<Error> errors)
    {
        var errorList = errors.ToList();
        if (errorList.All(error => error is HelpRequestedError or HelpVerbRequestedError or VersionRequestedError))
        {
            return 0;
        }

        Console.WriteLine(string.Format(
            CultureInfo.CurrentCulture,
            Messages.Composite.ParseErrorCount,
            errorList.Count));

        foreach (var error in errorList)
        {
            Console.WriteLine(error.ToString());
        }

        return 1;
    }
}
