using App.ScopedService;
using Confluent.Kafka;
using Azure.Identity;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<ScopedBackgroundService>();
        services.AddScoped<IScopedProcessingService, DefaultScopedProcessingService>();
        var kafkaConfig = new ProducerConfig();
        var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

        IConfiguration config = new ConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
                {
                    options.Connect(configuration.GetValue<string>("AppConfigsAzure"))
                   .ConfigureKeyVault(kv =>
                   {
                        kv.SetCredential(new DefaultAzureCredential());
                   });
                })
            .Build();

        kafkaConfig.BootstrapServers = config.GetValue<string>("kafka:configuration:BootstrapServers");

        services.Configure<MongoDBSettings>(config.GetSection("mongo:configuration"));
        services.AddSingleton<IProducer<string, string>>(new ProducerBuilder<string, string>(kafkaConfig).Build());
    });

IHost host = builder.Build();
host.Run();