using RabbitMQBridge.Models;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Consumers;

public class TestQueueMessageConsumer : IQueueConsumer<TestQueueMessage>
{
    private readonly ILogger<TestQueueMessageConsumer> _logger;

    public TestQueueMessageConsumer(ILogger<TestQueueMessageConsumer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ConsumeAsync(TestQueueMessage message)
    {
        _logger.LogInformation($"Message: {message.Sentence}");
        return Task.CompletedTask;
    }
}