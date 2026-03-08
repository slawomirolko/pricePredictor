namespace PricePredictor.Api.Finance;

public sealed class VolatilityCommodity
{
    public static readonly VolatilityCommodity Gold = new(1, "Gold");
    public static readonly VolatilityCommodity Silver = new(2, "Silver");
    public static readonly VolatilityCommodity NaturalGas = new(3, "NaturalGas");
    public static readonly VolatilityCommodity Oil = new(4, "Oil");

    private static readonly VolatilityCommodity[] All = { Gold, Silver, NaturalGas, Oil };

    public int Id { get; }
    public string Name { get; }

    private VolatilityCommodity(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public static VolatilityCommodity FromId(int id) =>
        All.FirstOrDefault(c => c.Id == id) 
            ?? throw new ArgumentException($"Unknown commodity ID: {id}", nameof(id));

    public static VolatilityCommodity FromName(string name) =>
        All.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal))
            ?? throw new ArgumentException($"Unknown commodity name: {name}", nameof(name));

    public override string ToString() => Name;

    public override bool Equals(object? obj) => obj is VolatilityCommodity other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}



