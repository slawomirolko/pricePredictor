namespace PricePredicator.App.Finance;

public record YahooFinanceSettings
{
    public const string SectionName = "YahooFinance";

    /// <summary>
    /// Symbols to fetch: GLD, SLV, NG=F, CL=F
    /// </summary>
    public string[] Symbols { get; init; } = new[] { "GLD", "SLV", "NG=F", "CL=F" };

    /// <summary>
    /// Interval for data: 1m for 1-minute
    /// </summary>
    public string Interval { get; init; } = "1m";

    /// <summary>
    /// Range for data: 1d for last day, 5d, 1mo, etc.
    /// </summary>
    public string Range { get; init; } = "1d";

    /// <summary>
    /// Every N minutes, log all data from last 10 minutes
    /// </summary>
    public int VolatilityBackupMinutes { get; init; } = 10;

    /// <summary>
    /// Windows for rolling volatility calculation (in minutes)
    /// </summary>
    public int[] VolatilityWindows { get; init; } = new[] { 5, 15, 60 };
}

/// <summary>
/// Mapping of Yahoo ticker symbols to commodity names for table naming
/// </summary>
public static class SymbolMapper
{
    private static readonly Dictionary<string, string> SymbolToTableName = new()
    {
        { "GLD", "Gold" },
        { "SLV", "Silver" },
        { "NG=F", "NaturalGas" },
        { "CL=F", "Oil" }
    };

    public static string GetTableName(string symbol) =>
        SymbolToTableName.TryGetValue(symbol, out var name) ? name : symbol;

    public static string GetFullName(string symbol) =>
        symbol switch
        {
            "GLD" => "Gold",
            "SLV" => "Silver",
            "NG=F" => "Natural Gas",
            "CL=F" => "Oil",
            _ => symbol
        };
}

