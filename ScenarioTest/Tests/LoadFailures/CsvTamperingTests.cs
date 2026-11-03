using Microsoft.Extensions.Logging.Abstractions;
using ScenarioTest.Managers;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.LoadFailures;

[Collection(CliCollectionDefinition.Name)]
public class CsvTamperingTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public async Task GradesCsvWithSixthScoreColumns_FailsAsExceedingDeclaredLength()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.File("School.Grades.csv"), x =>
        {
            x.AddColumn("Scores[5].Subject", "Music");
            x.AddColumn("Scores[5].Score", "90");
            x.AddColumn("Scores[5].Rank", "A");
        });
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Path), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "exceeds declared length 5");
    }

    [Fact]
    public async Task StudentsCsvWithoutPhoneColumn_FailsWithMissingHeader()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.File("School.Students.csv"), x => x.RemoveColumn("Phone"));
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Path), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "Header 'Phone' not found in CSV.");
    }

    [Fact]
    public async Task ClubsCsvMissing_FailsEvenThoughClubTableIsNullable()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        MoveAway(csv, "School.Clubs.csv");
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Path), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "School.Clubs.csv");
    }

    [Fact]
    public async Task ItemsCsvMissing_FailsTableLoad()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        MoveAway(csv, "Rpg.Items.csv");
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Path), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "Rpg.Items.csv");
    }

    [Fact]
    public async Task MonsterSkillsCsvHandEditedWithUnnamedSkillNumber_FailsEnumParse()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.File("Rpg.Monsters.csv"), x => x.Set("Id", "BossDragon", "Skills", "Fireball,-,1003"));
        var rpg = new RpgStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => rpg.LoadAsync(csv.Path), LoadAssert.TablesFailed);

        LoadAssert.ContainsAll(exception, "'1003'", "SkillId");
    }

    [Fact]
    public async Task StudentsCsvEmptied_FailsGradeForeignKeysAgainstEmptyTable()
    {
        using var culture = CultureScope.Invariant();
        using var csv = fixture.CopyMainCsv();
        CsvDocument.Edit(csv.File("School.Students.csv"), x => x.Rows.Clear());
        var school = new SchoolStaticData(NullLogger.Instance);

        var exception = await LoadAssert.FailsWithAggregate(() => school.LoadAsync(csv.Path), LoadAssert.FkFailed);

        LoadAssert.ContainsAll(exception, "GradeRecord", "StudentTable.Id");
    }

    private static void MoveAway(TempDirectory csv, string fileName)
    {
        var parking = Directory.CreateDirectory(Path.Combine(csv.Path, "parking"));
        File.Move(csv.File(fileName), Path.Combine(parking.FullName, fileName));
    }
}
