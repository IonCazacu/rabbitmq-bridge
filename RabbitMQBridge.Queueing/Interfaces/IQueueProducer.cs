namespace RabbitMQBridge.Queueing.Interfaces;

public interface IQueueProducer<in TQueueMessage> where TQueueMessage : IQueueMessage
{
    Task PublishMessageAsync(TQueueMessage message);
}