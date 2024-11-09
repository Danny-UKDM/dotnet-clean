// ReSharper disable InconsistentNaming

namespace Application.Persistence.Data.Traits;

public interface ICompositeKeyable
{
    public string PK { get; init; }
    public string SK { get; init; }
    public string GSI1PK { get; init; }
    public string GSI1SK { get; init; }
}
