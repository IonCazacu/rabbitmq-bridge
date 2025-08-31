using RabbitMQ.Client;

namespace RabbitMQBridge.Queueing.Interfaces;

internal interface IChannelProvider
{
    Task<IChannel> GetChannelAsync();
}