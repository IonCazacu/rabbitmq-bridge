namespace RabbitMQBridge.Queueing.Interfaces;

/// <summary>
/// Defines an interface for a consumer that processes a specific type of queue message.
/// </summary>
/// <typeparam name="TQueueMessage">The type of the message to be consumed, which must be a class and implement the IQueueMessage interface.</typeparam>
public interface IQueueConsumer<in TQueueMessage> where TQueueMessage : class, IQueueMessage
{
    /// <summary>
    /// Asynchronously processes the provided queue message.
    /// </summary>
    /// <param name="message">The message to be consumed.</param>
    /// <returns>A task that represents the asynchronous consume operation.</returns>
    Task ConsumeAsync(TQueueMessage message);
}
