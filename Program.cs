using App.ScopedService;
using Confluent.Kafka;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<ScopedBackgroundService>();
        services.AddScoped<IScopedProcessingService, DefaultScopedProcessingService>();
        var kafkaConfig = new ProducerConfig();
        var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();
        kafkaConfig.BootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers");

        kafkaConfig.BootstrapServers = "localhost:9092";
        services.AddSingleton<IProducer<string, string>>(new ProducerBuilder<string, string>(kafkaConfig).Build());
    });

IHost host = builder.Build();
host.Run();