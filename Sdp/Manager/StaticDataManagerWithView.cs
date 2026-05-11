using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sdp.Resources;
using Sdp.View;

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

            var fkTargetError = ForeignKeyTargetValidator.Validate<TTableSet>();
            if (fkTargetError is not null)
            {
                throw fkTargetError;
            }

            var tableSet = await TableSetLoader.LoadAsync<TTableSet>(csvDir, disabledTables, logger);

            ReferenceValidator.Validate(tableSet);
            Validate(tableSet);

            var viewSet = ViewSetBuilder.Build<TTableSet, TViewSet>(tableSet, logger);
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

    protected virtual void Validate(TTableSet tableSet)
    {
    }

    public sealed record TableAndViewSet(TTableSet Tables, TViewSet Views);
}
