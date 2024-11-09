using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Application.Common;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Kernel
{
    public static IServiceCollection ConfigureKernel(this IServiceCollection services, IConfiguration config) =>
        services
            .Configure<Config>(config.GetSection("Application"))
            .ConfigureMediator()
            .ConfigureAws(config)
            .ConfigurePersistence();

    private static IServiceCollection ConfigureMediator(this IServiceCollection services) =>
        services
            .AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(Kernel).Assembly))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    private static IServiceCollection ConfigureAws(this IServiceCollection services, IConfiguration config) =>
        services
            .AddDefaultAWSOptions(config.GetAWSOptions());

    private static IServiceCollection ConfigurePersistence(this IServiceCollection services) =>
        services
            .AddAWSService<IAmazonDynamoDB>()
            .AddScoped<IDynamoDBContext, DynamoDBContext>(provider =>
                new DynamoDBContext
                (
                    provider.GetRequiredService<IAmazonDynamoDB>(),
                    new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2, RetrieveDateTimeInUtc = true }
                )
            );
}
