namespace YAYA_api;


public interface ISnapshot
{
    public ulong AggregateVersion { get; set; }
}

public abstract class AggregateSnapshot<T> : ISnapshot 
    where T : class 
{
    public ulong AggregateVersion { get; set; }
    public T Data { get; set; }

    protected AggregateSnapshot(ulong version, T data)
    {
        AggregateVersion = version;
        Data = data;
    }
}