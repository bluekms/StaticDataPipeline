namespace ScenarioTest.Support;

public static class LoadAssert
{
    public const string TablesFailed = "One or more tables failed to load.";
    public const string FkFailed = "FK validation failed.";
    public const string ViewsFailed = "One or more views failed to build.";

    public static async Task<AggregateException> FailsWithAggregate(Func<Task> load, string expectedMessageStart)
    {
        var exception = await Record.ExceptionAsync(load);

        var aggregate = Assert.IsType<AggregateException>(exception);
        Assert.StartsWith(expectedMessageStart, aggregate.Message, StringComparison.Ordinal);

        return aggregate;
    }

    public static string Describe(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            var inner = aggregate.InnerExceptions.Select(Describe);
            return aggregate.Message + "\n" + string.Join("\n", inner);
        }

        if (exception.InnerException is not null)
        {
            return exception.Message + "\n" + Describe(exception.InnerException);
        }

        return exception.Message;
    }

    public static void ContainsAll(Exception exception, params string[] fragments)
    {
        var text = Describe(exception);
        foreach (var fragment in fragments)
        {
            Assert.True(text.Contains(fragment, StringComparison.Ordinal), FormattableString.Invariant(
                $"Exception does not contain '{fragment}'.\n{text}"));
        }
    }
}
