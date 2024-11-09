using Application.Persistence.Data.Entities;

namespace Test.Common.Builders;

public sealed record PersonBuilder
{
    private readonly Guid _entityId = Guid.NewGuid();
    private Guid _clientId = Guid.NewGuid();
    private readonly string _name;
    private readonly DateOnly _dateOfBirth;
    private readonly string _imageUrl;

    private PersonBuilder(Director director)
    {
        switch (director)
        {
            case Director.Barbra:
                _name = "Barbra Streisand";
                _dateOfBirth = new DateOnly(1942, 4, 24);
                _imageUrl = "https://cdn.com/barbra.png";
                break;
            case Director.Carole:
                _name = "Carole Baskin";
                _dateOfBirth = new DateOnly(1961, 6, 6);
                _imageUrl = "https://cdn.com/carole.png";
                break;
            case Director.Bob:
                _name = "Bob Ross";
                _dateOfBirth = new DateOnly(1942, 10, 29);
                _imageUrl = "https://cdn.com/bob.png";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(director), director, null);
        }
    }

    public static PersonBuilder Create(Director director = Director.Carole) => new(director);

    public PersonBuilder WithClientId(Guid clientId)
    {
        _clientId = clientId;
        return this;
    }

    public Person Build() => new(_entityId, _clientId, _name, _dateOfBirth, _imageUrl);

    public enum Director
    {
        Barbra,
        Carole,
        Bob
    }
}
