namespace Sdp.View;

public abstract class StaticDataView<TTableSet>(TTableSet tables)
    where TTableSet : class
{
    protected TTableSet Tables => tables;

    protected virtual void Validate()
    {
    }
}
