namespace RabbitMQBridge.Queueing.Interfaces;

internal interface IQueueConsumerHandler<TMessageConsumer, TQueueMessage>
    where TMessageConsumer : IQueueConsumer<TQueueMessage> where TQueueMessage : class, IQueueMessage
{
    Task RegisterQueueConsumerAsync();
    Task CancelQueueConsumerAsync();
}