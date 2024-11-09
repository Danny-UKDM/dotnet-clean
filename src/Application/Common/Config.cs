using Microsoft.Extensions.Options;

namespace Application.Common;

public sealed record Config : IOptions<Config>
{
    public required string TableName { get; init; }

    public Config Value => this;
}
