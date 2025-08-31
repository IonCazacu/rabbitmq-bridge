using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Queueing.Implementations;

internal class QueueProducer<TQueueMessage> : IQueueProducer<TQueueMessage> where TQueueMessage : IQueueMessage
{
    private ILogger<QueueProducer<TQueueMessage>> _logger;
    private readonly IChannel _channel;
    private readonly string _queueName;

    private QueueProducer(ILogger<QueueProducer<TQueueMessage>> logger, IChannel channel)
    {
        _logger = logger;
        _channel = channel;
        _queueName = typeof(TQueueMessage).Name;
    }

    // A static factory method used to asynchronously create and initialize a QueueProducer instance.
    public static async Task<QueueProducer<TQueueMessage>> CreateAsync(
        ILogger<QueueProducer<TQueueMessage>> logger, IQueueChannelProvider<TQueueMessage> channelProvider
    )
    {
        // Gets a pre-configured channel from the provider, which includes queue declarations.
        var channel = await channelProvider.GetChannelAsync();
        return new QueueProducer<TQueueMessage>(logger, channel);
    }

    public async Task PublishMessageAsync(TQueueMessage message)
    {
        // Throws an exception if the message object is null.
        if (Equals(message, default(TQueueMessage)))
        {
            throw new ArgumentNullException(nameof(message));
        }

        // Ensures the message has a TimeToLive value greater than zero.
        if (message.TimeToLive.Ticks <= 0)
        {
            throw new Exception($"{nameof(message.TimeToLive)} must be greater than 0");
        }

        // Assigns a unique ID to the message before publishing.
        message.MessageId = Guid.NewGuid();

        try
        {
            var serializedMessage = SerializeMessage(message);

            // Creates RabbitMQ properties for the message.
            var basicProperties = new BasicProperties
            {
                // Ensures the message will be durable and survive a broker restart.
                Persistent = true,
                Type = _queueName,
                // Specifies the Time-To-Live (TTL) for the message in milliseconds.
                Expiration = message.TimeToLive.Milliseconds.ToString(CultureInfo.InvariantCulture)
            };

            // Publishes the message to the specified queue.
            await _channel.BasicPublishAsync(_queueName, _queueName, false, basicProperties, serializedMessage);

            _logger.LogDebug($"Successfully published message with ID {message.MessageId} to queue {_queueName}.");
        }
        catch (Exception e)
        {
            var msg = $"Could not publish message to Queue: {_queueName}";
            _logger.LogError(e, msg);
            throw new Exception(msg);
        }
    }

    // Serializes the message object into a byte array for publishing.
    private static byte[] SerializeMessage(TQueueMessage message)
    {
        var stringContent = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(stringContent);
    }
}
