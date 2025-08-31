namespace RabbitMQBridge.Queueing.Implementations;

public sealed class QueueingConfigurationSettings
{
    public string RabbitMqHostname { get; set; }
    public string RabbitMqUsername { get; set; }
    public string RabbitMqPassword { get; set; }
    public int? RabbitMqPort { get; set; }
    public ushort? RabbitMqConsumerDispatchConcurrency { get; set; }
}