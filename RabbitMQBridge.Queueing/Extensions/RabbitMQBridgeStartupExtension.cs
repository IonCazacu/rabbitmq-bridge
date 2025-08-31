using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQBridge.Queueing.Implementations;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Queueing.Extensions;

public static class RabbitMQBridgeStartupExtension
{
    public static void AddQueueing(this IServiceCollection services, QueueingConfigurationSettings settings)
    {
        // Registers the configuration settings as a singleton, ensuring a single instance is used application-wide.
        services.AddSingleton<QueueingConfigurationSettings>(settings);

        // Registers the RabbitMQ ConnectionFactory as a singleton.
        // This provides a single, shared instance for creating all connections, which is a best practice.
        services.AddSingleton<ConnectionFactory>(provider =>
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.RabbitMqHostname,
                UserName = settings.RabbitMqUsername,
                Password = settings.RabbitMqPassword,
                Port = settings.RabbitMqPort.GetValueOrDefault(),
                ConsumerDispatchConcurrency = settings.RabbitMqConsumerDispatchConcurrency.GetValueOrDefault(),
                AutomaticRecoveryEnabled = true
            };

            return factory;
        });

        // Registers a singleton connection provider. This ensures there's only one TCP connection to the RabbitMQ server for the entire application lifetime.
        services.AddSingleton<IConnectionProvider, ConnectionProvider>();

        // Registers a scoped channel provider. Each logical operation or request will use its own channel,
        // which is the recommended way to use RabbitMQ channels.
        services.AddScoped<IChannelProvider, ChannelProvider>();

        // Registers a generic scoped queue channel provider for each specific message type.
        // This ensures that each type of queue has its own channel provider.
        services.AddScoped(typeof(IQueueChannelProvider<>), typeof(QueueChannelProvider<>));
    }

    public static void AddQueueMessageProducer<TQueueMessage>(this IServiceCollection services)
        where TQueueMessage : class, IQueueMessage
    {
        // Registers a scoped message producer for a specific message type.
        // Each producer will have its own dedicated channel, ensuring message publishing is isolated per scope.
        services.AddScoped<IQueueProducer<TQueueMessage>>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<QueueProducer<TQueueMessage>>>();
            var channelProvider = provider.GetRequiredService<IQueueChannelProvider<TQueueMessage>>();

            // Creates the producer instance asynchronously and blocks until it's ready.
            // This is necessary because the `AddScoped` method does not natively support async factory methods.
            return QueueProducer<TQueueMessage>
                .CreateAsync(logger, channelProvider)
                .GetAwaiter()
                .GetResult();
        });
    }

    public static void AddQueueMessageConsumer<TMessageConsumer, TQueueMessage>(this IServiceCollection services)
        where TMessageConsumer : IQueueConsumer<TQueueMessage>
        where TQueueMessage : class, IQueueMessage
    {
        // Registers the specific message consumer as a scoped service.
        // This allows the consumer to use its own set of scoped dependencies for each message it processes.
        services.AddScoped(typeof(TMessageConsumer));

        // Registers the consumer handler as a scoped service.
        // This handler manages the lifecycle of a specific consumer, including registration and cancellation.
        services
            .AddScoped<IQueueConsumerHandler<TMessageConsumer, TQueueMessage>,
                QueueConsumerHandler<TMessageConsumer, TQueueMessage>>();

        // Registers the registration service as an IHostedService.
        // This ensures that the consumer is automatically registered with the message queue when the application starts
        // and properly unregistered when the application shuts down.
        services.AddHostedService<QueueConsumerRegistrationService<TMessageConsumer, TQueueMessage>>();
    }
}
