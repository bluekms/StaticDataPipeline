using System.Globalization;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

// 추출기와 헤더 생성기가 Record 선언을 읽다가 멈출 때 내는 문구를 선언 결함마다 확인한다.
public class DeclarationDefectMessageTests(ITestOutputHelper testOutputHelper)
{
    public static TheoryData<string, string, string> Defects()
    {
        var data = new TheoryData<string, string, string>();

        // language=C#
        const string twoKeys =
            """
            using Sdp.Attributes;

            // [Key] 는 하나. 복합 키는 중첩 record 로 표현한다.
            [StaticDataRecord("Invalid", "Students")]
            public sealed partial record StudentRecord(
                [Key] int Id,
                [Key] string Name,
                int ClassRoomId);
            """;

        // language=C#
        const string lengthWithSingleColumn =
            """
            using System.Collections.Immutable;
            using Sdp.Attributes;

            // [Length] 는 열을 펼치고 [SingleColumnCollection] 은 한 셀에 담으므로 함께 쓸 수 없다.
            [StaticDataRecord("Invalid", "Cafeteria")]
            public sealed partial record CafeteriaRecord(
                [Key] int Id,
                [Length(4)] [SingleColumnCollection(",")] ImmutableArray<string> Menu);
            """;

        // language=C#
        const string innerNullableBranding =
            """
            using Sdp.Attributes;

            public record struct NpcId([NullString("")] int? Value);

            // 값 하나를 감싼 record 의 안쪽 값은 nullable 일 수 없다. 값이 없을 수 있는 자리는 NpcId? 와 [NullString] 으로 적는다.
            [StaticDataRecord("Invalid", "Notices")]
            public sealed partial record NoticeRecord(
                [Key] int Id,
                string Title,
                NpcId Sender);
            """;

        // language=C#
        const string columnNameForbiddenChar =
            """
            using Sdp.Attributes;

            // [ColumnName] 의 쉼표는 CSV 헤더 구분자와 겹친다.
            [StaticDataRecord("Invalid", "Teachers")]
            public sealed partial record TeacherRecord(
                [Key] int Id,
                [ColumnName("a,b")] string Name);
            """;

        // language=C#
        const string mapValueWithoutKey =
            """
            using System.Collections.Frozen;
            using Sdp.Attributes;

            public enum SubjectCode
            {
                Korean,
                Math,
            }

            public sealed record SubjectScore(SubjectCode Subject, int Score);

            // Map 의 Value record 에 [Key] 가 없으면 FrozenDictionary 의 키를 정할 수 없다.
            [StaticDataRecord("Invalid", "Grades")]
            public sealed partial record GradeRecord(
                [Key] int StudentId,
                [Length(2)] FrozenDictionary<SubjectCode, SubjectScore> Scores);
            """;

        // language=C#
        const string circularRecord =
            """
            using Sdp.Attributes;

            public sealed record Mentor(int TeacherId, Mentee Mentee);

            public sealed record Mentee(int StudentId, Mentor Mentor);

            // 중첩 record 의 순환 참조. 열을 펼치는 일이 끝나지 않으므로 선언 단계에서 막힌다.
            [StaticDataRecord("Invalid", "Mentoring")]
            public sealed partial record MentoringRecord(
                [Key] int Id,
                Mentor Mentor);
            """;

        // language=C#
        const string fkAndSwitchFkTogether =
            """
            using Sdp.Attributes;

            public enum RewardKind
            {
                Item,
                Currency,
            }

            // 한 열의 대상 테이블을 고정([ForeignKey])과 조건부([SwitchForeignKey])로 동시에 정할 수 없다.
            [StaticDataRecord("Invalid", "Rewards")]
            public sealed partial record RewardRecord(
                [Key] int Id,
                RewardKind Kind,
                [ForeignKey("ItemTable", "Id")]
                [SwitchForeignKey(nameof(Kind), "Currency", "CurrencyTable", "Id")]
                int TargetId);
            """;

        // language=C#
        const string guidWithRange =
            """
            using Sdp.Attributes;

            // Guid 는 대소 비교가 없어 [Range] 를 붙일 수 없다.
            [StaticDataRecord("Invalid", "Receipts")]
            public sealed partial record ReceiptRecord(
                [Key] int Id,
                [Range(typeof(Guid), "00000000-0000-0000-0000-000000000000", "ffffffff-ffff-ffff-ffff-ffffffffffff")] Guid Token);
            """;

        // language=C#
        const string guidWithDateTimeFormat =
            """
            using Sdp.Attributes;

            // [DateTimeFormat] 은 날짜·시간 타입에만 붙는다.
            [StaticDataRecord("Invalid", "Tokens")]
            public sealed partial record TokenRecord(
                [Key] int Id,
                [DateTimeFormat("yyyy-MM-dd")] Guid Token);
            """;

        // language=C#
        const string dateOnlyWithoutFormat =
            """
            using Sdp.Attributes;

            // DateOnly 는 [DateTimeFormat] 없이는 셀 문자열을 읽을 형식을 정할 수 없다.
            [StaticDataRecord("Invalid", "Holidays")]
            public sealed partial record HolidayRecord(
                [Key] int Id,
                DateOnly Day);
            """;

        // language=C#
        const string nullableWithoutNullString =
            """
            using Sdp.Attributes;

            // nullable 파라미터는 [NullString] 으로 어떤 셀이 null 인지 정해야 한다.
            [StaticDataRecord("Invalid", "Students")]
            public sealed partial record StudentRecord(
                [Key] int Id,
                string Name,
                string? Nickname);
            """;

        // language=C#
        const string undeclaredType =
            """
            using Sdp.Attributes;

            // Visibility 는 Record 소스 폴더에도 BCL 에도 없는 타입이다.
            [StaticDataRecord("Invalid", "Clubs")]
            public sealed partial record ClubRecord(
                [Key] int Id,
                string Name,
                Visibility Visibility);
            """;

        // language=C#
        const string bclEnumWithoutUsing =
            """
            using System.Collections.Immutable;
            using Sdp.Attributes;

            // DayOfWeek 는 BCL 에 있지만 이 파일에 using System 이 없다. 프로젝트의 ImplicitUsings 덕에 빌드는 되어도 추출기는 이 타입을 찾지 못한다.
            [StaticDataRecord("Invalid", "Clubs")]
            public sealed partial record ClubMeetingRecord(
                [Key] int Id,
                [SingleColumnCollection(",")] ImmutableArray<DayOfWeek> MeetingDays);
            """;

        data.Add(nameof(twoKeys), twoKeys, "must have at most one [Key] member");
        data.Add(nameof(lengthWithSingleColumn), lengthWithSingleColumn, "LengthAttribute and SingleColumnCollectionAttribute cannot be used together");
        data.Add(nameof(innerNullableBranding), innerNullableBranding, "A single-value record must wrap a non-nullable value");
        data.Add(nameof(columnNameForbiddenChar), columnNameForbiddenChar, "ColumnName \"a,b\" contains a forbidden character");
        data.Add(nameof(mapValueWithoutKey), mapValueWithoutKey, "KeyAttribute must be used in one of the parameters");
        data.Add(nameof(circularRecord), circularRecord, "Circular reference detected");
        data.Add(nameof(fkAndSwitchFkTogether), fkAndSwitchFkTogether, "cannot be used together on the same parameter");
        data.Add(nameof(guidWithRange), guidWithRange, "RangeAttribute cannot be used");
        data.Add(nameof(guidWithDateTimeFormat), guidWithDateTimeFormat, "DateTimeFormatAttribute cannot be used");
        data.Add(nameof(dateOnlyWithoutFormat), dateOnlyWithoutFormat, "must use DateTimeFormatAttribute");
        data.Add(nameof(nullableWithoutNullString), nullableWithoutNullString, "nullable type must use NullStringAttribute");
        data.Add(nameof(undeclaredType), undeclaredType, "is not found in the record source folder or in the runtime libraries");
        data.Add(nameof(bclEnumWithoutUsing), bclEnumWithoutUsing, "The type 'DayOfWeek'");
        data.Add(nameof(bclEnumWithoutUsing), bclEnumWithoutUsing, "the project's implicit usings are not applied");

        return data;
    }

    [Theory]
    [MemberData(nameof(Defects))]
    public void DefectiveDeclaration_IsRejectedWithItsMessage(string defect, string code, string expectedMessage)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<DeclarationDefectMessageTests>() is not TestOutputLogger<DeclarationDefectMessageTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            var exception = Record.Exception(() => SimpleCordParser.Parse(code, logger));

            Assert.NotNull(exception);
            var reported = exception.Message + Environment.NewLine + string.Join(Environment.NewLine, logger.Logs.Select(x => x.Message));
            Assert.True(
                reported.Contains(expectedMessage, StringComparison.Ordinal),
                defect + " 의 문구에 '" + expectedMessage + "' 가 없다: " + reported);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }
}
