using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class TimetableTable(ImmutableArray<TimetableRecord> records)
    : StaticDataTable<TimetableRecord>(records)
{
    protected override void Validate()
    {
        var doubleBooked = Records
            .GroupBy(x => new TeacherSlot(x.TeacherId, x.Slot.Day, x.Slot.Period))
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (doubleBooked.Count > 0)
        {
            var first = doubleBooked[0];
            throw new InvalidOperationException(FormattableString.Invariant(
                $"교사 {first.TeacherId.Value} 은 {first.Day} {first.Period} 교시에 수업이 둘이다."));
        }
    }

    private sealed record TeacherSlot(TeacherRecord.TeacherId TeacherId, Weekday Day, byte Period);
}
