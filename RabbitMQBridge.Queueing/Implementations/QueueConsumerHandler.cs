using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Queueing.Implementations;

internal class
    QueueConsumerHandler<TMessageConsumer, TQueueMessage> : IQueueConsumerHandler<TMessageConsumer, TQueueMessage>
    where TMessageConsumer : IQueueConsumer<TQueueMessage>
    where TQueueMessage : class, IQueueMessage
{
    private readonly ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _queueName;
    private IChannel _consumerRegistrationChannel;
    private string _consumerTag;
    private readonly string _consumerName;

    public QueueConsumerHandler(
        ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> logger, IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _queueName = typeof(TQueueMessage).Name;
        _consumerName = typeof(TMessageConsumer).Name;
    }

    public async Task RegisterQueueConsumerAsync()
    {
        // Creates a new DI scope specifically for registering the consumer.
        var scope = _serviceProvider.CreateAsyncScope();

        // Fetches a channel from the provider, which has already declared the necessary queues and exchanges.
        _consumerRegistrationChannel = await scope.ServiceProvider
            .GetRequiredService<IQueueChannelProvider<TQueueMessage>>()
            .GetChannelAsync();

        // Creates an asynchronous consumer to handle incoming messages.
        var consumer = new AsyncEventingBasicConsumer(_consumerRegistrationChannel);

        // Subscribes the HandleMessageAsync method to the consumer's received event.
        consumer.ReceivedAsync += HandleMessageAsync;

        try
        {
            // Starts consuming messages from the queue without auto-acknowledgement.
            _consumerTag = await _consumerRegistrationChannel.BasicConsumeAsync(_queueName, false, consumer);
            _logger.LogInformation($"Successfully registered consumer {_consumerName} for queue {_queueName}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to register consumer {_consumerName} for queue {_queueName}.");
            throw; // Re-throw the exception to fail the service startup.
        }
    }

    public async Task CancelQueueConsumerAsync()
    {
        _logger.LogInformation($"Cancelling queue consumer registration for {_consumerName}");

        try
        {
            // Cancels the consumer by its tag, which stops it from receiving new messages.
            await _consumerRegistrationChannel.BasicCancelAsync(_consumerTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while cancelling queue consumer registration for {_consumerName}.");
            throw; // Re-throw the exception to indicate failure.
        }
    }

    private async Task HandleMessageAsync(object ch, BasicDeliverEventArgs ea)
    {
        _logger.LogInformation($"Received message from queue {_queueName} with DeliveryTag {ea.DeliveryTag}.");

        // Creates a new, isolated DI scope for processing each individual message.
        var consumerScope = _serviceProvider.CreateAsyncScope();

        // Gets the channel on which the message was delivered.
        var consumingChannel = ((AsyncEventingBasicConsumer)ch).Channel;

        IChannel producingChannel = null;

        try
        {
            // Gets a separate channel for any messages that will be produced within this consumption process.
            // This is crucial for transactional publishing.
            producingChannel = await consumerScope.ServiceProvider
                .GetRequiredService<IChannelProvider>()
                .GetChannelAsync();

            var message = DeserializeMessage(ea.Body.ToArray());

            _logger.LogInformation($"Processing message {message.MessageId}");

            // Starts a transaction on the producing channel.
            await producingChannel.TxSelectAsync();

            // Retrieves a new instance of the consumer from the scope to ensure proper dependency resolution.
            var consumerInstance = consumerScope.ServiceProvider.GetRequiredService<TMessageConsumer>();

            // Calls the consumer's method to process the message asynchronously.
            await consumerInstance.ConsumeAsync(message);

            // Checks if either channel was unexpectedly closed during processing.
            if (producingChannel.IsClosed || consumingChannel.IsClosed)
            {
                throw new Exception("A channel is closed during processing");
            }

            // Commits the transaction, making any produced messages permanent.
            await producingChannel.TxSelectAsync();

            // Acknowledges the message on the consuming channel, signaling successful processing.
            await consumingChannel.BasicAckAsync(ea.DeliveryTag, false);

            _logger.LogInformation($"Successfully processed and acknowledged message {message.MessageId}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process message with delivery tag {ea.DeliveryTag} for consumer {_consumerName}.");
            await RejectMessage(ea.DeliveryTag, consumingChannel, producingChannel);
        }
        finally
        {
            // Ensures the scope is disposed, releasing all resources and channels created within it.
            await consumerScope.DisposeAsync();
        }
    }
    
    private async Task RejectMessage(ulong deliveryTag, IChannel consumeChannel, IChannel scopeChannel)
    {
        try
        {
            // Checks if a producing channel was created before rolling back the transaction.
            if (scopeChannel != null)
            {
                // Rolls back any messages that were published in the failed transaction.
                await scopeChannel.TxRollbackAsync();
                _logger.LogInformation("Successfully rolled back transaction.");
            }

            // Rejects the message on the consuming channel. The message will be routed to the dead-letter queue.
            await consumeChannel.BasicRejectAsync(deliveryTag, false);

            _logger.LogWarning($"Rejected message with delivery tag {deliveryTag}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to reject message with delivery tag {deliveryTag}");
        }
    }

    private static TQueueMessage DeserializeMessage(byte[] message)
    {
        var stringMessage = Encoding.UTF8.GetString(message);
        return JsonSerializer.Deserialize<TQueueMessage>(stringMessage);
    }
}
