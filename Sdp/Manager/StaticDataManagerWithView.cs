using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sdp.Resources;
using Sdp.View;

namespace Sdp.Manager;

public abstract class StaticDataManager<TTableSet, TViewSet>(ILogger logger)
    where TTableSet : class
    where TViewSet : class
{
    private volatile State current = null!;

    protected TTableSet CurrentTables => current.Tables;

    protected TViewSet CurrentViews => current.Views;

    public async Task LoadAsync(string csvDir, List<string>? disabledTables = null)
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
        current = new State(tableSet, viewSet);

        stopwatch.Stop();
        logger.LogInformation(
            Messages.LoadAsyncCompleted,
            stopwatch.ElapsedMilliseconds);
    }

    protected virtual void Validate(TTableSet tableSet)
    {
    }

    private sealed record State(TTableSet Tables, TViewSet Views);
}
