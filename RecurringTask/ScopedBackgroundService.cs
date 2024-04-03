using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using APIEmisorKafka.Enum;
using APIEmisorKafka.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RestSharp;

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

                var indexKeysDefinition = Builders<ExecutionHistory>.IndexKeys.Descending(r => r.EndDate);
                var indexOptions = new CreateIndexOptions { Unique = false }; // Ajusta las opciones según tus necesidades

                var createIndexModel = new CreateIndexModel<ExecutionHistory>(indexKeysDefinition, indexOptions);

                await collectionHistory.Indexes.CreateOneAsync(createIndexModel);

                var result = collectionHistory.Find(Builders<ExecutionHistory>.Filter.Empty)
                    .Sort(Builders<ExecutionHistory>.Sort.Descending(r => r.EndDate)).FirstOrDefault();

                ExecutionHistory newExecution = new ExecutionHistory
                {
                    StartDate = result != null ? result.EndDate.AddHours(-5).AddSeconds(1) : DateTime.Now.AddSeconds(-1),
                    EndDate = DateTime.Now,
                };

                var notifications = collectionNotification.Find(Builders<Notification>.Filter.Empty).ToList();

                notifications = notifications.Where(item => newExecution.StartDate <= item.ProgrammingInfo.ActivationTime && item.ProgrammingInfo.ActivationTime <= newExecution.EndDate && item.ProgrammingInfo.Active).ToList();

                foreach (var notification in notifications)
                {
                    if (notification.GetObject)
                    {
                        notification.Object = GetObject(notification.GetObjectUrl);
                    }

                    var message = new Message<string, string>
                    {
                        Key = null,
                        Value = Newtonsoft.Json.JsonConvert.SerializeObject(notification)
                    };

                    await _kafkaProducer.ProduceAsync("test-kafka", message);

                    if(notification.ProgrammingInfo.IsRecurring)
                    {
                        switch (notification.ProgrammingInfo.Recurrence)
                        {
                            case Recurrence.Annual:
                                notification.ProgrammingInfo.ActivationTime = notification.ProgrammingInfo.ActivationTime.AddYears(1);
                                break;
                            case Recurrence.Monthly:
                                notification.ProgrammingInfo.ActivationTime = notification.ProgrammingInfo.ActivationTime.AddMonths(1);
                                break;
                            case Recurrence.Diary:
                                notification.ProgrammingInfo.ActivationTime = notification.ProgrammingInfo.ActivationTime.AddDays(1);
                                break;
                            case Recurrence.Hourly:
                                notification.ProgrammingInfo.ActivationTime = notification.ProgrammingInfo.ActivationTime.AddHours(1);
                                break;
                            default:
                                break;
                        }

                        if (notification.ProgrammingInfo.ActivationTime > notification.ProgrammingInfo.EndDate)
                        {
                            var filter = Builders<Notification>.Filter.Eq("_id", notification.Id);
                            collectionNotification.DeleteOne(filter);
                        }
                        else
                        {
                            var filter = Builders<Notification>.Filter.Eq("_id", notification.Id);

                            var update = Builders<Notification>.Update
                                .Set("ProgrammingInfo.ActivationTime", notification.ProgrammingInfo.ActivationTime)
                                .CurrentDate("CampoFechaActualizacion");

                            var resultUpdate = collectionNotification.UpdateOne(filter, update);
                        }
                    }
                    else
                    {
                        var filter = Builders<Notification>.Filter.Eq("_id", notification.Id);
                        collectionNotification.DeleteOne(filter);
                    }
                }

                collectionHistory.InsertOne(newExecution);
            }
            catch (Exception ex)
            {

            }
        }

        private static string GetObject(string Url)
        {
            Uri uri = new Uri(Url);

            var restClient = new RestClient(uri.GetLeftPart(UriPartial.Authority));

            var request = new RestRequest(uri.PathAndQuery, Method.Get);

            var response = restClient.Execute(request);

            return response.Content;
        }
    }
}

