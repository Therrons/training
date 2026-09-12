using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Kafka
{
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

    public record FraudKafkaProducerSettings
    {
        public CompressionType CompressionType { get; set; } = CompressionType.Snappy;
        public int MessageTimeoutMs { get; set; } = 30000;
        public double LingerMs { get; set; } = 10;
        public bool EnableIdempotence { get; set; } = true;
        public int ReconnectBackoffMs { get; set; } = 50;
        public int ReconnectBackoffMaxMs { get; set; } = 10000;
        public int ApiVersionRequestTimeoutMs { get; set; } = 10000;
    }

    public record FraudKafkaConsumerSettings
    {
        // The Kafka topic to read incoming transactions from.
        [Required(ErrorMessage = "TransactionTopic cannot be empty")]
        public required string TransactionTopic { get; set; }

        [Required(ErrorMessage = "TransactionTopicDtl cannot be empty")]
        public required string TransactionTopicDtl { get; set; }

        public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
        public PartitionAssignmentStrategy PartitionAssignmentStrategy { get; set; } = PartitionAssignmentStrategy.RoundRobin;
        public int MaxPollIntervalMs { get; set; } = 300000; // 5 minutes
        public int SessionTimeoutMs { get; set; } = 45000; // 45 seconds
        public int Concurrency { get; set; } = 1;
        public bool SequentialProcessing { get; set; } = true;
        public int StartWorkerRetry { get; set; } = 5;

        // Batch processing settings
        public int BatchSize { get; set; } = 100; // Process 100 messages at once
        public int BatchProcessTimeout { get; set; } = 5; // Or wait max 5 seconds
        public int ConsumeMessageIntervalMs { get; set; } = 100; // Interval to consume messages    
        public int FetchMinBytes { get; set; } = 1024; // Minimum data to fetch
        public int FetchMaxBytes { get; set; } = 52428800; // 50 MB
    }
}