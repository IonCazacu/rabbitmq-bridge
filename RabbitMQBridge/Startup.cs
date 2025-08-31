using RabbitMQBridge.Consumers;
using RabbitMQBridge.Models;
using RabbitMQBridge.Queueing.Extensions;
using RabbitMQBridge.Queueing.Implementations;

namespace RabbitMQBridge;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddQueueing(new QueueingConfigurationSettings
        {
            RabbitMqHostname = "localhost",
            RabbitMqUsername = "guest",
            RabbitMqPassword = "guest",
            RabbitMqPort = 5672,
            RabbitMqConsumerDispatchConcurrency = 5
        });

        services.AddQueueMessageConsumer<TestQueueMessageConsumer, TestQueueMessage>();
        services.AddQueueMessageProducer<TestQueueMessage>();

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(x => x.MapControllers());
    }
}