using Microsoft.AspNetCore.Mvc;
using RabbitMQBridge.Models;
using RabbitMQBridge.Queueing.Interfaces;

namespace RabbitMQBridge.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueController : ControllerBase
{
    private readonly IQueueProducer<TestQueueMessage> _queueProducer;

    public QueueController(IQueueProducer<TestQueueMessage> queueProducer)
    {
        _queueProducer = queueProducer;
    }

    [HttpPost("test-queue")]
    public async Task<IActionResult> TestQueue([FromBody] string sentence)
    {
        var message = new TestQueueMessage
        {
            TimeToLive = TimeSpan.FromMinutes(1),
            Sentence = sentence
        };

        await _queueProducer.PublishMessageAsync(message);
        return Ok("Message enqueued");
    }
}