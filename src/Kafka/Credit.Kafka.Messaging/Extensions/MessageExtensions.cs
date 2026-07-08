using Confluent.Kafka;
using Credit.Kafka.Messaging.Contracts;
using System.Text;

namespace Credit.Kafka.Messaging.Extensions;

public static class MessageExtensions
{
    public static bool TryGetDomainEventType(this Message<byte[], byte[]> message, out string? eventType)
    {
        bool exists = message.Headers.TryGetLastBytes(DomainEventHeaders.EventType, out var headerBytes);

        if (!exists)
        {
            eventType = null;
            return false;
        }

        eventType = Encoding.UTF8.GetString(headerBytes);
        return true;
    }

    public static Dictionary<string, string> GetMessageHeaders(this Message<byte[], byte[]> message)
    {
        var headers = message.Headers.ToDictionary(header => header.Key, header => Encoding.UTF8.GetString(header.GetValueBytes()));

        return headers;
    }
}