using fraud_poc_project.Models;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_repo.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace fraud_poc_project.Controllers
{
    /// <summary>
    /// Produces synthetic transaction events to Kafka to simulate load.
    /// </summary>
    [ApiController]
    [Route("api/load")]
    public class LoadSimulatorController : ControllerBase
    {
        private static readonly string[] TransactionTypes = { "POS", "ATM", "EFT", "CNP" };
        private static readonly string[] Channels = { "Branch", "Online", "ATM", "Mobile" };
        private static readonly string[] MerchantCategories = { "retail", "grocery", "gambling", "crypto", "fuel", "restaurant", "travel", "money_transfer" };
        private static readonly string[] CountryCodes = { "ZA", "US", "GB", "NG", "DE", "CN" };

        private readonly IFraudProducer _producer;
        private readonly FraudKafkaConsumerSettings _consumerSettings;
        private readonly ILogger<LoadSimulatorController> _logger;

        public LoadSimulatorController(
            IFraudProducer producer,
            IOptions<FraudKafkaConsumerSettings> consumerSettings,
            ILogger<LoadSimulatorController> logger)
        {
            _producer = producer;
            _consumerSettings = consumerSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Produce a batch of synthetic transaction events to the fraud topic.
        /// </summary>
        /// <param name="count">Number of events to produce (1–1000, default 50).</param>
        /// <param name="highFraudRatio">Fraction of events that should look fraudulent (0.0–1.0, default 0.2).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost("simulate")]
        public async Task<ActionResult<LoadSimulationResult>> Simulate(
            [FromQuery] int count = 50,
            [FromQuery] double highFraudRatio = 0.2,
            CancellationToken cancellationToken = default)
        {
            if (count < 1 || count > 1000)
                return BadRequest("count must be between 1 and 1000.");

            if (highFraudRatio < 0.0 || highFraudRatio > 1.0)
                return BadRequest("highFraudRatio must be between 0.0 and 1.0.");

            var rng = new Random();
            int produced = 0;
            int failed = 0;

            cancellationToken = cancellationToken == default ? new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token : cancellationToken;

            for (int i = 0; i < count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                bool isFraudulent = rng.NextDouble() < highFraudRatio;
                var @event = BuildEvent(rng, isFraudulent);

                bool ok = await _producer.ProduceAsync(@event, cancellationToken);
                if (ok)
                    produced++;
                else
                    failed++;

                // small stagger to avoid overwhelming the broker in a single burst
                if (i % 50 == 49)
                    await Task.Delay(50, cancellationToken);
            }

            _logger.LogInformationOnly(
                "Load simulation completed: {Produced} produced, {Failed} failed out of {Total} requested.",
                produced, failed, count);

            return Ok(new LoadSimulationResult
            {
                Requested = count,
                Produced = produced,
                Failed = failed,
                Topic = _consumerSettings.TransactionTopic
            });
        }

        /// <summary>
        /// Produce a single synthetic transaction event and return it as a preview.
        /// </summary>
        /// <param name="fraudulent">Retrieve Fraudulent event if true, otherwise a normal event.</param>
        [HttpGet("preview")]
        public ActionResult<TransactionEvent> Preview([FromQuery] bool fraudulent = false)
        {
            return Ok(BuildEvent(new Random(), fraudulent));
        }

        private TransactionEvent BuildEvent(Random rng, bool fraudulent)
        {
            // Pick a random customer from a pool so some customers appear repeatedly
            var customerId = $"CUST-{rng.Next(1, 200):D4}";
            var accountId = $"ACC-{rng.Next(1, 500):D5}";

            string transactionType;
            string? channel;
            decimal amount;
            string? merchantCategory;
            string? countryCode;
            DateTime transactionTime = DateTime.UtcNow;

            if (fraudulent)
            {
                // Deliberately trigger fraud rules
                transactionType = Pick(rng, new[] { "CNP", "ATM" });
                channel = transactionType == "CNP" ? "Online" : "ATM";
                amount = Pick(rng, new[] { 60000m, 10000m, 15000m, 100000m, 5000m });
                merchantCategory = Pick(rng, new[] { "gambling", "crypto", "money_transfer" });
                countryCode = Pick(rng, new[] { "US", "NG", "GB" });        // non-ZA
            }
            else
            {
                transactionType = Pick(rng, TransactionTypes);
                channel = Pick(rng, Channels);
                amount = Math.Round((decimal)(rng.NextDouble() * 4900 + 100), 2);
                merchantCategory = Pick(rng, MerchantCategories);
                countryCode = rng.NextDouble() < 0.9 ? "ZA" : Pick(rng, CountryCodes);
            }

            var correlationId = Guid.NewGuid();
            return new TransactionEvent
            {
                KafkaTopic = _consumerSettings.TransactionTopic,
                CorrelationId = correlationId,
                TransactionId = Guid.NewGuid(),
                CustomerId = customerId,
                AccountId = accountId,
                Amount = amount,
                Currency = "ZAR",
                MerchantName = $"Merchant-{rng.Next(1, 100)}",
                MerchantCategory = merchantCategory,
                TransactionType = transactionType,
                Channel = channel,
                CountryCode = countryCode,
                TransactionTime = transactionTime
            };
        }

        private static T Pick<T>(Random rng, T[] items) => items[rng.Next(items.Length)];
    }
}
