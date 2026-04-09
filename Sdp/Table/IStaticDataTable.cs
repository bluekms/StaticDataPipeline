using System.Collections;

namespace Sdp.Table;

internal interface IStaticDataTable
{
    Type RecordType { get; }
    string? PrimaryKeyPropertyName { get; }
    IEnumerable GetAllRecords();
    bool ContainsPrimaryKey(object? value);
}
