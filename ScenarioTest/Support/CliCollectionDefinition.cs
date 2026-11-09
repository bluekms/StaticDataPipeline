namespace ScenarioTest.Support;

// 추출기와 헤더 생성기는 Serilog 의 전역 Log.Logger 를 바꾸고 닫으므로 CLI 를 부르는 테스트는 한 줄로 돌린다.
[CollectionDefinition(Name)]
public sealed class CliCollectionDefinition : ICollectionFixture<ExtractedCsvFixture>
{
    public const string Name = "Cli";
}
