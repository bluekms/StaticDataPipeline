using ScenarioTest.Support;

namespace ScenarioTest.Fixtures;

public class ExcelFixtureBuildTests
{
    [Fact]
    public void BuildExcelFixtures()
    {
        if (Environment.GetEnvironmentVariable("SDP_BUILD_EXCEL") != "1")
        {
            return;
        }

        ExcelFixtures.Build(Path.Combine(SourceTree.ProjectDirectory, "Excel"));
    }
}
