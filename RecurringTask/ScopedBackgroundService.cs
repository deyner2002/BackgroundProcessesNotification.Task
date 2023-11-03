using APIEmisorKafka.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace App.ScopedService
{
    public sealed class DefaultScopedProcessingService : IScopedProcessingService
    {
        private int _executionCount;
        private readonly ILogger<DefaultScopedProcessingService> _logger;
        private readonly IProducer<string, string> _kafkaProducer;
        private readonly MongoDBSettings _mongoDBSettings;

        public DefaultScopedProcessingService(
            ILogger<DefaultScopedProcessingService> logger, IProducer<string, string> kafkaProducer, IOptions<MongoDBSettings> mongoDBSettings)
        {
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _mongoDBSettings = mongoDBSettings.Value;
        }

        public async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            try
            {
                MongoClient client = new MongoClient(_mongoDBSettings.ConnectionString);

                IMongoDatabase database = client.GetDatabase(_mongoDBSettings.DatabaseName);

                IMongoCollection<Notification> collectionNotification = database.GetCollection<Notification>(_mongoDBSettings.CollectionName);

                IMongoCollection<ExecutionHistory> collectionHistory = database.GetCollection<ExecutionHistory>(_mongoDBSettings.CollectionNameHistory);

                var result = collectionHistory.Find(Builders<ExecutionHistory>.Filter.Empty)
                    .Sort(Builders<ExecutionHistory>.Sort.Descending(r => r.EndDate)).FirstOrDefault();

                ExecutionHistory newExecution = new ExecutionHistory
                {
                    StartDate = result != null ? result.EndDate.AddHours(-5).AddSeconds(1) : DateTime.Now.AddSeconds(-1),
                    EndDate = DateTime.Now,
                };

                var notifications = collectionNotification.Find(Builders<Notification>.Filter.Empty).ToList();

                notifications = notifications.Where(item => newExecution.StartDate <= item.ProgrammingInfo.ActivationTime && item.ProgrammingInfo.ActivationTime <= newExecution.EndDate).ToList();

                foreach (var notification in notifications)
                {
                    var message = new Message<string, string>
                    {
                        Key = null,
                        Value = Newtonsoft.Json.JsonConvert.SerializeObject(notification)
                    };

                    await _kafkaProducer.ProduceAsync("test-kafka", message);
                }

                collectionHistory.InsertOne(newExecution);
            }
            catch (Exception ex)
            {

            }
        }
    }
}

