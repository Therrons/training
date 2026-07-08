using Credit.Kafka.Messaging.Producers;
using fraud_poc_project.Kafka.Events;
using fraud_poc_project_models.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private readonly IDomainProducer _producer;
        private readonly AppSettings _appSettings;
        private readonly ILogger<LoadSimulatorController> _logger;

        public LoadSimulatorController(
            IDomainProducer producer,
            AppSettings appSettings,
            ILogger<LoadSimulatorController> logger)
        {
            _producer = producer;
            _appSettings = appSettings;
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

            var topic = _appSettings.IncomingFraudConsumerOptions?.FraudTopic
                ?? throw new InvalidOperationException("FraudTopic is not configured.");

            var rng = new Random();
            int produced = 0;
            int failed = 0;

            for (int i = 0; i < count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                bool isFraudulent = rng.NextDouble() < highFraudRatio;
                var @event = BuildEvent(rng, isFraudulent);

                bool ok = await _producer.ProduceAsync(topic, @event.TransactionId.ToString(), @event);
                if (ok)
                    produced++;
                else
                    failed++;

                // small stagger to avoid overwhelming the broker in a single burst
                if (i % 50 == 49)
                    await Task.Delay(50, cancellationToken);
            }

            _logger.LogInformation(
                "Load simulation completed: {Produced} produced, {Failed} failed out of {Total} requested.",
                produced, failed, count);

            return Ok(new LoadSimulationResult
            {
                Requested = count,
                Produced = produced,
                Failed = failed,
                Topic = topic
            });
        }

        /// <summary>
        /// Produce a single synthetic transaction event and return it as a preview.
        /// </summary>
        [HttpGet("preview")]
        public ActionResult<FraudTransactionDomainEvent> Preview([FromQuery] bool fraudulent = false)
        {
            return Ok(BuildEvent(new Random(), fraudulent));
        }

        private static FraudTransactionDomainEvent BuildEvent(Random rng, bool fraudulent)
        {
            // Pick a random customer from a pool so some customers appear repeatedly
            var customerId = $"CUST-{rng.Next(1, 200):D4}";
            var accountId = $"ACC-{rng.Next(1, 500):D5}";

            string transactionType;
            string? channel;
            decimal amount;
            string? merchantCategory;
            string? countryCode;
            DateTime transactionTime;

            if (fraudulent)
            {
                // Deliberately trigger fraud rules
                transactionType = Pick(rng, new[] { "CNP", "ATM" });
                channel = transactionType == "CNP" ? "Online" : "ATM";
                amount = Pick(rng, new[] { 60000m, 10000m, 15000m, 100000m, 5000m });
                merchantCategory = Pick(rng, new[] { "gambling", "crypto", "money_transfer" });
                countryCode = Pick(rng, new[] { "US", "NG", "GB" });        // non-ZA
                // Unusual hours: midnight–4 AM
                var baseDate = DateTime.UtcNow.Date.AddHours(2);
                transactionTime = baseDate.AddMinutes(rng.Next(0, 120));
            }
            else
            {
                transactionType = Pick(rng, TransactionTypes);
                channel = Pick(rng, Channels);
                amount = Math.Round((decimal)(rng.NextDouble() * 4900 + 100), 2);
                merchantCategory = Pick(rng, MerchantCategories);
                countryCode = rng.NextDouble() < 0.9 ? "ZA" : Pick(rng, CountryCodes);
                // Normal daytime hours
                transactionTime = DateTime.UtcNow.AddHours(-rng.Next(0, 48));
            }

            var correlationId = Guid.NewGuid();
            return new FraudTransactionDomainEvent
            {
                Metadata = new Credit.Kafka.Messaging.Contracts.DomainEventMetadata
                {
                    PublishedAt = DateTime.UtcNow,
                    Type = FraudTransactionDomainEvent.EventType,
                    Version = FraudTransactionDomainEvent.EventVersion,
                    CorrelationId = correlationId
                },
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

    public class LoadSimulationResult
    {
        public int Requested { get; init; }
        public int Produced { get; init; }
        public int Failed { get; init; }
        public string Topic { get; init; } = string.Empty;
    }
}
