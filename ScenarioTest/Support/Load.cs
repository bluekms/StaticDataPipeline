using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;

namespace ScenarioTest.Support;

public static class Load
{
    public static Func<Task> Rpg(string csvDir, List<string>? disabledTables = null)
    {
        return () => new RpgStaticData(NullLogger.Instance).LoadAsync(csvDir, disabledTables);
    }

    public static Func<Task> School(string csvDir, List<string>? disabledTables = null)
    {
        return () => new SchoolStaticData(NullLogger.Instance).LoadAsync(csvDir, disabledTables);
    }

    public static Func<Task> Science(string csvDir, List<string>? disabledTables = null)
    {
        return () => new ScienceStaticData(NullLogger.Instance).LoadAsync(csvDir, disabledTables);
    }

    public static Func<Task> ForCsv(string csvDir, string csvFileName)
    {
        var isRpg = csvFileName.StartsWith("Rpg.", StringComparison.Ordinal);
        if (isRpg)
        {
            return Rpg(csvDir);
        }

        return School(csvDir);
    }
}
