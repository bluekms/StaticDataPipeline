using System.Collections.Immutable;
using System.Globalization;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class ShopTable(ImmutableArray<ShopRecord> records)
    : StaticDataTable<ShopRecord>(records)
{
    protected override void Validate()
    {
        foreach (var record in Records)
        {
            var hasLimit = record.LimitKind != LimitKind.None;
            var hasCount = record.LimitCount > 0;
            if (hasLimit != hasCount)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "상점 상품 {0} 의 구매 제한 {1} 과 제한 수 {2} 가 맞지 않는다. None 이면 0, 그 밖에는 1 이상이어야 한다.",
                    record.Id.Value,
                    record.LimitKind,
                    record.LimitCount));
            }
        }
    }
}
