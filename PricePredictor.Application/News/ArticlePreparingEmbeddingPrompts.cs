namespace PricePredictor.Application.News;

public static class ArticlePreparingEmbeddingPrompts
{
    public static string BuildEmbeddingPrompt(string inputText)
    {
        return $"""
                Your task is to transform the input text into a semantically rich, normalized representation optimized for vector embeddings.

                Instructions:
                1. Preserve the core meaning and important context.
                2. Expand abbreviations and implicit references when possible.
                3. Remove filler words and repetition.
                4. Include key entities, concepts, and relationships.
                5. Use clear and explicit wording.

                Text:
                \"\"\"
                {inputText}
                \"\"\"
                """;
    }
}