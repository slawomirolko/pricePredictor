using System.Text.Json.Serialization;

namespace PricePredicator.App.Finance;

/// <summary>
/// Models for parsing Yahoo Finance JSON response
/// Example: https://query1.finance.yahoo.com/v8/finance/chart/GLD?interval=1m&range=1d
/// </summary>

public class YahooFinanceResponse
{
    [JsonPropertyName("chart")]
    public ChartData? Chart { get; set; }
}

public class ChartData
{
    [JsonPropertyName("result")]
    public ChartResult[]? Result { get; set; }

    [JsonPropertyName("error")]
    public ErrorData? Error { get; set; }
}

public class ChartResult
{
    [JsonPropertyName("meta")]
    public MetaData? Meta { get; set; }

    [JsonPropertyName("timestamp")]
    public long[]? Timestamp { get; set; }

    [JsonPropertyName("indicators")]
    public IndicatorsData? Indicators { get; set; }
}

public class MetaData
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("regularMarketPrice")]
    public decimal RegularMarketPrice { get; set; }
}

public class IndicatorsData
{
    [JsonPropertyName("quote")]
    public QuoteData[]? Quote { get; set; }
}

public class QuoteData
{
    [JsonPropertyName("open")]
    public decimal?[]? Open { get; set; }

    [JsonPropertyName("high")]
    public decimal?[]? High { get; set; }

    [JsonPropertyName("low")]
    public decimal?[]? Low { get; set; }

    [JsonPropertyName("close")]
    public decimal?[]? Close { get; set; }

    [JsonPropertyName("volume")]
    public long?[]? Volume { get; set; }
}

public class ErrorData
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class CandlePoint
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long? Volume { get; set; }
}

