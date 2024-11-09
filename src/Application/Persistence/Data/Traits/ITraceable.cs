namespace Application.Persistence.Data.Traits;

public interface ITraceable
{
    public Guid EntityId { get; init; }
    public Guid ClientId { get; init; }
}
