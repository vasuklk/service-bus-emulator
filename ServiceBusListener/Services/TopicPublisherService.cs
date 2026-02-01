using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceBusListener.Services
{
    public class TopicPublisherService
    {
        private readonly string _connectionString;
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly ILogger<TopicPublisherService> _logger;

        public TopicPublisherService(string connectionString, string topicName, string subscriptionName, ILogger<TopicPublisherService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _subscriptionName = subscriptionName ?? throw new ArgumentNullException(nameof(subscriptionName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TopicPublisherService starting...");

            try
            {
                string testMessage = $"test-message-published";

                await using var client = new ServiceBusClient(_connectionString);

                // Create topic and subscription if they don't exist
                var sender = client.CreateSender(_topicName);

                // Send a message to the topic
                var message = new ServiceBusMessage(testMessage)
                {
                    MessageId = Guid.NewGuid().ToString()
                };
                message.ApplicationProperties["UserType"] = "Admin";
                
                await sender.SendMessageAsync(message);
                PublishedMessages.Add(testMessage);
                _logger.LogInformation($"Sent message {testMessage} to topic {_topicName} for subscription {_subscriptionName}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TopicPublisherService cancellation requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TopicPublisherService failed: {Message}", ex.Message);
                throw;
            }
        }

        public static List<string> PublishedMessages { get; } = new();
    }
}
