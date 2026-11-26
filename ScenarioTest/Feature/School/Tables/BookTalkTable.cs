using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class BookTalkTable(ImmutableArray<BookTalkRecord> records)
    : StaticDataTable<BookTalkRecord>(records);
