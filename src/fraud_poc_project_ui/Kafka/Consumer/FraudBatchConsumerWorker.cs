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
    // Takes transaction events that have been read from Kafka and runs them through
    // fraud evaluation, then saves the results to the database.
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

        // Evaluates one transaction for fraud and saves the result. If anything goes
        // wrong, the transaction is sent to the dead-letter topic instead of being lost.
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

        // NOTE: this is currently a no-op placeholder - it takes the batch but doesn't
        // evaluate or save anything yet. FraudConsumer.RunConsumerLoopAsync (in the repo
        // project) calls this method for every batch it reads, so today those batches
        // are effectively not being processed. HandleAsync above does the real
        // evaluate-and-save work but isn't currently being called by the batch consumer.
        // Left as-is since fixing the actual batch processing logic is a behavior change,
        // not a simplification, and wasn't part of this cleanup.
        public Task HandleBatchAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> batch, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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
