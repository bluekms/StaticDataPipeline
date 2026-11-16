using System.Collections.Immutable;
using ScenarioTest.Records.Rpg;
using Sdp.Table;

namespace ScenarioTest.Tables;

public sealed partial class SkillTable(ImmutableArray<SkillRecord> records)
    : StaticDataTable<SkillRecord>(records);
