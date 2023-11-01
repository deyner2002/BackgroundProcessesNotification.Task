namespace App.ScopedService;
using Confluent.Kafka;
using static Confluent.Kafka.ConfigPropertyNames;

public sealed class DefaultScopedProcessingService : IScopedProcessingService
{
    private int _executionCount;
    private readonly ILogger<DefaultScopedProcessingService> _logger;
    private readonly IProducer<string, string> _kafkaProducer;

    public DefaultScopedProcessingService(
        ILogger<DefaultScopedProcessingService> logger, IProducer<string, string> kafkaProducer)
    {
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

    public async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        var message = new Message<string, string>
        {
            Key = null,
            Value = "Mensaje 3 Tarea recurrente Valen"
        };

        await _kafkaProducer.ProduceAsync("test-kafka", message);
    }
}