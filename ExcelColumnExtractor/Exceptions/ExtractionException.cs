using System.Globalization;
using SchemaInfoScanner.Schemata;

namespace ExcelColumnExtractor.Exceptions;

public sealed class ExtractionException : Exception
{
    public sealed record Failure(RecordSchema RecordSchema, Exception Cause);

    public IReadOnlyList<Failure> Failures { get; }

    public ExtractionException(IReadOnlyList<Failure> failures)
        : base(BuildMessage(failures), BuildInnerException(failures))
    {
        Failures = failures;
    }

    private static Exception BuildInnerException(IReadOnlyList<Failure> failures)
    {
        if (failures.Count == 1)
        {
            return failures[0].Cause;
        }

        return new AggregateException(failures.Select(x => x.Cause));
    }

    private static string BuildMessage(IReadOnlyList<Failure> failures)
    {
        return string.Join(
            Environment.NewLine,
            failures.Select(x => string.Format(CultureInfo.InvariantCulture, "{0}: {1}", x.RecordSchema.RecordName.FullName, x.Cause.Message)));
    }
}
