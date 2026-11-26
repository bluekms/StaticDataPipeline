using Microsoft.Extensions.Logging;
using ScenarioTest.Tables;
using Sdp.Manager;

namespace ScenarioTest.Managers;

public sealed partial class ScienceStaticData(ILogger logger)
    : StaticDataManager<ScienceStaticData.TableSet>(logger)
{
    public sealed partial record TableSet(ElementTable? ElementTable);
}
