using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Exceptions;
using Credit.Kafka.Messaging.Handlers;
using Credit.Kafka.Messaging.Helpers;

namespace Credit.Kafka.Messaging.Builders;

/// <summary>
/// Builder class for creating <see cref="_batchConsumerOptions"/>
/// </summary>
public class BatchConsumerOptionsBuilder
{
    private readonly BatchConsumerOptions _batchConsumerOptions = new();

    /// <summary>
    /// Assigns a handler to a topic
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithTopicHandler(string topic, Type handler)
    {
        _batchConsumerOptions.MessageHandlers.Add(topic, handler);
        return this;
    }

    /// <summary>
    /// Group id for the consumer
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithGroupId(string groupId)
    {
        _batchConsumerOptions.GroupId = groupId;
        return this;
    }

    /// <summary>
    /// Should the consumer start consuming from the earlier or latest offset, this only applies when there is no initial offset
    /// </summary>
    /// <param name="autoOffsetReset"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithAutoOffsetReset(AutoOffsetReset autoOffsetReset)
    {
        _batchConsumerOptions.AutoOffsetReset = autoOffsetReset;
        return this;
    }

    /// <summary>
    /// Partition assignment strategy, default is RoundRobin
    /// </summary>
    /// <param name="partitionAssignmentStrategy"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithPartitionAssignmentStrategy(PartitionAssignmentStrategy partitionAssignmentStrategy)
    {
        _batchConsumerOptions.PartitionAssignmentStrategy = partitionAssignmentStrategy;
        return this;
    }

    /// <summary>
    /// Batch size that each handler will receive
    /// </summary>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithBatchSize(int batchSize)
    {
        _batchConsumerOptions.BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Maximum allowed time between calls to consume messages.
    /// If this interval is exceeded the consumer is considered failed and the group will rebalance in order to reassign the partitions to another consumer group member.
    /// </summary>
    /// <param name="maxPollIntervalMs"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithMaxPollIntervalMs(int maxPollIntervalMs)
    {
        _batchConsumerOptions.MaxPollIntervalMs = maxPollIntervalMs;
        return this;
    }

    /// <summary>
    /// Client group session and failure detection timeout. The consumer sends periodic heartbeats (heartbeat. interval. ms) to indicate its liveness to the broker. If no hearts are received by the broker for a group member within the session timeout, the broker will remove the consumer from the group and trigger a rebalance.
    /// </summary>
    /// <param name="sessionTimeoutMs"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithSessionTimeoutMs(int sessionTimeoutMs)
    {
        _batchConsumerOptions.SessionTimeoutMs = sessionTimeoutMs;
        return this;
    }

    /// <summary>
    /// Sets the number of concurrent consumers
    /// </summary>
    /// <param name="concurrency"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder WithConcurrency(int concurrency)
    {
        _batchConsumerOptions.Concurrency = concurrency;
        return this;
    }

    /// <summary>
    /// Topics to subscribe to
    /// </summary>
    /// <param name="topics"></param>
    /// <returns></returns>
    public BatchConsumerOptionsBuilder SubscribeToTopics(IEnumerable<string> topics)
    {
        _batchConsumerOptions.Topics.AddRange(topics);
        return this;
    }

    private void Validate()
    {
        OptionsValidator.ValidateOptions(_batchConsumerOptions);
        if (_batchConsumerOptions.MessageHandlers.Count == 0)
            throw new ConfigurationException("At least one topic handler must be specified");

        if (_batchConsumerOptions.BatchSize <= 0)
            throw new ConfigurationException("Batch size must be greater than 0");

        foreach (var handler in _batchConsumerOptions.MessageHandlers.Values)
        {
            if (handler.GetInterface(nameof(IBatchMessageHandler)) == null)
                throw new ConfigurationException($"BatchHandler type {handler} must implement {nameof(IBatchMessageHandler)}");
        }
    }

    /// <summary>
    /// Builds and validates the BatchConsumerOptions
    /// </summary>
    /// <returns></returns>
    internal BatchConsumerOptions Build()
    {
        Validate();
        return _batchConsumerOptions;
    }
}