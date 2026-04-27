using System.Collections;

namespace Sdp.Table;

internal interface IStaticDataTable
{
    Type RecordType { get; }

    IEnumerable GetAllRecords();

    void Validate();
}
