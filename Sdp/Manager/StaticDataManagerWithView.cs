using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sdp.Resources;

namespace Sdp.Manager;

public abstract class StaticDataManager<TTableSet, TViewSet>(ILogger logger)
    where TTableSet : class
    where TViewSet : class
{
    private volatile TableAndViewSet current = null!;
    private int loading;

    public TableAndViewSet Current => current;

    public async Task LoadAsync(string csvDir, List<string>? disabledTables = null)
    {
        if (Interlocked.CompareExchange(ref loading, 1, 0) != 0)
        {
            throw new InvalidOperationException(Messages.LoadAsyncAlreadyInProgress);
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            var tableSet = await LoadTableSetAsync(csvDir, disabledTables, logger);

            ValidateForeignKeys(tableSet);
            Validate(tableSet);

            var viewSet = BuildViewSet(tableSet, logger);
            current = new TableAndViewSet(tableSet, viewSet);

            stopwatch.Stop();
            logger.LogInformation(
                Messages.LoadAsyncCompleted,
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            Interlocked.Exchange(ref loading, 0);
        }
    }

    protected abstract Task<TTableSet> LoadTableSetAsync(
        string csvDir,
        List<string>? disabledTables,
        ILogger logger);

    protected abstract void ValidateForeignKeys(TTableSet tableSet);

    protected abstract TViewSet BuildViewSet(TTableSet tableSet, ILogger logger);

    protected virtual void Validate(TTableSet tableSet)
    {
    }

    public sealed record TableAndViewSet(TTableSet Tables, TViewSet Views);
}
