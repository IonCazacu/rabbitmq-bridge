namespace RabbitMQBridge.Queueing.Interfaces;

/// <summary>
/// Defines a provider for a queue channel that handles messages of a specific type.
/// </summary>
/// <typeparam name="TQueueMessage">The type of queue message, which must implement the IQueueMessage interface.</typeparam>
internal interface IQueueChannelProvider<in TQueueMessage> : IChannelProvider where TQueueMessage : IQueueMessage
{
}