using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common;

public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly ILogger<TRequest> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = request.GetType().Name;
        var requestIdentifier = Guid.NewGuid().ToString();

        var identifiedRequest = $"{requestName} [{requestIdentifier}]";

        LogStartRequest(identifiedRequest);

        var stopwatch = Stopwatch.StartNew();

        TResponse response;
        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            _logger.LogGenericException(ex, ex.GetType().Name, GetType().Name, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogEndRequest(identifiedRequest, stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
        }

        return response;
    }

    [LoggerMessage(LogLevel.Information, "[START] {Request}")]
    partial void LogStartRequest(string request);

    [LoggerMessage(LogLevel.Information, "[END] {Request}; Execution time: {ExecutionTime}ms")]
    partial void LogEndRequest(string request, long executionTime);
}
