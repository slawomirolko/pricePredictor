using System.Text.RegularExpressions;

namespace PricePredictor.Application;

public static class PromptHelper
{
    public const string ContentOfHtmlPrefix = "Content of HTML is: ";
    public const string HtmlContentLabelPrefix = "HTML_CONTENT:\n";

    public const string TradingAssessmentSystemPrompt = @"You are a commodities trading impact classifier.
Decide if a news article is likely useful for short-term trading decisions because it may move prices of OIL, NATURAL GAS, GOLD, or SILVER.

Return ONLY strict JSON:
{""isTradeUseful"":true|false,""reason"":""max 30 words""}

Rules:
- true only if the content strongly suggests macro, geopolitical, supply/demand, policy, sanctions, conflict, inventory, production, central bank, inflation, or major risk events.
- false for generic market noise, lifestyle, opinion-only, or unrelated topics.
- If uncertain, return false.";

    public const string SummarizeSystemPrompt = @"You are a concise trading news summarizer.
Summarize the article in 500 characters or fewer. Focus only on information relevant to commodities trading (gold, silver, oil, natural gas).
Return ONLY the summary text.";

    public const string EmbeddingPreparationPrompt = @"Your task is to transform the input text into a semantically rich, normalized representation optimized for vector embeddings.
Instructions:
1. Preserve the core meaning and important context.
2. Expand abbreviations and implicit references when possible.
3. Remove filler words and repetition.
4. Include key entities, concepts, and relationships.
5. Use clear and explicit wording.";

    public static string BuildArticleExtractionSystemPrompt(string inputText)
    {
        return EmbeddingPreparationPrompt
            + "\n\nText:\n"
            + inputText;
    }

    public static string NormalizeForEmbedding(string text)
    {
        text = text.Replace("\n", " ");
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"[*_#>`]", "");
        text = text.Trim();
        return text;
    }
}
