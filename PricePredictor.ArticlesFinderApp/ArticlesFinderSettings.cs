namespace PricePredictor.ArticlesFinderApp;

public sealed record ArticlesFinderSettings
{
    public const string SectionName = "ArticlesFinder";

    public int SyncIntervalSecondsMin { get; init; } = 60;
    public int SyncIntervalSecondsMax { get; init; } = 150;
}
