namespace PricePredictor.Persistence;

public sealed record PersistenceDefaultConnectionSettings
{
    public const string SectionName = "PersistenceDefaultConnection";

    public string ConnectionString { get; init; } = string.Empty;
}

