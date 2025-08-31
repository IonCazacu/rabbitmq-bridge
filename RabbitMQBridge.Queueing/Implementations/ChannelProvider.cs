using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Queueing.Implementations;

internal sealed class ChannelProvider : IAsyncDisposable, IChannelProvider
{
    private readonly ILogger<ChannelProvider> _logger;
    private readonly IConnectionProvider _connectionProvider;
    private IChannel? _channel;

    public ChannelProvider(ILogger<ChannelProvider> logger, IConnectionProvider  connectionProvider)
    {
        _logger = logger;
        _connectionProvider = connectionProvider;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel != null && _channel.IsOpen)
            {
                _logger.LogDebug("Disposing the channel");
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
                _channel = null;
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error while disposing the channel");
        }
    }

    public async Task<IChannel> GetChannelAsync()
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogDebug("Creating a new channel");
            var connection = await _connectionProvider.GetConnectionAsync();
            _channel = await connection.CreateChannelAsync();
        }

        return _channel;
    }
}