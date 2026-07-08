using Confluent.Kafka;
using Credit.Kafka.Messaging.Contracts;
using Credit.Kafka.Messaging.Models;

namespace Credit.Kafka.Messaging.Producers;

/// <summary>
///     Leverages the injected KafkaClientHandle instance to allow
///     Confluent.Kafka.Message{K,V}s to be produced to Kafka.
/// </summary>
public interface IDomainProducer
{
    string ProducerId { get; }
    IProducer<byte[], byte[]> KafkaProducer { get; }
    Task<bool> ProduceAsync<T>(string topic, string key, T eventMessage, Dictionary<string, string>? additionalHeaders = null) where T : DomainEvent;
    Task<bool> ProduceToDltAsync(string topic, Message<byte[], byte[]> eventMessage);
    void Produce<T>(string topic, string key, T eventMessage, Dictionary<string, string>? additionalHeaders = null, Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null) where T : DomainEvent;
    void ProduceToDlt(string topic, Message<byte[], byte[]> eventMessage, Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null);
    /// <summary>
    /// Batch produce events of a single type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic"></param>
    /// <param name="eventMessages"></param>
    /// <returns></returns>
    bool ProduceBatch<T>(string topic, List<ProduceDomainEventMessage<T>> eventMessages) where T : DomainEvent;
    /// <summary>
    /// Batch produce multiple types of events.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="eventMessages"></param>
    /// <returns></returns>
    bool ProduceBatch(string topic, List<ProduceDomainEventMessage<DomainEvent>> eventMessages);
    /// <summary>
    /// Batch produce multiple types of events to multiple topics.
    /// <para>If ordering is of concern, maintain event order in each list.</para>
    /// </summary>
    /// <param name="eventTopicDict">event types mapped to target topics</param>
    /// <param name="eventMessages"></param>
    /// <returns></returns>
    bool ProduceBatch(Dictionary<string, List<ProduceDomainEventMessage<DomainEvent>>> topicEvents);
    void Flush(TimeSpan? timeout = null);
}