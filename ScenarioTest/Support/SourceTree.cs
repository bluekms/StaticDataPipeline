namespace ScenarioTest.Support;

public static class SourceTree
{
    public static string ProjectDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var projectFile = Path.Combine(directory.FullName, "ScenarioTest.csproj");
                if (File.Exists(projectFile))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("ScenarioTest.csproj not found above the test output folder.");
        }
    }

    public static string OutputDirectory
        => AppContext.BaseDirectory;
}
