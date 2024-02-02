namespace YAYA_api;


public interface IProjection
{
    void When(object @event);
}

public interface IVersionedProjection: IProjection
{
    public ulong LastProcessedPosition { get; set; }
}
