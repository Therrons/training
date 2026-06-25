using Confluent.Kafka;
using fraud_poc_project_models.Models;
using fraud_poc_project_models.Models.Fraud;
using fraud_poc_project_repo;
using fraud_poc_project_repo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace fraud_poc_project.Fraud.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IFraudEvaluationService _evaluationService;
        private readonly IFraudRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaConsumerService> _logger;

        public KafkaConsumerService(
            IFraudEvaluationService evaluationService,
            IFraudRepository repository,
            IConfiguration configuration,
            ILogger<KafkaConsumerService> logger)
        {
            _evaluationService = evaluationService;
            _repository = repository;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"]
                ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");
            var groupId = _configuration["Kafka:GroupId"] ?? "fraud-detection-group";
            var topic = _configuration["Kafka:Topic"]
                ?? throw new InvalidOperationException("Kafka:Topic is not configured.");

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(topic);

            _logger.LogInformation("Kafka consumer started. Topic: {Topic}, Group: {GroupId}", topic, groupId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value == null)
                        continue;

                    TransactionEvent? transactionEvent;
                    try
                    {
                        transactionEvent = JsonConvert.DeserializeObject<TransactionEvent>(consumeResult.Message.Value);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize Kafka message at offset {Offset}", consumeResult.Offset.Value);
                        await SendTodlt(topic, consumeResult.Message.Value, ex.Message);
                        consumer.Commit(consumeResult);
                        continue;
                    }

                    if (transactionEvent == null)
                    {
                        _logger.LogWarning("Null transaction event deserialized from Kafka message at offset {Offset}", consumeResult.Offset.Value);
                        consumer.Commit(consumeResult);
                        continue;
                    }

                    var kafkaEvent = new KafkaTransactionEvent
                    {
                        KafkaTopic = consumeResult.Topic,
                        KafkaPartition = consumeResult.Partition.Value,
                        KafkaOffset = consumeResult.Offset.Value,
                        TransactionId = transactionEvent.TransactionId,
                        CustomerId = transactionEvent.CustomerId,
                        AccountId = transactionEvent.AccountId,
                        Amount = transactionEvent.Amount,
                        Currency = transactionEvent.Currency,
                        MerchantName = transactionEvent.MerchantName,
                        MerchantCategory = transactionEvent.MerchantCategory,
                        TransactionType = transactionEvent.TransactionType,
                        Channel = transactionEvent.Channel,
                        CountryCode = transactionEvent.CountryCode,
                        TransactionTime = transactionEvent.TransactionTime
                    };

                    try
                    {
                        var result = _evaluationService.Evaluate(kafkaEvent);
                        await _repository.SaveFraudEvaluationAsync(result);
                        consumer.Commit(consumeResult);

                        _logger.LogInformation(
                            "Processed transaction {TransactionId}: flagged={IsFlagged}, score={FraudScore}",
                            kafkaEvent.TransactionId, result.IsFlagged, result.FraudScore);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing transaction {TransactionId}", kafkaEvent.TransactionId);
                        await SendTodlt(topic, consumeResult.Message.Value, ex.Message);
                        consumer.Commit(consumeResult);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            consumer.Close();
            _logger.LogInformation("Kafka consumer stopped.");
        }

        private async Task SendTodlt(string topic, string messageData, string error)
        {
            try
            {
                await _repository.SavedltErrorAsync(topic, messageData, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write message to dlt for topic {Topic}", topic);
            }
        }
    }
}
