namespace Sdp.View;

public abstract class StaticDataView<TSelf, TTableSet>(TTableSet tables) : IStaticDataView
    where TSelf : StaticDataView<TSelf, TTableSet>
    where TTableSet : class
{
    protected TTableSet Tables => tables;
}
