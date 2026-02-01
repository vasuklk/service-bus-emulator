using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Xunit;

namespace Publisher.Tests
{
    public class TopicTests
    {
        private static string connectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        string topicName = "topic.1";
        string subscriptionName = "subscription.2";

        ServiceBusClient client = new ServiceBusClient(connectionString);

        [Fact]
        public async Task SendMessage_ToTopic()
        {
            
            string body = $"test-message";
            ServiceBusSender sender = client.CreateSender(topicName);

            // Create first message with properties
            var message1 = new ServiceBusMessage(body);
            message1.ApplicationProperties["UserType"] = "Admin";
            message1.ApplicationProperties["UserRole"] = "Guest";

            // Create second message with different properties
            var message2 = new ServiceBusMessage(body);
            message2.ApplicationProperties["UserType"] = "User";

            // Send both messages
            await sender.SendMessagesAsync(messages: new[] { message1, message2 });
        }

        [Fact]
        public async Task ReceiveMessage_FromTopic()
        {
            
            string body = $"test-message";

            ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName, new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

            var responses = await receiver.ReceiveMessagesAsync(3, TimeSpan.FromSeconds(5));
            ServiceBusReceivedMessage received = responses[0];

            Assert.NotNull(received);
            Assert.Equal(body, received.Body.ToString());
        }
    }
}
