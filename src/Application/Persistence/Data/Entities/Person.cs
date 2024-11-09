using Application.Persistence.Data.Traits;

namespace Application.Persistence.Data.Entities;

public record Person : ICompositeKeyable, ITraceable
{
    public const string HashPrefix = "PERSON";
    public const string RangePrefix = "CLIENT";

    public const string SecondaryHashPrefix = "CLIENT";
    public const string SecondaryRangePrefix = "DOB";

    public string PK { get; init; }
    public string SK { get; init; }
    public string GSI1PK { get; init; }
    public string GSI1SK { get; init; }

    public Guid EntityId { get; init; }
    public Guid ClientId { get; init; }
    public string Name { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string ImageUrl { get; init; }

    public Person(Guid entityId, Guid clientId, string name, DateOnly dateOfBirth, string imageUrl)
    {
        EntityId = entityId;
        ClientId = clientId;
        Name = name;
        DateOfBirth = dateOfBirth;
        ImageUrl = imageUrl;

        PK = $"{HashPrefix}#{EntityId:D}";
        SK = $"{RangePrefix}#{ClientId:D}";
        GSI1PK = $"{SecondaryHashPrefix}#{ClientId:D}";
        GSI1SK = $"{SecondaryRangePrefix}#{dateOfBirth:O}";
    }

    public Person() : this(Guid.Empty, Guid.Empty, string.Empty, DateOnly.MinValue, string.Empty)
    {
    }
}
