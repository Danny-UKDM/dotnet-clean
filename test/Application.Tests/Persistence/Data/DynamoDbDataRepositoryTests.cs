using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Application.Common;
using Application.Persistence.Data;
using Application.Persistence.Data.Entities;
using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Test.Common.Builders;

namespace Application.Tests.Persistence.Data;

public abstract class DynamoDbDataRepositoryTests
{
    public class Get
    {
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly DynamoDbDataRepository _repository;
        private const string TableName = "table-name";

        public Get()
        {
            _dynamoDbContext = Substitute.For<IDynamoDBContext>();

            var options = Substitute.For<IOptions<Config>>();
            options.Value.Returns(new Config { TableName = TableName });

            _repository = new DynamoDbDataRepository(_dynamoDbContext, NullLogger<DynamoDbDataRepository>.Instance, options);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task GetThrowsArgumentExceptionWhenHashKeyIsNullOrEmpty(string hashKey)
        {
            var action = async () => await _repository.Get<Person>(hashKey, "range-key");

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task GetThrowsArgumentExceptionWhenRangeKeyIsNullOrEmpty(string rangeKey)
        {
            var action = async () => await _repository.Get<Person>("hash-key", rangeKey);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetReturnsErrorResultWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            var result = await _repository.Get<Person>("hash-key", "range-key", cancellationToken: ctx.Token);

            result.Should().BeEquivalentTo(Result<Person>.Error("Cancellation token was requested"));
        }

        [Fact]
        public async Task GetInvokesNoServicesWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            await _repository.Get<Person>("hash-key", "range-key", cancellationToken: ctx.Token);

            await _dynamoDbContext
                .DidNotReceiveWithAnyArgs()
                .LoadAsync<Person>(default, default, default, default);

            _dynamoDbContext
                .DidNotReceiveWithAnyArgs()
                .QueryAsync<Person>(default, default, default);
        }

        [Fact]
        public async Task GetReturnsNotFoundResultWhenQueryResultIsEmpty()
        {
            var query = Substitute.For<AsyncSearch<Person>>();

            query
                .GetNextSetAsync()
                .ReturnsForAnyArgs([]);

            _dynamoDbContext
                .QueryAsync<Person>(default, default, default)
                .ReturnsForAnyArgs(query);

            var result = await _repository.Get<Person>("hash-key", "range-key", "index");

            result.Should().BeEquivalentTo(Result<Person>.NotFound());
        }

        [Fact]
        public async Task GetReturnsNotFoundResultWhenLoadResultIsNull()
        {
            _dynamoDbContext
                .LoadAsync<Person>(default, default, default, default)
                .ReturnsNullForAnyArgs();

            var result = await _repository.Get<Person>("hash-key", "range-key");

            result.Should().BeEquivalentTo(Result<Person>.NotFound());
        }

        [Fact]
        public async Task GetReturnsCriticalErrorResultWhenExceptionThrown()
        {
            _dynamoDbContext
                .LoadAsync<Person>(default, default, default, default)
                .ThrowsAsyncForAnyArgs(new ArgumentException("Some Exception"));

            var result = await _repository.Get<Person>("hash-key", "range-key");

            result.Should().BeEquivalentTo(Result<Person>.CriticalError($"{nameof(ArgumentException)}: Some Exception"));
        }

        [Fact]
        public async Task GetReturnsSuccessResultWithEntityWhenEntityFoundInMainIndex()
        {
            var person = PersonBuilder.Create().Build();

            _dynamoDbContext
                .LoadAsync<Person>
                (
                    Arg.Is(person.PK),
                    Arg.Is(person.SK),
                    Arg.Is<DynamoDBOperationConfig>(config =>
                        config.OverrideTableName == TableName &&
                        config.IndexName == null
                    ),
                    Arg.Any<CancellationToken>()
                )
                .Returns(person);

            var result = await _repository.Get<Person>(person.PK, person.SK);

            result.Should().BeEquivalentTo(Result<Person>.Success(person));
        }

        [Fact]
        public async Task GetReturnsSuccessResultWithEntityWhenEntityFoundInSecondaryIndex()
        {
            var person = PersonBuilder.Create().Build();
            const string indexName = "index";

            var query = Substitute.For<AsyncSearch<Person>>();

            query
                .GetNextSetAsync()
                .ReturnsForAnyArgs([person]);

            _dynamoDbContext
                .QueryAsync<Person>
                (
                    Arg.Is(person.PK),
                    Arg.Is(QueryOperator.Equal),
                    Arg.Any<IEnumerable<object>>(),
                    Arg.Is<DynamoDBOperationConfig>(config =>
                        config.OverrideTableName == TableName &&
                        config.IndexName == indexName
                    )
                )
                .Returns(query);

            var result = await _repository.Get<Person>(person.PK, person.SK, indexName);

            result.Should().BeEquivalentTo(Result<Person>.Success(person));
        }
    }

    public class GetMany
    {
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly DynamoDbDataRepository _repository;
        private const string TableName = "table-name";

        public GetMany()
        {
            _dynamoDbContext = Substitute.For<IDynamoDBContext>();

            var options = Substitute.For<IOptions<Config>>();
            options.Value.Returns(new Config { TableName = TableName });

            _repository = new DynamoDbDataRepository(_dynamoDbContext, NullLogger<DynamoDbDataRepository>.Instance, options);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task GetManyThrowsArgumentExceptionWhenHashKeyIsNullOrEmpty(string hashKey)
        {
            var action = async () => await _repository.GetMany<Person>(hashKey);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetManyReturnsErrorResultWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            var result = await _repository.GetMany<Person>("hash-key", cancellationToken: ctx.Token);

            result.Should().BeEquivalentTo(Result<Person[]>.Error("Cancellation token was requested"));
        }

        [Fact]
        public async Task GetManyInvokesNoServicesWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            await _repository.GetMany<Person>("hash-key", cancellationToken: ctx.Token);

            _dynamoDbContext
                .DidNotReceiveWithAnyArgs()
                .QueryAsync<Person>(default);

            _dynamoDbContext
                .DidNotReceiveWithAnyArgs()
                .QueryAsync<Person>(default, default, default);
        }

        [Fact]
        public async Task GetManyReturnsNotFoundResultWhenQueryResultAgainstMainIndexIsEmpty()
        {
            var query = Substitute.For<AsyncSearch<Person>>();

            query
                .GetNextSetAsync()
                .ReturnsForAnyArgs([]);

            query.IsDone.Returns(true);

            _dynamoDbContext
                .QueryAsync<Person>(default)
                .ReturnsForAnyArgs(query);

            var result = await _repository.GetMany<Person>("hash-key");

            result.Should().BeEquivalentTo(Result<Person[]>.NotFound());
        }

        [Fact]
        public async Task GetManyReturnsNotFoundResultWhenQueryResultAgainstSecondaryIndexIsEmpty()
        {
            var query = Substitute.For<AsyncSearch<Person>>();

            query
                .GetNextSetAsync()
                .ReturnsForAnyArgs([]);

            query.IsDone.Returns(true);

            _dynamoDbContext
                .QueryAsync<Person>(default, default, default)
                .ReturnsForAnyArgs(query);

            var result = await _repository.GetMany<Person>("hash-key", "index", ["range-key"], Operator.Equal);

            result.Should().BeEquivalentTo(Result<Person[]>.NotFound());
        }

        [Fact]
        public async Task GetManyReturnsCriticalErrorResultWhenExceptionThrown()
        {
            var query = Substitute.For<AsyncSearch<Person>>();

            query
                .GetNextSetAsync()
                .ReturnsForAnyArgs([]);

            query.IsDone.Returns(true);

            _dynamoDbContext
                .QueryAsync<Person>(default)
                .ThrowsForAnyArgs(new ArgumentException("Some Exception"));

            var result = await _repository.GetMany<Person>("hash-key");

            result.Should().BeEquivalentTo(Result<Person[]>.CriticalError($"{nameof(ArgumentException)}: Some Exception"));
        }

        [Fact]
        public async Task GetManyReturnsSuccessResultWithEntitiesWhenEntitiesFoundInMainIndex()
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            var carole = PersonBuilder.Create(PersonBuilder.Director.Carole).Build();
            var barbra = PersonBuilder.Create(PersonBuilder.Director.Barbra).Build();
            var bob = PersonBuilder.Create(PersonBuilder.Director.Bob).Build();

            const string hashKey = "hash-key";

            var query = Substitute.For<AsyncSearch<Person>>();

            query
                .GetNextSetAsync()
                .ReturnsForAnyArgs([carole, barbra, bob]);

            query.IsDone.Returns(true);

            _dynamoDbContext
                .QueryAsync<Person>
                (
                    Arg.Is(hashKey),
                    Arg.Is<DynamoDBOperationConfig>(config =>
                        config.OverrideTableName == TableName &&
                        config.IndexName == null
                    )
                )
                .Returns(query);

            var result = await _repository.GetMany<Person>(hashKey);

            result.Should().BeEquivalentTo(Result<Person[]>.Success([carole, barbra, bob]));
        }

        [Fact]
        public async Task GetManyReturnsSuccessResultWithEntitiesWhenEntitiesFoundInSecondaryIndex()
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            var carole = PersonBuilder.Create(PersonBuilder.Director.Carole).Build();
            var barbra = PersonBuilder.Create(PersonBuilder.Director.Barbra).Build();
            var bob = PersonBuilder.Create(PersonBuilder.Director.Bob).Build();

            const string hashKey = "hash-key";
            const string rangeKey = "range-key";
            const string index = "index";

            var query = Substitute.For<AsyncSearch<Person>>();

            query
                .GetNextSetAsync()
                .ReturnsForAnyArgs([carole, barbra, bob]);

            query.IsDone.Returns(true);

            _dynamoDbContext
                .QueryAsync<Person>
                (
                    Arg.Is(hashKey),
                    Arg.Is(QueryOperator.Equal),
                    Arg.Any<IEnumerable<object>>(),
                    Arg.Is<DynamoDBOperationConfig>(config =>
                        config.OverrideTableName == TableName &&
                        config.IndexName == index
                    )
                )
                .Returns(query);

            var result = await _repository.GetMany<Person>(hashKey, index, [rangeKey], Operator.Equal);

            result.Should().BeEquivalentTo(Result<Person[]>.Success([carole, barbra, bob]));
        }
    }

    public class Upsert
    {
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly DynamoDbDataRepository _repository;
        private const string TableName = "table-name";

        public Upsert()
        {
            _dynamoDbContext = Substitute.For<IDynamoDBContext>();

            var options = Substitute.For<IOptions<Config>>();
            options.Value.Returns(new Config { TableName = TableName });

            _repository = new DynamoDbDataRepository(_dynamoDbContext, NullLogger<DynamoDbDataRepository>.Instance, options);
        }

        [Fact]
        public async Task UpsertThrowsArgumentNullExceptionWhenEntityIsNull()
        {
            var action = async () => await _repository.Upsert<Person>(null);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task UpsertReturnsErrorResultWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            var result = await _repository.Upsert(new Person(), cancellationToken: ctx.Token);

            result.Should().BeEquivalentTo(Result.Error("Cancellation token was requested"));
        }

        [Fact]
        public async Task UpsertInvokesNoServicesWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            await _repository.Upsert(new Person(), cancellationToken: ctx.Token);

            await _dynamoDbContext
                .DidNotReceiveWithAnyArgs()
                .SaveAsync(default, default, CancellationToken.None);
        }

        [Fact]
        public async Task UpsertReturnsCriticalErrorResultWhenExceptionThrown()
        {
            _dynamoDbContext
                .SaveAsync<Person>(default, default, default)
                .ThrowsAsyncForAnyArgs(new ArgumentException("Some Exception"));

            var result = await _repository.Upsert(new Person());

            result.Should().BeEquivalentTo(Result.CriticalError($"{nameof(ArgumentException)}: Some Exception"));
        }

        [Fact]
        public async Task UpsertReturnsSuccessResultWhenEntitySuccessfullyUpserted()
        {
            var person = PersonBuilder.Create().Build();

            var result = await _repository.Upsert(person);

            result.Should().BeEquivalentTo(Result.Success());
        }
    }

    public class Delete
    {
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly DynamoDbDataRepository _repository;
        private const string TableName = "table-name";

        public Delete()
        {
            _dynamoDbContext = Substitute.For<IDynamoDBContext>();

            var options = Substitute.For<IOptions<Config>>();
            options.Value.Returns(new Config { TableName = TableName });

            _repository = new DynamoDbDataRepository(_dynamoDbContext, NullLogger<DynamoDbDataRepository>.Instance, options);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task DeleteThrowsArgumentExceptionWhenHashKeyIsNullOrEmpty(string hashKey)
        {
            var action = async () => await _repository.Delete<Person>(hashKey, "range-key");

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task DeleteThrowsArgumentExceptionWhenRangeKeyIsNullOrEmpty(string rangeKey)
        {
            var action = async () => await _repository.Delete<Person>("hash-key", rangeKey);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task DeleteReturnsErrorResultWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            var result = await _repository.Delete<Person>("hash-key", "range-key", cancellationToken: ctx.Token);

            result.Should().BeEquivalentTo(Result.Error("Cancellation token was requested"));
        }

        [Fact]
        public async Task DeleteInvokesNoServicesWhenCancellationRaised()
        {
            using var ctx = new CancellationTokenSource();
            await ctx.CancelAsync();

            await _repository.Delete<Person>("hash-key", "range-key", cancellationToken: ctx.Token);

            await _dynamoDbContext
                .DidNotReceiveWithAnyArgs()
                .DeleteAsync<Person>(default, default, default, default);
        }

        [Fact]
        public async Task DeleteReturnsCriticalErrorResultWhenExceptionThrown()
        {
            _dynamoDbContext
                .DeleteAsync<Person>(default, default, default, default)
                .ThrowsAsyncForAnyArgs(new ArgumentException("Some Exception"));

            var result = await _repository.Delete<Person>("hash-key", "range-key");

            result.Should().BeEquivalentTo(Result.CriticalError($"{nameof(ArgumentException)}: Some Exception"));
        }

        [Fact]
        public async Task DeleteReturnsSuccessResultWhenEntitySuccessfullyUpserted()
        {
            var result = await _repository.Delete<Person>("hash-key", "range-key");

            result.Should().BeEquivalentTo(Result.Success());
        }
    }
}
