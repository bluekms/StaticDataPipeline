using System.Collections.Immutable;
using System.Globalization;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class CalendarTable(ImmutableArray<CalendarRecord> records)
    : StaticDataTable<CalendarRecord>(records)
{
    protected override void Validate()
    {
        foreach (var record in Records)
        {
            if (record.End is null)
            {
                continue;
            }

            if (record.End.Value < record.Start)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "일정 {0} 의 종료일({1:yyyy-MM-dd})이 시작일({2:yyyy-MM-dd})보다 앞선다.",
                    record.Id,
                    record.End.Value,
                    record.Start));
            }
        }
    }
}
