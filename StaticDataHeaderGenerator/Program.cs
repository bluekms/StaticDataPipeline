using System.Globalization;
using CommandLine;
using StaticDataHeaderGenerator.OptionHandlers;
using StaticDataHeaderGenerator.ProgramOptions;
using StaticDataHeaderGenerator.Resources;

namespace StaticDataHeaderGenerator;

internal class Program
{
    private static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<
                GenerateHeaderOptions,
                GenerateAllHeaderOptions>(args)
            .MapResult(
                (GenerateHeaderOptions options) => GenerateHeaderHandler.Generate(options),
                (GenerateAllHeaderOptions options) => GenerateAllHeaderHandler.Generate(options),
                HandleParseError);
    }

    private static int HandleParseError(IEnumerable<Error> errors)
    {
        var errorList = errors.ToList();

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
