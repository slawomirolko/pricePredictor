namespace PricePredictor.Application.Models;

public class Commodity
{
    private Commodity()
    {
    }

    private Commodity(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public List<VolatilityDaily> DailyVolatilities { get; private set; } = new();

    public static Commodity Create(string name, int? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Commodity name cannot be empty.", nameof(name));
        }

        var resolvedId = id ?? 0;
        return resolvedId < 0 ? throw new ArgumentException("Commodity id cannot be negative.", nameof(id)) : new Commodity(resolvedId, name);
    }
}
