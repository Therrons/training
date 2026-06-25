using Confluent.Kafka;
using OptionsModels.KafkaOptions;

namespace OptionsModels;

public record ProducerOptions : KafkaClientOptions
{
    public CompressionType CompressionType { get; init; }

}