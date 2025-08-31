using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Models;

public class TestQueueMessage : IQueueMessage
{
    public Guid MessageId { get; set; }
    public TimeSpan TimeToLive { get; set; }
    public string Sentence { get; set; }
}