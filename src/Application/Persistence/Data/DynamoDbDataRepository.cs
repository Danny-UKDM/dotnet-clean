using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Application.Common;
using Application.Persistence.Data.Traits;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Persistence.Data;

public sealed record DynamoDbDataRepository : IDataRepository
{
    private readonly IDynamoDBContext _dynamoDbContext;
    private readonly ILogger<DynamoDbDataRepository> _logger;
    private readonly string _tableName;

    private static readonly IDictionary<Operator, QueryOperator> Operators = new Dictionary<Operator, QueryOperator>
    {
        [Operator.Equal] = QueryOperator.Equal,
        [Operator.LessThanOrEqual] = QueryOperator.LessThanOrEqual,
        [Operator.LessThan] = QueryOperator.LessThan,
        [Operator.GreaterThanOrEqual] = QueryOperator.GreaterThanOrEqual,
        [Operator.GreaterThan] = QueryOperator.GreaterThan,
        [Operator.BeginsWith] = QueryOperator.BeginsWith,
        [Operator.Between] = QueryOperator.Between
    };

    public DynamoDbDataRepository(IDynamoDBContext dynamoDbContext, ILogger<DynamoDbDataRepository> logger, IOptions<Config> config)
    {
        _dynamoDbContext = dynamoDbContext;
        _logger = logger;

        var tableName = config.Value.TableName;
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName, nameof(tableName));

        _tableName = tableName;
    }

    public async Task<Result<T>> Get<T>(string hashKey, string rangeKey, string? indexName = null,
        Operator @operator = Operator.Equal, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashKey, nameof(hashKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(rangeKey, nameof(rangeKey));

        if (cancellationToken.IsCancellationRequested)
            return Result<T>.Error("Cancellation token was requested");

        var dynamoDbOperationConfig = new DynamoDBOperationConfig { IndexName = indexName, OverrideTableName = _tableName };

        try
        {
            if (indexName is not null)
            {
                var query = _dynamoDbContext.QueryAsync<T>(hashKey, Operators[@operator], [rangeKey], dynamoDbOperationConfig);
                var queryResult = (await query.GetNextSetAsync(cancellationToken)).FirstOrDefault();

                return queryResult is not null
                    ? Result<T>.Success(queryResult)
                    : Result<T>.NotFound();
            }

            var loadResult = await _dynamoDbContext.LoadAsync<T>(hashKey, rangeKey, dynamoDbOperationConfig, cancellationToken);

            return loadResult is not null
                ? Result<T>.Success(loadResult)
                : Result<T>.NotFound();
        }
        catch (AmazonDynamoDBException ex)
        {
            if (ex.Retryable is not null)
                _logger.LogRetryableException(ex, ex.GetType().Name, GetType().Name, ex.Message); // TODO : retry strategy
            else
                _logger.LogGenericException(ex, ex.GetType().Name, GetType().Name, ex.Message);

            return Result<T>.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogGenericException(ex, ex.GetType().Name, GetType().Name, ex.Message);
            return Result<T>.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<Result<T[]>> GetMany<T>(string hashKey, string? indexName = null, string[]? rangeValues = null,
        Operator? @operator = null, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashKey, nameof(hashKey));

        if (cancellationToken.IsCancellationRequested)
            return Result<T[]>.Error("Cancellation token was requested");

        var dynamoDbOperationConfig = new DynamoDBOperationConfig { IndexName = indexName, OverrideTableName = _tableName };

        try
        {
            var query = rangeValues is not null && @operator.HasValue
                ? _dynamoDbContext.QueryAsync<T>(hashKey, Operators[@operator.Value], [rangeValues], dynamoDbOperationConfig)
                : _dynamoDbContext.QueryAsync<T>(hashKey, dynamoDbOperationConfig);

            var results = new List<T>();

            do
                results.AddRange(await query.GetNextSetAsync(cancellationToken));
            while
                (!query.IsDone);

            return results.Count is not 0
                ? Result<T[]>.Success(results.ToArray())
                : Result<T[]>.NotFound();
        }
        catch (AmazonDynamoDBException ex)
        {
            LogDynamoException(ex);
            return Result<T[]>.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogGenericException(ex, ex.GetType().Name, GetType().Name, ex.Message);
            return Result<T[]>.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<Result> Upsert<T>(T entity, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        if (cancellationToken.IsCancellationRequested)
            return Result.Error("Cancellation token was requested");

        var dynamoDbOperationConfig = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        try
        {
            await _dynamoDbContext.SaveAsync(entity, dynamoDbOperationConfig, cancellationToken);
            return Result.Success();
        }
        catch (AmazonDynamoDBException ex)
        {
            LogDynamoException(ex);
            return Result.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogGenericException(ex, ex.GetType().Name, GetType().Name, ex.Message);
            return Result.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<Result> Delete<T>(string hashKey, string rangeKey, CancellationToken cancellationToken = default) where T : ICompositeKeyable, ITraceable
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashKey, nameof(hashKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(rangeKey, nameof(rangeKey));

        if (cancellationToken.IsCancellationRequested)
            return Result.Error("Cancellation token was requested");

        var dynamoDbOperationConfig = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        try
        {
            await _dynamoDbContext.DeleteAsync<T>(hashKey, rangeKey, dynamoDbOperationConfig, cancellationToken);
            return Result.Success();
        }
        catch (AmazonDynamoDBException ex)
        {
            LogDynamoException(ex);
            return Result.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogGenericException(ex, ex.GetType().Name, GetType().Name, ex.Message);
            return Result.CriticalError($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void LogDynamoException(AmazonDynamoDBException ex)
    {
        if (ex.Retryable is not null)
            _logger.LogRetryableException(ex, ex.GetType().Name, GetType().Name, ex.Message); // TODO : retry strategy
        else
            _logger.LogGenericException(ex, ex.GetType().Name, GetType().Name, ex.Message);
    }
}
