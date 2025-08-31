using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Queueing.Implementations;

internal class QueueConsumerRegistrationService<TMessageConsumer, TQueueMessage> : IHostedService
    where TMessageConsumer : IQueueConsumer<TQueueMessage> where TQueueMessage : class, IQueueMessage
{
    private readonly ILogger<QueueConsumerRegistrationService<TMessageConsumer, TQueueMessage>> _logger;
    private IQueueConsumerHandler<TMessageConsumer, TQueueMessage> _consumerHandler;
    private readonly IServiceProvider _serviceProvider;
    private IServiceScope _scope;

    public QueueConsumerRegistrationService(
        ILogger<QueueConsumerRegistrationService<TMessageConsumer, TQueueMessage>> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            $"Starting consumer registration for {ConsumerType} on queue for messages of type {MessageType}.");

        // Creates a new dependency injection scope for the consumer handler to manage its lifecycle and dependencies.
        // This ensures that each consumer handler instance gets its own isolated set of scoped services, like a dedicated RabbitMQ channel.
        _scope = _serviceProvider.CreateAsyncScope();

        _consumerHandler = _scope.ServiceProvider
            .GetRequiredService<IQueueConsumerHandler<TMessageConsumer, TQueueMessage>>();
        _consumerHandler.RegisterQueueConsumerAsync();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            $"Stopping consumer {ConsumerType} for queue with messages of type {MessageType}.");
        _consumerHandler.CancelQueueConsumerAsync();
        // Disposes the service scope to release all scoped services and resources (e.g., the RabbitMQ channel).
        _scope.Dispose();
        return Task.CompletedTask;
    }
}
