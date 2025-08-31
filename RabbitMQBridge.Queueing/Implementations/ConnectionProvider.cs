using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Queueing.Implementations;

internal sealed class ConnectionProvider : IAsyncDisposable, IConnectionProvider
{
    private readonly ILogger<ConnectionProvider> _logger;
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;

    public ConnectionProvider(ILogger<ConnectionProvider> logger, ConnectionFactory connectionFactory)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_connection != null && _connection.IsOpen)
            {
                _logger.LogDebug("Disposing the connection");
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error while disposing the connection");
        }
    }

    public async Task<IConnection> GetConnectionAsync()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            _logger.LogDebug("Creating a new connection");
            _connection = await _connectionFactory.CreateConnectionAsync();
        }

        return _connection;
    }
}