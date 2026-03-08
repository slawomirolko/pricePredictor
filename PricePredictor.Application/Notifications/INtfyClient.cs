namespace PricePredictor.Application.Notifications;

/// <summary>
/// Abstraction for sending notifications via Ntfy service
/// </summary>
public interface INtfyClient
{
    /// <summary>
    /// Send a message to a specific topic
    /// </summary>
    Task SendAsync(string topic, string message, CancellationToken cancellationToken = default);
}

