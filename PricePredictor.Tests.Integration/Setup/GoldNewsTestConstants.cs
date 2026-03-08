namespace PricePredictor.Tests.Integration.Setup;

internal static class GoldNewsTestConstants
{
    public const string ReutersUrl = "https://www.reuters.com/world/china/gold-hits-record-high-us-china-trade-woes-escalate-silver-scales-all-time-peak-2025-10-13/";

    public const string ExpectedSentence =
        "Gold could easily continue its upward momentum. We could see prices north of $5,000 by the end of 2026,\" said Phillip Streible, chief market strategist at Blue Line Futures. Steady central bank purchases, firm ETF inflows, U.S.-China trade tensions and the prospect of lower U.S. interest rates are providing structural support for the market, Streible added.";

    public static string ArticleContent =>
        "Gold market update: " + ExpectedSentence + " Further context and analysis follow.";
}

