using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Xunit;

namespace Publisher.Tests
{
    public class TopicTests
    {
        private string GetConnectionString() =>
            Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTIONSTRING")
            ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        [Fact]
        public async Task SendAndReceiveMessage_ToTopic2()
        {
            string connectionString = GetConnectionString();
            string topicName = "topic.1";
            string subscriptionName = "subscription.2";
            string body = $"test-message-{Guid.NewGuid()}";

            await using var client = new ServiceBusClient(connectionString);
            ServiceBusSender sender = client.CreateSender(topicName);
            var messageId = Guid.NewGuid().ToString();
            var message1 = new ServiceBusMessage(body) { MessageId = messageId };
            message1.ApplicationProperties["UserType"] = "Admin";
            message1.ApplicationProperties["UserRole"] = "Guest";
            var message2 = new ServiceBusMessage(body) { MessageId = messageId };
            message2.ApplicationProperties["UserType"] = "User";
            await sender.SendMessagesAsync(messages: new[] { message1, message2 });

            ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName, new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

            ServiceBusReceivedMessage received = null;
            var responses = await receiver.ReceiveMessagesAsync(3, TimeSpan.FromSeconds(5));
            foreach (var r in responses)
            {
                if (r.MessageId == messageId)
                {
                    received = r;
                    await receiver.CompleteMessageAsync(r);
                    break;
                }
                else
                {
                    await receiver.AbandonMessageAsync(r);
                }
            }

            Assert.NotNull(received);
            Assert.Equal(body, received.Body.ToString());
        }
    }
}
