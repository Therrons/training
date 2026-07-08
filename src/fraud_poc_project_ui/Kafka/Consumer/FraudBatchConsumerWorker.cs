using Confluent.Kafka;
using Credit.Kafka.Messaging.Extensions;
using Credit.Kafka.Messaging.Handlers;
using fraud_poc_project.Fraud.Services;
using fraud_poc_project.Kafka.Events;
using fraud_poc_project_models.Models.Fraud;
using fraud_poc_project_repo;
using fraud_poc_project_repo.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fraud_poc_project.Kafka.Consumer
{
    public class FraudBatchConsumerWorker : IBatchMessageHandler
    {
        private readonly ILogger<FraudBatchConsumerWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FraudBatchConsumerWorker(
            ILogger<FraudBatchConsumerWorker> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task HandleBatchMessagesAsync(
            List<ConsumeResult<byte[], byte[]>> messages,
            CancellationToken cancellationToken)
        {
            if (messages.Count == 0)
                return;

            using var scope = _serviceScopeFactory.CreateScope();
            var evaluationService = scope.ServiceProvider.GetRequiredService<IFraudEvaluationService>();
            var repository = scope.ServiceProvider.GetRequiredService<IFraudRepository>();

            foreach (var consumeResult in messages)
            {
                if (consumeResult?.Message?.Value == null)
                    continue;

                var rawJson = Encoding.UTF8.GetString(consumeResult.Message.Value);

                FraudTransactionDomainEvent? domainEvent;
                try
                {
                    domainEvent = JsonConvert.DeserializeObject<FraudTransactionDomainEvent>(rawJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to deserialize Kafka message at offset {Offset} on topic {Topic}",
                        consumeResult.Offset.Value, consumeResult.Topic);
                    await SendToDlt(repository, consumeResult.Topic, rawJson, ex.Message);
                    continue;
                }

                if (domainEvent == null)
                {
                    _logger.LogWarning(
                        "Null domain event at offset {Offset} on topic {Topic}",
                        consumeResult.Offset.Value, consumeResult.Topic);
                    continue;
                }

                var kafkaEvent = new KafkaTransactionEvent
                {
                    KafkaTopic = consumeResult.Topic,
                    KafkaPartition = consumeResult.Partition.Value,
                    KafkaOffset = consumeResult.Offset.Value,
                    TransactionId = domainEvent.TransactionId,
                    CustomerId = domainEvent.CustomerId,
                    AccountId = domainEvent.AccountId,
                    Amount = domainEvent.Amount,
                    Currency = domainEvent.Currency,
                    MerchantName = domainEvent.MerchantName,
                    MerchantCategory = domainEvent.MerchantCategory,
                    TransactionType = domainEvent.TransactionType,
                    Channel = domainEvent.Channel,
                    CountryCode = domainEvent.CountryCode,
                    TransactionTime = domainEvent.TransactionTime
                };

                try
                {
                    var result = evaluationService.Evaluate(kafkaEvent);
                    await repository.SaveFraudEvaluationAsync(result);

                    _logger.LogInformation(
                        "Processed transaction {TransactionId}: flagged={IsFlagged}, score={FraudScore}",
                        kafkaEvent.TransactionId, result.IsFlagged, result.FraudScore);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing transaction {TransactionId}", kafkaEvent.TransactionId);
                    await SendToDlt(repository, consumeResult.Topic, rawJson, ex.Message);
                }
            }
        }

        private async Task SendToDlt(IFraudRepository repository, string topic, string messageData, string error)
        {
            try
            {
                await repository.SavedltErrorAsync(topic, messageData, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write message to DLT for topic {Topic}", topic);
            }
        }
    }
}
