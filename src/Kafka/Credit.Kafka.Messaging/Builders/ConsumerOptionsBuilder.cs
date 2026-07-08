using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Exceptions;
using Credit.Kafka.Messaging.Handlers;
using Credit.Kafka.Messaging.Helpers;

namespace Credit.Kafka.Messaging.Builders
{
    public class ConsumerOptionsBuilder
    {
        private readonly ConsumerOptions _options = new();
        private bool _isMessageHandler;
        private bool _isTopicHandler;

        /// <summary>
        /// Sets the consumer group ID.
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public ConsumerOptionsBuilder WithGroupId(string groupId)
        {
            _options.GroupId = groupId;
            return this;
        }

        /// <summary>
        /// Sets the auto offset reset policy.
        /// </summary>
        /// <param name="autoOffsetReset"></param>
        /// <returns></returns>
        public ConsumerOptionsBuilder WithAutoOffsetReset(AutoOffsetReset autoOffsetReset)
        {
            _options.AutoOffsetReset = autoOffsetReset;
            return this;
        }

        /// <summary>
        /// Sets the partition assignment strategy. Default is RoundRobin.
        /// When the strategy is set to CooperativeSticky, there is a chance that the consumer will fail to commit messages on the partitions it is no longer assigned to
        /// RoundRobin and Range do not have this problem.
        /// </summary>
        /// <param name="partitionAssignmentStrategy"></param>
        /// <returns></returns>
        public ConsumerOptionsBuilder WithPartitionAssignmentStrategy(PartitionAssignmentStrategy partitionAssignmentStrategy)
        {
            _options.PartitionAssignmentStrategy = partitionAssignmentStrategy;
            return this;
        }

        /// <summary>
        /// Assigns a message handler to a specific Domain event type.
        /// You can have all handlers be topic handlers or all be message handlers. i.e. you cannot mix the two.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="handlerType"></param>
        /// <returns></returns>
        public ConsumerOptionsBuilder WithMessageHandler(string eventType, Type handlerType)
        {
            _options.MessageHandlers[eventType] = handlerType;
            _isMessageHandler = true;
            return this;
        }

        /// <summary>
        /// Assigns a message handler to a specific topic.
        /// You can have all handlers be topic handlers or all be message handlers. i.e. you cannot mix the two.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="handlerType"></param>
        /// <returns></returns>
        public ConsumerOptionsBuilder WithTopicHandler(string topic, Type handlerType)
        {
            _options.MessageHandlers[topic] = handlerType;
            _isTopicHandler = true;
            return this;
        }

        /// <summary>
        /// Sets the number of concurrent consumers
        /// </summary>
        /// <param name="concurrency"></param>
        /// <returns></returns>
        public ConsumerOptionsBuilder WithConcurrency(int concurrency)
        {
            _options.Concurrency = concurrency;
            return this;
        }

        /// <summary>
        /// Topics to subscribe to
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        public ConsumerOptionsBuilder SubscribeToTopics(IEnumerable<string> topics)
        {
            _options.Topics.AddRange(topics);
            return this;
        }

        private void Validate()
        {
            OptionsValidator.ValidateOptions(_options);

            if (_options.MessageHandlers.Count == 0)
            {
                throw new ConfigurationException("No message handlers configured.");
            }

            foreach (var handler in _options.MessageHandlers.Values)
            {
                if (handler.GetInterface(nameof(IMessageHandler)) == null)
                    throw new ConfigurationException($"Handler type {handler} must implement {nameof(IMessageHandler)}");
            }

            if (_isMessageHandler && _isTopicHandler)
            {
                throw new ConfigurationException("All configured handlers must be either domain event handlers or topic handlers, not a combination of the two.");
            }

            _options.ConsumeDomainEvents = _isMessageHandler;

            if (_options.Concurrency <= 0)
            {
                throw new ConfigurationException("Concurrency must be greater than 0");
            }

            if (_options?.MessageHandlers?.Keys?.Any() ?? false)
            {
                throw new ConfigurationException("At least one topic must be subscribed to");
            }
        }

        /// <summary>
        /// Validates and returns ConsumerOptions
        /// </summary>
        /// <returns></returns>
        internal ConsumerOptions Build()
        {
            Validate();
            return _options;
        }
    }
}