using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Kafka
{
    /// <summary>
    /// Simplified Kafka broker configuration for fraud detection
    /// </summary>
    public record FraudKafkaBrokerSettings
    {
        [Required(ErrorMessage = "BootstrapServers cannot be emptpy")]
        public required string BootstrapServers { get; set; }

        public string? SaslUserName { get; set; }
        public string? SaslPassword { get; set; }
        public SaslMechanism SaslMechanism { get; set; } = SaslMechanism.Plain;
        public SecurityProtocol SecurityProtocol { get; set; } = SecurityProtocol.SaslSsl;
        public bool AllowAutoCreateTopics { get; set; } = false;
    }

    /// <summary>
    /// Producer configuration for fraud events
    /// </summary>
    public record FraudKafkaProducerSettings
    {
        public CompressionType CompressionType { get; set; } = CompressionType.Snappy;
        public int MessageTimeoutMs { get; set; } = 30000;
        public double LingerMs { get; set; } = 10;
        public bool EnableIdempotence { get; set; } = true;
    }

    /// <summary>
    /// Consumer configuration for transaction events
    /// </summary>
    public record FraudKafkaConsumerSettings
    {
        [Required(ErrorMessage = "Topic Name cannot be emptpy")]
        public required string TransactionTopic { get; set; }

        [Required(ErrorMessage = "Topic Dtl Name cannot be emptpy")]
        public required string TransactionTopicDtl { get; set; }

        public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
        public PartitionAssignmentStrategy PartitionAssignmentStrategy { get; set; } = PartitionAssignmentStrategy.CooperativeSticky;
        public int MaxPollIntervalMs { get; set; } = 300000; // 5 minutes
        public int SessionTimeoutMs { get; set; } = 45000; // 45 seconds
        public bool EnableAutoCommit { get; set; } = false; // Manual commit for better control
        public int Concurrency { get; set; } = 1;

        // Batch processing settings
        public int BatchSize { get; set; } = 100; // Process 100 messages at once
        public int BatchTimeoutSeconds { get; set; } = 5; // Or wait max 5 seconds
        public int FetchMinBytes { get; set; } = 1024; // Minimum data to fetch
        public int FetchMaxBytes { get; set; } = 52428800; // 50 MB
    }
}