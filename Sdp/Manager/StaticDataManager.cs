using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sdp.Resources;

namespace Sdp.Manager;

public abstract class StaticDataManager<TTableSet>(ILogger logger)
    where TTableSet : class
{
    private volatile TTableSet current = null!;

    protected TTableSet Current => current;

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
        current = tableSet;

        stopwatch.Stop();
        logger.LogInformation(
            Messages.LoadAsyncCompleted,
            stopwatch.ElapsedMilliseconds);
    }

    protected virtual void Validate(TTableSet tableSet)
    {
    }
}
