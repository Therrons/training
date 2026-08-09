using Confluent.Kafka;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Service;
using fraud_poc_project_repo.Interfaces;
using fraud_poc_project_repo.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace fraud_poc_project.Kafka.Consumer
{
    public class FraudBatchConsumerWorker : ITransactionEventHandler
    {
        private readonly ILogger<FraudBatchConsumerWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IFraudEvaluationService _evaluationService;
        private readonly IFraudRepository _fraudRepository;

        public FraudBatchConsumerWorker(
            ILogger<FraudBatchConsumerWorker> logger,
            IServiceScopeFactory serviceScopeFactory,
            IFraudEvaluationService evaluationService,
            IFraudRepository fraudRepository)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _evaluationService = evaluationService;
            _fraudRepository = fraudRepository;
        }

        public async Task HandleAsync((TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits) consumeResult, CancellationToken cancellationToken)
        {
            if (consumeResult.transactionEvent == null)
            {
                _logger.LogWarning("TransactionEvent is null for {Offset} for topic {Topic}",
                    consumeResult.transactionEventAsBits.Offset, consumeResult.transactionEventAsBits.Topic);
                return;
            }

            try
            {
                var result = _evaluationService.Evaluate(consumeResult.transactionEvent);
                await _fraudRepository.SaveFraudEvaluationAsync(result);

                _logger.LogInformation(
                    "Processed transaction {TransactionId}: flagged={IsFlagged}, score={FraudScore}",
                    consumeResult.transactionEvent.TransactionId, result.IsFlagged, result.FraudScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction {TransactionId}", consumeResult.transactionEvent.TransactionId);
                await SendToDlt(_fraudRepository, consumeResult.transactionEvent.KafkaTopic, JsonConvert.SerializeObject(consumeResult.transactionEvent), ex.Message);
            }
        }

        public async Task HandleBatchAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
                return;

            using var scope = _serviceScopeFactory.CreateScope();
            var evaluationService = scope.ServiceProvider.GetRequiredService<IFraudEvaluationService>();
            var repository = scope.ServiceProvider.GetRequiredService<IFraudRepository>();

            foreach (var consumeResult in batch)
            {
                //if (consumeResult?.Message?.Value == null)
                //    continue;

                //var rawJson = Encoding.UTF8.GetString(consumeResult.Message.Value);

                //TransactionEvent? transEvent;
                //try
                //{
                //    transEvent = JsonConvert.DeserializeObject<TransactionEvent>(rawJson);
                //}
                //catch (JsonException ex)
                //{
                //    _logger.LogWarning(ex,
                //        "Failed to deserialize Kafka message at offset {Offset} on topic {Topic}",
                //        consumeResult.Event.Offset.Value, consumeResult.Topic);
                //    await SendToDlt(repository, consumeResult.Topic, rawJson, ex.Message);
                //    continue;
                //}

                //if (transEvent == null)
                //{
                //    _logger.LogWarning(
                //        "Null domain event at offset {Offset} on topic {Topic}",
                //        consumeResult.Offset.Value, consumeResult.Topic);
                //    continue;
                //}

                //var kafkaEvent = new TransactionEvent
                //{
                //    KafkaTopic = consumeResult.Topic,
                //    TransactionId = transEvent.TransactionId,
                //    CustomerId = transEvent.CustomerId,
                //    AccountId = transEvent.AccountId,
                //    Amount = transEvent.Amount,
                //    Currency = transEvent.Currency,
                //    MerchantName = transEvent.MerchantName,
                //    MerchantCategory = transEvent.MerchantCategory,
                //    TransactionType = transEvent.TransactionType,
                //    Channel = transEvent.Channel,
                //    CountryCode = transEvent.CountryCode,
                //    TransactionTime = transEvent.TransactionTime
                //};

                //try
                //{
                //    var result = evaluationService.Evaluate(kafkaEvent);
                //    await repository.SaveFraudEvaluationAsync(result);

                //    _logger.LogInformation(
                //        "Processed transaction {TransactionId}: flagged={IsFlagged}, score={FraudScore}",
                //        kafkaEvent.TransactionId, result.IsFlagged, result.FraudScore);
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogError(ex, "Error processing transaction {TransactionId}", kafkaEvent.TransactionId);
                //    await SendToDlt(repository, consumeResult.Topic, rawJson, ex.Message);
                //}
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
