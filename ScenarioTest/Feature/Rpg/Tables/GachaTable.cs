using System.Collections.Immutable;
using System.Globalization;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class GachaTable(ImmutableArray<GachaRecord> records)
    : StaticDataTable<GachaRecord>(records)
{
    protected override void Validate()
    {
        var total = Records.Sum(x => x.Rate);
        if (total != 100m)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "가챠 확률의 합은 100 이어야 하는데 {0} 이다.",
                total));
        }
    }
}
