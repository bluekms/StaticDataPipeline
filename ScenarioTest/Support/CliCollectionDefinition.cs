namespace ScenarioTest.Support;

// 병렬로 실행하면 서로의 Serilog 의 전역 Log.Logger 를 닫으므로, CLI 를 부르는 테스트 클래스는 이 컬렉션에 넣어 순차적으로 실행
[CollectionDefinition(Name)]
public sealed class CliCollectionDefinition : ICollectionFixture<ExtractedCsvFixture>
{
    public const string Name = "Cli";
}
