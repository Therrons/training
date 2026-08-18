using Confluent.Kafka;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Service;
using fraud_poc_project_buss.Helper;
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
        private readonly IServiceScopeFactory _serviceLocator;

        public FraudBatchConsumerWorker(
            IServiceScopeFactory serviceLocator,
            ILogger<FraudBatchConsumerWorker> logger,
            IServiceScopeFactory serviceScopeFactory,
            IFraudEvaluationService evaluationService,
            IFraudRepository fraudRepository)
        {
            _logger = logger;
            _serviceLocator = serviceLocator;
            _serviceScopeFactory = serviceScopeFactory;
            _evaluationService = evaluationService;
            _fraudRepository = fraudRepository;
        }

        // Evaluates one transaction for fraud and saves the result. If anything goes
        // wrong, the transaction is sent to the dead-letter topic instead of being lost.
        public async Task HandleTransactionAsync((TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits) consumeResult, CancellationToken cancellationToken)
        {
            try
            {
                var result = _evaluationService.Evaluate(consumeResult.transactionEvent);
                await _fraudRepository.SaveFraudEvaluationAsync(result);

                _logger.LogInformationOnly("Processed transaction {TransactionId}: flagged={IsFlagged}, score={FraudScore}",
                        consumeResult.transactionEvent.TransactionId.ToString(), result.IsFlagged, result.FraudScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction {TransactionId}", consumeResult.transactionEvent.TransactionId);
                await SendToDlt(_fraudRepository, consumeResult.transactionEvent.KafkaTopic, JsonConvert.SerializeObject(consumeResult.transactionEvent), ex.Message);
            }
        }

        

        // Process batch of Transactions - the total items in the batch 
        // is defined in the ConsumerSettings
        public async Task HandleBatchTransactionSequentialAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> messages, CancellationToken cancellationToken)
        {
            using var scope = _serviceLocator.CreateScope();
            var batchEvaluationService = scope.ServiceProvider.GetRequiredService<IFraudEvaluationService>();
            var batchFraudRepository = scope.ServiceProvider.GetRequiredService<IFraudRepository>();
            int iTotal = messages?.Count ?? 0;

            if (iTotal > 0)
            {
                for (int i = 0; i < iTotal; i++)
                {
                    var msg = messages[i].transactionEvent;
                    try
                    {
                        if (msg != null)
                        {
                            var result = batchEvaluationService.Evaluate(messages[i].transactionEvent);
                            await batchFraudRepository.SaveFraudEvaluationAsync(result);
                            _logger.LogSensitiveData("Processed transaction: {data}", JsonConvert.SerializeObject(msg));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (msg != null)
                        {
                            _logger.LogError(ex, "Failed to consume the following Transaction data={data}", JsonConvert.SerializeObject(msg));
                            await SendToDlt(_fraudRepository, msg.KafkaTopic, JsonConvert.SerializeObject(msg), ex.Message);
                        }
                        else
                            _logger.LogError(ex, "Failed to consume the following Transaction data - see error object details");
                    }
                }
            }
        }

        // Process batch of Transactions - the total items in the batch 
        // is defined in the ConsumerSettings
        public async Task HandleBatchTransactionNonSequentialAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> messages,
                                                                   int concurrency, CancellationToken cancellationToken)
        {
            int iTotal = messages?.Count ?? 0;
            if (iTotal == 0) return;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = concurrency,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(messages, parallelOptions, async (msg, ct) =>
            {
                // Create a new scope per parallel task for thread-safety
                using var scope = _serviceLocator.CreateScope();
                var evaluationService = scope.ServiceProvider.GetRequiredService<IFraudEvaluationService>();
                var fraudRepository = scope.ServiceProvider.GetRequiredService<IFraudRepository>();

                try
                {
                    if (msg.transactionEvent != null)
                    {
                        var result = evaluationService.Evaluate(msg.transactionEvent);
                        await fraudRepository.SaveFraudEvaluationAsync(result);
                        _logger.LogInformationOnly("Processed transaction: {data}", JsonConvert.SerializeObject(msg));
                    }
                }
                catch (Exception ex)
                {
                    if (msg.transactionEvent != null)
                    {
                        _logger.LogError(ex, "Failed to consume the following Transaction data={data}", JsonConvert.SerializeObject(msg));
                        await SendToDlt(_fraudRepository, msg.transactionEvent.KafkaTopic, JsonConvert.SerializeObject(msg.transactionEvent), ex.Message);
                    }
                    else
                        _logger.LogError(ex, "Failed to consume the following Transaction data - see error object details");
                }
            });
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
