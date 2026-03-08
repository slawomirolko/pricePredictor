﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using PricePredictor.Application.Data;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;

namespace PricePredictor.Application;

public interface INewsService
{
    Task<NewsServiceResult> DownloadAndStoreAsync(string url, CancellationToken cancellationToken);
}

public sealed class NewsService : INewsService
{
    private readonly IGoldNewsClient _client;
    private readonly IGoldNewsEmbeddingRepository _repository;
    private readonly IOllamaApiClient _ollama;
    private readonly GoldNewsSettings _settings;
    private readonly ILogger<NewsService> _logger;

    public NewsService(
        IGoldNewsClient client,
        IGoldNewsEmbeddingRepository repository,
        IOllamaApiClient ollama,
        IOptions<GoldNewsSettings> settings,
        ILogger<NewsService> logger)
    {
        _client = client;
        _repository = repository;
        _ollama = ollama;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<NewsServiceResult> DownloadAndStoreAsync(string url, CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(url, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Article already stored: {Url}", url);
            return NewsServiceResult.AlreadyStored();
        }

        var content = await _client.FetchArticleContentAsync(url, cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Failed to extract content from: {Url}", url);
            return NewsServiceResult.Failed("Failed to extract article content");
        }

        _logger.LogInformation("Extracted {Length} characters from article", content.Length);

        _ollama.SelectedModel = _settings.OllamaModel;
        var embeddingResponse = await _ollama.EmbedAsync(content, cancellationToken);

        if (embeddingResponse.Embeddings.Count == 0)
        {
            _logger.LogError("Failed to generate embedding for: {Url}", url);
            return NewsServiceResult.Failed("Failed to generate embedding", content.Length);
        }

        await _repository.UpsertAsync(
            url,
            content,
            embeddingResponse.Embeddings[0],
            _settings.EmbeddingDimensions,
            cancellationToken);

        _logger.LogInformation("Stored article: {Url}", url);

        return NewsServiceResult.Stored(content.Length);
    }
}

public sealed record NewsServiceResult(
    bool Success,
    string Message,
    bool WasAlreadyStored,
    int ContentLength)
{
    public static NewsServiceResult AlreadyStored() => new(
        true,
        "Article already exists in database",
        true,
        0);

    public static NewsServiceResult Failed(string message, int contentLength = 0) => new(
        false,
        message,
        false,
        contentLength);

    public static NewsServiceResult Stored(int contentLength) => new(
        true,
        "Article downloaded and stored successfully",
        false,
        contentLength);
}
