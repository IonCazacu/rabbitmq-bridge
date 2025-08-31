using RabbitMQ.Client;

namespace RabbitMQBridge.Queueing.Interfaces;

internal interface IConnectionProvider
{
    Task<IConnection> GetConnectionAsync();
}