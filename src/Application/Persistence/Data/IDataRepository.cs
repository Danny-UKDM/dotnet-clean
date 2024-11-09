using Application.Persistence.Data.Traits;
using Ardalis.Result;

namespace Application.Persistence.Data;

public interface IDataRepository
{
    Task<Result<T>> Get<T>(string hashKey, string rangeKey, string? indexName = null,
        Operator @operator = Operator.Equal, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable;

    Task<Result<T[]>> GetMany<T>(string hashKey, string? indexName = null, string[]? rangeValues = null,
        Operator? @operator = null, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable;

    Task<Result> Upsert<T>(T entity, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable;

    Task<Result> Delete<T>(string hashKey, string rangeKey, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable;
}
