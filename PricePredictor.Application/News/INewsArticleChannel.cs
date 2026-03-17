using System.Threading.Channels;

namespace PricePredictor.Application.News;

public interface INewsArticleChannel
{
    void Publish(NewsItem item);
    IAsyncEnumerable<NewsItem> ReadAllAsync(CancellationToken cancellationToken);
}

public sealed class NewsArticleChannel : INewsArticleChannel
{
    private readonly Channel<NewsItem> _channel =
        Channel.CreateUnbounded<NewsItem>(new UnboundedChannelOptions { SingleWriter = false, AllowSynchronousContinuations = false });

    public void Publish(NewsItem item) => _channel.Writer.TryWrite(item);

    public IAsyncEnumerable<NewsItem> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

