using RabbitMQ.Client;
using RabbitMQBridge.Queueing.Constants;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Queueing.Implementations;

internal class QueueChannelProvider<TQueueMessage> : IQueueChannelProvider<TQueueMessage>
    where TQueueMessage : IQueueMessage
{
    private readonly IChannelProvider _channelProvider;
    private IChannel _channel;
    private readonly string _queueName;

    public QueueChannelProvider(IChannelProvider channelProvider)
    {
        _channelProvider = channelProvider;
        // Sets the queue name based on the name of the message type (e.g., "EmailMessage").
        _queueName = typeof(TQueueMessage).Name;
    }

    public async Task<IChannel> GetChannelAsync()
    {
        _channel = await _channelProvider.GetChannelAsync();
        // Asynchronously declares the main queue and its associeted dead-letter queue.
        await DeclareQueueAndDeadLetter();
        return _channel;
    }

    private async Task DeclareQueueAndDeadLetter()
    {
        // Appends a suffix to the main queue name to create the dead-letter queue name (e.g., "EmailMessage-deadLetter").
        var deadLetterQueueName = $"{_queueName}{QueueingConstants.DeadLetterAddition}";

        // Configures arguments for a quorum-type dead-letter queue.
        var deadLetterQueueArgs = new Dictionary<string, object?>
        {
            { "x-queue-type", "quorum" },
            { "overflow", "reject-publish" }
        };

        // Declares a direct exchange for the dead-letter queue.
        await _channel.ExchangeDeclareAsync(deadLetterQueueName, ExchangeType.Direct);
        // Declares the dead-letter queue with the specified arguments. It is durable (true).
        await _channel.QueueDeclareAsync(deadLetterQueueName, true, false, false, deadLetterQueueArgs);
        // Binds the dead-letter queue to its corresponding exchange.
        await _channel.QueueBindAsync(deadLetterQueueName, deadLetterQueueName, deadLetterQueueName);

        // Configures arguments for the main queue, linking it to the dead-letter exchange.
        var queueArgs = new Dictionary<string, object?>
        {
            // Specifies the dead-letter exchange to which unroutable messages will be sent.
            { "x-dead-letter-exchange", deadLetterQueueName },
            // Specifies the routing key for messages sent to the dead-letter exchange.
            { "x-dead-letter-routing-key", deadLetterQueueName },
            // Specifies the queue type as quorum.
            { "x-queue-type", "quorum" },
            // Configures the dead-letter strategy to ensure messages are redelivered at least once.
            { "x-dead-letter-strategy", "at-least-once" },
            // Configures overflow behavior to reject new messages if the queue is full.
            { "overflow", "reject-publish" }
        };

        await _channel.ExchangeDeclareAsync(_queueName, ExchangeType.Direct);
        await _channel.QueueDeclareAsync(_queueName, true, false, false, queueArgs);
        await _channel.QueueBindAsync(_queueName, _queueName, _queueName);
    }
}
