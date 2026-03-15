using OllamaSharp.Models;
using PricePredictor.Application;
using PricePredictor.Application.News;

namespace PricePredictor.Infrastructure.GoldNews;

public sealed class OllamaGenerateRequestBuilder
{
    private const int DefaultPromptLimit = 15000;

    private string _model = string.Empty;
    private string _systemPrompt = string.Empty;
    private string _userPrompt = string.Empty;
    private int _promptLimit = DefaultPromptLimit;

    public OllamaGenerateRequestBuilder WithModel(string model)
    {
        _model = model;
        return this;
    }

    public OllamaGenerateRequestBuilder WithSystemPrompt(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        return this;
    }

    public OllamaGenerateRequestBuilder WithUserHtmlContent(string htmlContent)
    {
        _userPrompt = $"{PromptHelper.HtmlContentLabelPrefix}{htmlContent}";
        return this;
    }

    public OllamaGenerateRequestBuilder WithPromptLimit(int promptLimit)
    {
        _promptLimit = promptLimit;
        return this;
    }

    public GenerateRequest Build()
    {
        var normalizedUserPrompt = _userPrompt.Length > _promptLimit
            ? _userPrompt[.._promptLimit]
            : _userPrompt;

        return new GenerateRequest
        {
            Model = _model,
            System = _systemPrompt,
            Prompt = normalizedUserPrompt
        };
    }
}
