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
                var found = File.Exists(projectFile);
                if (found)
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("테스트 출력 폴더의 상위에서 ScenarioTest.csproj 를 찾지 못했다.");
        }
    }

    public static string OutputDirectory { get; } = AppContext.BaseDirectory;
}
