using System.Collections.Immutable;
using ScenarioTest.Records.School;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class BookTable(ImmutableArray<BookRecord> records)
    : StaticDataTable<BookRecord>(records);
