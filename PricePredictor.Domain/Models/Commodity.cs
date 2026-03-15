namespace PricePredictor.Domain.Models;

public class Commodity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<VolatilityDaily> DailyVolatilities { get; set; } = new();
}

